using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class TaskListApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task List_Defaults_CountsOnlyVisibleActiveTasksAndReturnsFirstTwenty(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member, role);

        using var response = await client.GetAsync("/api/v1/tasks");

        var page = await ReadPageAsync(response);
        var expected = sample.Tasks.Where(task => task.ProjectId != sample.Archived.Id &&
            (role == "Admin" || task.ProjectId != sample.Foreign.Id))
            .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Take(20).Select(task => task.Id), page.Items.Select(task => task.Id));
        Assert.Equal(1, factory.Introspection.RequestCount);
    }

    [Theory]
    [InlineData("User", "active", true)]
    [InlineData("Admin", "active", true)]
    [InlineData("User", "foreign", false)]
    [InlineData("Admin", "foreign", true)]
    [InlineData("User", "archived", false)]
    [InlineData("Admin", "archived", true)]
    [InlineData("User", "missing", false)]
    [InlineData("Admin", "missing", false)]
    [InlineData("User", "empty", true)]
    [InlineData("Admin", "empty", true)]
    public async Task List_ProjectFilter_DistinguishesInvisibleProjectsFromEmptyResults(
        string role, string scenario, bool visible)
    {
        var sample = await SeedAsync();
        var projectId = scenario switch
        {
            "active" => sample.Active[0].Id,
            "foreign" => sample.Foreign.Id,
            "archived" => sample.Archived.Id,
            "empty" => sample.Empty.Id,
            _ => Guid.NewGuid()
        };
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member, role);

        using var response = await client.GetAsync($"/api/v1/tasks?projectId={projectId}&pageSize=100");

        if (!visible)
        {
            await ReadProblemAsync(response, HttpStatusCode.NotFound);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(sample.Foreign.Name, body);
            Assert.DoesNotContain(sample.Archived.Name, body);
            return;
        }
        var page = await ReadPageAsync(response);
        var expected = sample.Tasks.Where(task => task.ProjectId == projectId)
            .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData("status=ToDo", "status")]
    [InlineData("status=ToDo&status=Done", "statuses")]
    [InlineData("priority=High", "priority")]
    [InlineData("priority=Low&priority=High", "priorities")]
    [InlineData("status=ToDo&status=InProgress&priority=High", "combined")]
    [InlineData("status=ToDo&status=ToDo&priority=Low&priority=Low", "duplicates")]
    public async Task List_EnumFilters_ApplyAlternativesWithinEachFilterAndCombineFilters(
        string query, string scenario)
    {
        var sample = await SeedAsync();
        var expected = VisibleTasks(sample).Where(task => scenario switch
        {
            "status" => task.Status == TaskState.ToDo,
            "statuses" => task.Status is TaskState.ToDo or TaskState.Done,
            "priority" => task.Priority == TaskPriority.High,
            "priorities" => task.Priority is TaskPriority.Low or TaskPriority.High,
            "combined" => task.Status != TaskState.Done && task.Priority == TaskPriority.High,
            _ => task.Status == TaskState.ToDo && task.Priority == TaskPriority.Low
        }).OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks?{query}&pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData("unassigned")]
    [InlineData("assigned")]
    [InlineData("unknown")]
    public async Task List_AssigneeFilter_DistinguishesUnassignedSpecificUserAndUnknownUser(string mode)
    {
        var sample = await SeedAsync();
        var assigneeId = mode == "unknown" ? Guid.NewGuid() : sample.Assignees[0].Id;
        var parameter = mode == "unassigned" ? "unassigned" : assigneeId.ToString();
        var expected = VisibleTasks(sample).Where(task => mode == "unassigned"
            ? task.AssignedId is null : task.AssignedId == assigneeId)
            .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks?assigneeId={parameter}&pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Fact]
    public async Task List_AllFiltersTogether_CountsBeforePagination()
    {
        var sample = await SeedAsync();
        var expected = VisibleTasks(sample).Where(task => task.ProjectId == sample.Active[0].Id &&
            task.Status == TaskState.ToDo && task.Priority == TaskPriority.Low &&
            task.AssignedId == sample.Assignees[0].Id)
            .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks?projectId={sample.Active[0].Id}" +
            $"&status=ToDo&priority=Low&assigneeId={sample.Assignees[0].Id}&pageSize=1");

        var page = await ReadPageAsync(response);
        Assert.True(expected.Length > 1);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Take(1).Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    [InlineData("dueDate", null)]
    [InlineData("dueDate", "asc")]
    [InlineData("dueDate", "desc")]
    public async Task List_Sorting_UsesContractTieBreaksAndKeepsNullDueDatesLast(string sort, string? order)
    {
        var sample = await SeedAsync();
        var tasks = VisibleTasks(sample).ToArray();
        IEnumerable<DomainTask> expected;
        if (sort == "dueDate")
        {
            // Separar las fechas ausentes permite comprobar NULLS LAST en ambas direcciones.
            var dated = tasks.Where(task => task.DueDate.HasValue);
            var ordered = order == "desc"
                ? dated.OrderByDescending(task => task.DueDate)
                : dated.OrderBy(task => task.DueDate);
            expected = ordered.ThenByDescending(task => task.CreatedAt).ThenBy(task => task.Id)
                .Concat(tasks.Where(task => task.DueDate is null)
                    .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id));
        }
        else
        {
            expected = (order == "asc" ? tasks.OrderBy(task => task.CreatedAt)
                : tasks.OrderByDescending(task => task.CreatedAt)).ThenBy(task => task.Id);
        }
        var expectedIds = expected.Select(task => task.Id).ToArray();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var query = $"sort={sort}" + (order is null ? "" : $"&order={order}");

        using var response = await client.GetAsync($"/api/v1/tasks?{query}&pageSize=100");
        var page = await ReadPageAsync(response);
        Assert.Equal(expectedIds, page.Items.Select(task => task.Id));

        using var pagedResponse = await client.GetAsync($"/api/v1/tasks?{query}&page=2&pageSize=5");
        var secondPage = await ReadPageAsync(pagedResponse);
        Assert.Equal(expectedIds.Length, secondPage.TotalCount);
        Assert.Equal(expectedIds.Skip(5).Take(5), secondPage.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 20)]
    [InlineData(3, 13)]
    [InlineData(99, 20)]
    [InlineData(int.MaxValue, 100)]
    public async Task List_Pagination_ReturnsRequestedSliceAndRealTotalEvenBeyondLastPage(int page, int pageSize)
    {
        var sample = await SeedAsync();
        var expected = VisibleTasks(sample).OrderByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id).ToArray();
        var offset = ((long)page - 1) * pageSize;
        var expectedIds = offset >= expected.Length ? [] :
            expected.Skip((int)offset).Take(pageSize).Select(task => task.Id).ToArray();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);

        using var response = await client.GetAsync($"/api/v1/tasks?page={page}&pageSize={pageSize}");

        var result = await ReadPageAsync(response);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(expected.Length, result.TotalCount);
        Assert.Equal(expectedIds, result.Items.Select(task => task.Id));
    }

    [Fact]
    public async Task List_NoMemberships_ReturnsEmptyPage()
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Outsider);

        using var response = await client.GetAsync("/api/v1/tasks");

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task List_Contract_MapsProjectNamesAndAssigneesWithoutExposingIdentityDataOrChangingTasks()
    {
        var sample = await SeedAsync();
        var expected = VisibleTasks(sample).ToDictionary(task => task.Id);
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var before = await ReadSnapshotAsync();

        using var response = await client.GetAsync("/api/v1/tasks?pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.All(page.Items, item =>
        {
            var task = expected[item.Id];
            Assert.Equal(task.ProjectId, item.ProjectId);
            Assert.Equal(sample.Active.Single(project => project.Id == task.ProjectId).Name, item.ProjectName);
            Assert.Equal(task.Title, item.Title);
            Assert.Equal(task.Description, item.Description);
            Assert.Equal(task.Status.ToString(), item.Status);
            Assert.Equal(task.Priority.ToString(), item.Priority);
            Assert.Equal(task.DueDate, item.DueDate);
            Assert.Equal(task.CreatedAt, item.CreatedAt);
            Assert.Equal(task.UpdatedAt, item.UpdatedAt);
            if (task.AssignedId is null)
                Assert.Null(item.Assignee);
            else
            {
                Assert.NotNull(item.Assignee);
                Assert.Equal(task.AssignedId, item.Assignee.Id);
                Assert.Equal(sample.Assignees.Single(user => user.Id == task.AssignedId).DisplayName,
                    item.Assignee.DisplayName);
            }
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "items", "page", "pageSize", "totalCount" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.Equal(new[] { "assignee", "createdAt", "description", "dueDate", "id", "priority",
                "projectId", "projectName", "status", "title", "updatedAt" },
                item.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
            if (item.GetProperty("assignee").ValueKind != JsonValueKind.Null)
                Assert.Equal(new[] { "displayName", "id" }, item.GetProperty("assignee")
                    .EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        }
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    [Theory]
    [InlineData("status=todo", "status")]
    [InlineData("status=0", "status")]
    [InlineData("status=ToDo&status=Invalid", "status")]
    [InlineData("priority=high", "priority")]
    [InlineData("priority=1", "priority")]
    [InlineData("priority=High&priority=Invalid", "priority")]
    [InlineData("assigneeId=Unassigned", "assigneeId")]
    [InlineData("assigneeId=invalid", "assigneeId")]
    [InlineData("sort=title", "sort")]
    [InlineData("sort=DueDate", "sort")]
    [InlineData("order=ASC", "order")]
    [InlineData("order=invalid", "order")]
    [InlineData("page=0", "page")]
    [InlineData("page=-1", "page")]
    [InlineData("page=abc", "page")]
    [InlineData("page=2147483648", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("pageSize=abc", "pageSize")]
    [InlineData("projectId=invalid", "projectId")]
    public async Task List_InvalidQuery_ReturnsFieldValidationProblemWithoutWriting(string query, string field)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Member);
        var before = await ReadSnapshotAsync();

        using var response = await client.GetAsync($"/api/v1/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ValidationProblemDetails>(
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
        Assert.Equal(400, problem.Status);
        Assert.Contains(problem.Errors.Keys, key => key.Equals(field, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task List_InvalidSession_DoesNotReturnTasksOrWrite(string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                session == "invalid-token" ? "invalid-token" :
                factory.CreateToken(sample.Member.KeycloakSub, session == "missing-role" ? ["Other"] : ["User"]));
        if (session is "inactive-token" or "introspection-unavailable")
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("{\"active\":false}") });
        var before = await ReadSnapshotAsync();

        using var response = await client.GetAsync("/api/v1/tasks");

        await ReadProblemAsync(response, status);
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    private async Task<Sample> SeedAsync()
    {
        await using var context = CreateContext();
        // Solo se limpia la base temporal de la fixture de esta clase, nunca la de Aspire.
        await context.Projects.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", "creator@example.com");
        var member = new AppUser(Guid.NewGuid().ToString(), "Miembro lector", "member@example.com");
        var outsider = new AppUser(Guid.NewGuid().ToString(), "Sin pertenencias", null);
        var assignees = new[]
        {
            new AppUser(Guid.NewGuid().ToString(), "Asignado A", "a@example.com"),
            new AppUser(Guid.NewGuid().ToString(), "Asignado B", "b@example.com")
        };
        context.Users.AddRange(new[] { creator, member, outsider }.Concat(assignees));
        var active = new[] { new Project("Proyecto A", null, creator.Id), new Project("Proyecto B", null, creator.Id) };
        var foreign = new Project("Proyecto ajeno", null, creator.Id);
        var archived = new Project("Proyecto archivado", null, creator.Id);
        var empty = new Project("Proyecto sin tareas", null, creator.Id);
        archived.Archive();
        var projects = active.Concat(new[] { foreign, archived, empty }).ToArray();
        context.Projects.AddRange(projects);
        foreach (var project in projects)
        {
            context.ProjectMemberships.Add(new AppUserProject(project.Id, creator.Id));
            foreach (var assignee in assignees)
                context.ProjectMemberships.Add(new AppUserProject(project.Id, assignee.Id));
            if (project.Id != foreign.Id)
                context.ProjectMemberships.Add(new AppUserProject(project.Id, member.Id));
        }
        var tasks = new DomainTask[28];
        var firstDate = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        for (var index = 0; index < tasks.Length; index++)
        {
            var project = index == 26 ? foreign : index == 27 ? archived : active[index % 2];
            var task = new DomainTask(project.Id, $"Tarea {index}", (TaskPriority)((index / 3) % 3),
                index % 2 == 0 ? null : $"Descripción {index}",
                index % 3 == 2 ? null : assignees[index % 3].Id,
                (TaskState)(index % 3), index % 4 == 3 ? null : new DateOnly(2026, 11, 10).AddDays(index % 3));
            context.Entry(task).Property(item => item.Id).CurrentValue =
                Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}");
            context.Entry(task).Property(item => item.CreatedAt).CurrentValue = firstDate.AddHours(index / 2);
            context.Entry(task).Property(item => item.UpdatedAt).CurrentValue = task.CreatedAt.AddMinutes(1);
            tasks[index] = task;
        }
        // Insertar en orden inverso evita depender accidentalmente del orden de inserción.
        context.Tasks.AddRange(tasks.Reverse());
        await context.SaveChangesAsync();
        return new Sample(member, outsider, assignees, active, foreign, archived, empty, tasks);
    }

    private static IEnumerable<DomainTask> VisibleTasks(Sample sample) => sample.Tasks
        .Where(task => sample.Active.Any(project => project.Id == task.ProjectId));

    private AuthenticationTestFactory CreateFactory() => new(postgres.Database.GetConnectionString());

    private TaskFlowDbContext CreateContext() => new(new DbContextOptionsBuilder<TaskFlowDbContext>()
        .UseNpgsql(postgres.Database.GetConnectionString()).Options);

    private async Task<string> ReadSnapshotAsync()
    {
        await using var context = CreateContext();
        return JsonSerializer.Serialize(new
        {
            Users = await context.Users.AsNoTracking().OrderBy(user => user.Id).ToArrayAsync(),
            Projects = await context.Projects.AsNoTracking().OrderBy(project => project.Id).ToArrayAsync(),
            Memberships = await context.ProjectMemberships.AsNoTracking()
                .OrderBy(item => item.ProjectId).ThenBy(item => item.UserId).ToArrayAsync(),
            Tasks = await context.Tasks.AsNoTracking().OrderBy(task => task.Id).ToArrayAsync()
        });
    }

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user, string role = "User")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static async Task<TaskPageResponse> ReadPageAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<TaskPageResponse>(await response.Content.ReadFromJsonAsync<TaskPageResponse>());
    }

    private static async Task ReadProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = Assert.IsType<ProblemDetails>(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal((int)status, problem.Status);
    }

    private sealed record Sample(AppUser Member, AppUser Outsider, AppUser[] Assignees, Project[] Active,
        Project Foreign, Project Archived, Project Empty, DomainTask[] Tasks);
}
