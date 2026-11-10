using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;
using AppUser = TaskFlow.Domain.AppUser;
using AppUserProject = TaskFlow.Domain.AppUserProject;
using DomainTask = TaskFlow.Domain.Task;
using Project = TaskFlow.Domain.Project;
using TaskPriority = TaskFlow.Domain.TaskPriority;
using TaskState = TaskFlow.Domain.TaskState;

namespace TaskFlow.Api.IntegrationTests;

public class MyTasksApiTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task List_UsesTokenIdentityAndOnlyActiveProjectsWithoutChangingStoredTasks(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current, role);
        var expected = Mine(sample, sample.Current).OrderByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id).ToArray();
        var before = await ReadSnapshotAsync();

        // Parámetros ajenos al contrato no pueden sustituir la identidad autenticada.
        using var response = await client.GetAsync($"/api/v1/me/tasks?userId={sample.Other.Id}&isAdmin=true");

        var page = await ReadPageAsync(response);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item =>
        {
            var task = expected.Single(task => task.Id == item.Id);
            Assert.NotNull(item.Assignee);
            Assert.Equal(sample.Current.Id, item.Assignee.Id);
            Assert.Equal(sample.Current.DisplayName, item.Assignee.DisplayName);
            Assert.Equal(sample.Active.Single(project => project.Id == task.ProjectId).Name, item.ProjectName);
            Assert.Equal(task.Title, item.Title);
            Assert.Equal(task.Description, item.Description);
            Assert.Equal(task.Status.ToString(), item.Status);
            Assert.Equal(task.Priority.ToString(), item.Priority);
            Assert.Equal(task.DueDate, item.DueDate);
            Assert.Equal(task.CreatedAt, item.CreatedAt);
            Assert.Equal(task.UpdatedAt, item.UpdatedAt);
        });
        Assert.Equal(before, await ReadSnapshotAsync());
        Assert.Equal(1, factory.Introspection.RequestCount);

        using var otherClient = CreateClient(factory, sample.Other, role);
        using var otherResponse = await otherClient.GetAsync("/api/v1/me/tasks?pageSize=100");
        var otherPage = await ReadPageAsync(otherResponse);
        var others = Mine(sample, sample.Other).OrderByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id).ToArray();
        Assert.NotEmpty(others);
        Assert.Equal(others.Length, otherPage.TotalCount);
        Assert.Equal(others.Select(task => task.Id), otherPage.Items.Select(task => task.Id));
        Assert.Empty(page.Items.Select(task => task.Id).Intersect(otherPage.Items.Select(task => task.Id)));
    }

    [Theory]
    [InlineData("User", "active", true)]
    [InlineData("Admin", "active", true)]
    [InlineData("User", "foreign", false)]
    [InlineData("Admin", "foreign", true)]
    [InlineData("User", "archived", false)]
    [InlineData("Admin", "archived", false)]
    [InlineData("User", "missing", false)]
    [InlineData("Admin", "missing", false)]
    [InlineData("User", "empty", true)]
    [InlineData("Admin", "empty", true)]
    public async Task List_ProjectFilter_EnforcesVisibilityAndRejectsArchivedProjectsEvenForAdmin(
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
        using var client = CreateClient(factory, sample.Current, role);

        using var response = await client.GetAsync($"/api/v1/me/tasks?projectId={projectId}&pageSize=100");

        if (!visible)
        {
            await ReadProblemAsync(response, HttpStatusCode.NotFound);
            return;
        }
        var page = await ReadPageAsync(response);
        var expected = Mine(sample, sample.Current).Where(task => task.ProjectId == projectId)
            .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task List_AssigneeParameter_IsRejectedIncludingEmptyRepeatedAndCaseVariants(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current, role);
        var before = await ReadSnapshotAsync();
        var attempts = new[]
        {
            $"assigneeId={sample.Other.Id}", $"assigneeId={sample.Current.Id}",
            "assigneeId=unassigned", "assigneeId=", "assigneeId", "assigneeId=invalid",
            $"AssigneeId={sample.Other.Id}", $"assigneeId=&assigneeId={sample.Other.Id}"
        };

        foreach (var query in attempts)
        {
            using var response = await client.GetAsync($"/api/v1/me/tasks?{query}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = Assert.IsType<ValidationProblemDetails>(
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
            Assert.Equal(400, problem.Status);
            Assert.Contains(problem.Errors.Keys,
                key => key.Equals("assigneeId", StringComparison.OrdinalIgnoreCase));
        }
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task Service_OverridesCallerSuppliedAssignmentAndActiveProjectFlags(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TaskService>();
        var filter = new TaskListFilter
        {
            AssigneeId = sample.Other.Id,
            OnlyUnassigned = true,
            OnlyActiveProjects = false,
            PageSize = 100
        };

        var page = await service.GetMyTasksAsync(filter, sample.Current.Id, role == "Admin", default);

        Assert.NotNull(page);
        var expected = Mine(sample, sample.Current).OrderByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id).ToArray();
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Select(task => task.Id), page.Items.Select(task => task.Id));
        Assert.All(page.Items, task => Assert.Equal(sample.Current.Id, task.AssignedId));
        // El servicio construye sus propios filtros sin modificar el objeto recibido.
        Assert.Equal(sample.Other.Id, filter.AssigneeId);
        Assert.True(filter.OnlyUnassigned);
        Assert.False(filter.OnlyActiveProjects);
    }

    [Theory]
    [InlineData("status=ToDo&status=InProgress", "statuses")]
    [InlineData("priority=Low&priority=High", "priorities")]
    [InlineData("status=ToDo&priority=High", "combined")]
    public async Task List_Filters_ApplyToOwnTasksBeforeCountingAndPaging(string query, string mode)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current);
        var expected = Mine(sample, sample.Current).Where(task => mode switch
        {
            "statuses" => task.Status is TaskState.ToDo or TaskState.InProgress,
            "priorities" => task.Priority is TaskPriority.Low or TaskPriority.High,
            _ => task.Status == TaskState.ToDo && task.Priority == TaskPriority.High
        }).OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id).ToArray();

        using var response = await client.GetAsync($"/api/v1/me/tasks?{query}&pageSize=1");

        var page = await ReadPageAsync(response);
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal(expected.Take(1).Select(task => task.Id), page.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    [InlineData("dueDate", null)]
    [InlineData("dueDate", "asc")]
    [InlineData("dueDate", "desc")]
    public async Task List_Sorting_PreservesTieBreaksAndNullsLastAcrossPages(string sort, string? order)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current);
        var mine = Mine(sample, sample.Current).ToArray();
        IEnumerable<DomainTask> expected;
        if (sort == "dueDate")
        {
            var dated = mine.Where(task => task.DueDate.HasValue);
            var ordered = order == "desc" ? dated.OrderByDescending(task => task.DueDate)
                : dated.OrderBy(task => task.DueDate);
            expected = ordered.ThenByDescending(task => task.CreatedAt).ThenBy(task => task.Id)
                .Concat(mine.Where(task => task.DueDate is null)
                    .OrderByDescending(task => task.CreatedAt).ThenBy(task => task.Id));
        }
        else
            expected = (order == "asc" ? mine.OrderBy(task => task.CreatedAt)
                : mine.OrderByDescending(task => task.CreatedAt)).ThenBy(task => task.Id);
        var expectedIds = expected.Select(task => task.Id).ToArray();
        var query = $"sort={sort}" + (order is null ? "" : $"&order={order}");

        using var response = await client.GetAsync($"/api/v1/me/tasks?{query}&pageSize=100");
        var page = await ReadPageAsync(response);
        Assert.Equal(expectedIds, page.Items.Select(task => task.Id));

        using var nextResponse = await client.GetAsync($"/api/v1/me/tasks?{query}&page=2&pageSize=3");
        var nextPage = await ReadPageAsync(nextResponse);
        Assert.Equal(expectedIds.Length, nextPage.TotalCount);
        Assert.Equal(2, nextPage.Page);
        Assert.Equal(3, nextPage.PageSize);
        Assert.Equal(expectedIds.Skip(3).Take(3), nextPage.Items.Select(task => task.Id));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    public async Task List_PageBeyondEnd_ReturnsEmptyItemsAndOwnFilteredTotal(int pageNumber)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current);

        using var response = await client.GetAsync($"/api/v1/me/tasks?page={pageNumber}&pageSize=100");

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(Mine(sample, sample.Current).Count(), page.TotalCount);
        Assert.Equal(pageNumber, page.Page);
        Assert.Equal(100, page.PageSize);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public async Task List_UserWithMembershipsButNoAssignments_ReturnsEmptyPage(string role)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Creator, role);

        using var response = await client.GetAsync("/api/v1/me/tasks");

        var page = await ReadPageAsync(response);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Theory]
    [InlineData("status=done")]
    [InlineData("priority=1")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task List_InvalidFilters_StillUseSharedValidation(string query)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = CreateClient(factory, sample.Current);

        using var response = await client.GetAsync($"/api/v1/me/tasks?{query}");

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("missing-token", HttpStatusCode.Unauthorized)]
    [InlineData("invalid-token", HttpStatusCode.Unauthorized)]
    [InlineData("missing-role", HttpStatusCode.Forbidden)]
    [InlineData("inactive-token", HttpStatusCode.Unauthorized)]
    [InlineData("introspection-unavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task List_InvalidSession_DoesNotExposeTasksOrWrite(string session, HttpStatusCode status)
    {
        var sample = await SeedAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        if (session != "missing-token")
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                session == "invalid-token" ? "invalid-token" :
                factory.CreateToken(sample.Current.KeycloakSub, session == "missing-role" ? ["Other"] : ["User"]));
        if (session is "inactive-token" or "introspection-unavailable")
            factory.Introspection.Respond = _ => Task.FromResult(new HttpResponseMessage(
                session == "inactive-token" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            { Content = new StringContent("{\"active\":false}") });
        var before = await ReadSnapshotAsync();

        using var response = await client.GetAsync("/api/v1/me/tasks");

        await ReadProblemAsync(response, status);
        Assert.Equal(before, await ReadSnapshotAsync());
    }

    private async Task<Sample> SeedAsync()
    {
        await using var context = CreateContext();
        // Esta base de datos pertenece al contenedor temporal exclusivo de esta clase.
        await context.Projects.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
        var creator = new AppUser(Guid.NewGuid().ToString(), "Creadora", "creator@example.com");
        var current = new AppUser(Guid.NewGuid().ToString(), "Usuario actual", "current@example.com");
        var other = new AppUser(Guid.NewGuid().ToString(), "Otra persona", "other@example.com");
        context.Users.AddRange(creator, current, other);
        var active = new[] { new Project("Activo A", null, creator.Id), new Project("Activo B", null, creator.Id) };
        var foreign = new Project("Proyecto ajeno", null, creator.Id);
        var archived = new Project("Archivado con tarea propia", null, creator.Id);
        var empty = new Project("Activo vacío", null, creator.Id);
        archived.Archive();
        var projects = active.Concat(new[] { foreign, archived, empty }).ToArray();
        context.Projects.AddRange(projects);
        foreach (var project in projects)
        {
            context.ProjectMemberships.Add(new AppUserProject(project.Id, creator.Id));
            context.ProjectMemberships.Add(new AppUserProject(project.Id, other.Id));
            if (project.Id != foreign.Id)
                context.ProjectMemberships.Add(new AppUserProject(project.Id, current.Id));
        }
        var tasks = new DomainTask[27];
        var start = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        for (var index = 0; index < tasks.Length; index++)
        {
            var project = index == 25 ? foreign : index == 26 ? archived : active[index % 2];
            Guid? assigned = index == 25 ? other.Id : index == 26 ? current.Id : (index % 4) switch
            {
                3 => other.Id,
                2 => null,
                _ => current.Id
            };
            var task = new DomainTask(project.Id, $"Tarea {index}", (TaskPriority)((index / 3) % 3),
                index % 2 == 0 ? null : $"Descripción {index}", assigned, (TaskState)(index % 3),
                index % 5 == 0 ? null : new DateOnly(2026, 11, 10).AddDays((index / 2) % 3));
            context.Entry(task).Property(item => item.Id).CurrentValue =
                Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}");
            context.Entry(task).Property(item => item.CreatedAt).CurrentValue = start.AddHours(index / 3);
            context.Entry(task).Property(item => item.UpdatedAt).CurrentValue = task.CreatedAt.AddMinutes(1);
            tasks[index] = task;
        }
        context.Tasks.AddRange(tasks.Reverse());
        await context.SaveChangesAsync();
        return new Sample(creator, current, other, active, foreign, archived, empty, tasks);
    }

    private static IEnumerable<DomainTask> Mine(Sample sample, AppUser user) => sample.Tasks
        .Where(task => task.AssignedId == user.Id && task.ProjectId != sample.Archived.Id);

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

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            factory.CreateToken(user.KeycloakSub, [role]));
        return client;
    }

    private static HttpClient CreateClient(AuthenticationTestFactory factory, AppUser user) =>
        CreateClient(factory, user, "User");

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

    private sealed record Sample(AppUser Creator, AppUser Current, AppUser Other, Project[] Active,
        Project Foreign, Project Archived, Project Empty, DomainTask[] Tasks);
}
