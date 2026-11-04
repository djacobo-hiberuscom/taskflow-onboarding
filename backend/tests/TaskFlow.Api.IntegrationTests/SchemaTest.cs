using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Task;
using Task = System.Threading.Tasks.Task;

namespace TaskFlow.Api.IntegrationTests;

public class SchemaTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public SchemaTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Task_RejectsAssigneeWhoIsNotMember()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var user = CreateUser("Luis");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Tasks.Add(new DomainTask(
            project.Id, "Tarea de prueba", TaskPriority.Medium, assignedId: user.Id));

        await AssertUpdateFailureAsync(
            () => context.SaveChangesAsync(),
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_task_app_user_project_assignment");
    }

    [Fact]
    public async Task Task_RejectsAssigneeWhoOnlyBelongsToAnotherProject()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var (otherOwner, _) = await SeedProjectAsync(context);

        context.Tasks.Add(new DomainTask(
            project.Id, "Tarea de prueba", TaskPriority.Medium, assignedId: otherOwner.Id));

        await AssertUpdateFailureAsync(
            () => context.SaveChangesAsync(),
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_task_app_user_project_assignment");
    }

    [Theory]
    [InlineData(TaskState.ToDo, TaskPriority.Low)]
    [InlineData(TaskState.InProgress, TaskPriority.Medium)]
    [InlineData(TaskState.Done, TaskPriority.High)]
    public async Task Task_AcceptsAssigneeWhoIsMember(TaskState status, TaskPriority priority)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var user = CreateUser("Luis");
        var description = new string('d', 5000);
        project.UpdateDetails(new string('p', 150), description);
        context.Users.Add(user);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, user.Id));
        await context.SaveChangesAsync();

        var dueDate = new DateOnly(2020, 1, 1);
        var task = new DomainTask(
            project.Id, new string('t', 200), priority, description, user.Id, status, dueDate);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        // Recuperar objetos nuevos comprueba lo guardado, no las instancias de preparación.
        context.ChangeTracker.Clear();
        var savedTask = await context.Tasks.SingleAsync(item => item.Id == task.Id);
        var savedProject = await context.Projects.SingleAsync(item => item.Id == project.Id);

        Assert.Equal(project.Id, savedTask.ProjectId);
        Assert.Equal(user.Id, savedTask.AssignedId);
        Assert.Equal(task.Title, savedTask.Title);
        Assert.Equal(description, savedTask.Description);
        Assert.Equal(status, savedTask.Status);
        Assert.Equal(priority, savedTask.Priority);
        Assert.Equal(dueDate, savedTask.DueDate);
        Assert.Equal(project.Name, savedProject.Name);
        Assert.Equal(description, savedProject.Description);
    }

    [Fact]
    public async Task Task_AcceptsNoAssigneeAndOptionalFields()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var task = new DomainTask(project.Id, "Sin asignar", TaskPriority.Low);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var savedTask = await context.Tasks.SingleAsync(item => item.Id == task.Id);

        Assert.Equal(project.Id, savedTask.ProjectId);
        Assert.Null(savedTask.AssignedId);
        Assert.Null(savedTask.Description);
        Assert.Null(savedTask.DueDate);
        Assert.Equal(TaskState.ToDo, savedTask.Status);
        Assert.Equal(TaskPriority.Low, savedTask.Priority);
    }

    [Fact]
    public async Task Task_RejectsUnknownProject()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Tasks.Add(new DomainTask(Guid.NewGuid(), "Sin proyecto", TaskPriority.Low));

        await AssertUpdateFailureAsync(
            () => context.SaveChangesAsync(),
            PostgresErrorCodes.ForeignKeyViolation,
            "FK_task_project_project_id");
    }

    [Fact]
    public async Task Membership_RejectsDuplicateProjectAndUser()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (owner, project) = await SeedProjectAsync(context);

        // Evitar el conflicto del seguimiento de EF para probar la restricción de PostgreSQL.
        context.ChangeTracker.Clear();
        context.ProjectMemberships.Add(new AppUserProject(project.Id, owner.Id));

        await AssertUpdateFailureAsync(
            () => context.SaveChangesAsync(),
            PostgresErrorCodes.UniqueViolation,
            "PK_app_user_project");
    }

    [Fact]
    public async Task User_RejectsDuplicateKeycloakSub()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = CreateUser("Ana");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Users.Add(new AppUser(user.KeycloakSub, "Otra referencia", null));

        await AssertUpdateFailureAsync(
            () => context.SaveChangesAsync(),
            PostgresErrorCodes.UniqueViolation,
            "IX_app_user_keycloak_sub");
    }

    [Fact]
    public async Task DeletingProject_DeletesTasksAndMembershipsButPreservesOtherData()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (owner, project) = await SeedProjectAsync(context);
        var (_, otherProject) = await SeedProjectAsync(context);
        var otherTask = new DomainTask(otherProject.Id, "Conservar", TaskPriority.Low);
        context.Tasks.AddRange(
            new DomainTask(project.Id, "Asignada", TaskPriority.High, assignedId: owner.Id),
            new DomainTask(project.Id, "Sin asignar", TaskPriority.Low),
            otherTask);
        await context.SaveChangesAsync();

        // ExecuteDelete envía el DELETE directamente: las cascadas las ejecuta PostgreSQL.
        var deleted = await context.Projects.Where(item => item.Id == project.Id).ExecuteDeleteAsync();

        Assert.Equal(1, deleted);
        Assert.False(await context.Projects.AnyAsync(item => item.Id == project.Id));
        Assert.False(await context.Tasks.AnyAsync(item => item.ProjectId == project.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(item => item.ProjectId == project.Id));
        Assert.True(await context.Users.AnyAsync(item => item.Id == owner.Id));
        Assert.True(await context.Projects.AnyAsync(item => item.Id == otherProject.Id));
        Assert.True(await context.Tasks.AnyAsync(item => item.Id == otherTask.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(item => item.ProjectId == otherProject.Id));
    }

    [Fact]
    public async Task DeletingMembership_UnassignsOnlyItsTasksAndPreservesProject()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (owner, project) = await SeedProjectAsync(context);
        var user = CreateUser("Luis");
        context.Users.Add(user);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, user.Id));
        var assignedTask = new DomainTask(project.Id, "De Luis", TaskPriority.Medium, assignedId: user.Id);
        var ownerTask = new DomainTask(project.Id, "De Ana", TaskPriority.High, assignedId: owner.Id);
        var unassignedTask = new DomainTask(project.Id, "Sin asignar", TaskPriority.Low);
        context.Tasks.AddRange(assignedTask, ownerTask, unassignedTask);
        await context.SaveChangesAsync();

        var deleted = await context.ProjectMemberships
            .Where(item => item.ProjectId == project.Id && item.UserId == user.Id)
            .ExecuteDeleteAsync();

        context.ChangeTracker.Clear();
        var savedTask = await context.Tasks.SingleAsync(item => item.Id == assignedTask.Id);
        var savedOwnerTask = await context.Tasks.SingleAsync(item => item.Id == ownerTask.Id);

        Assert.Equal(1, deleted);
        Assert.Null(savedTask.AssignedId);
        Assert.Equal(project.Id, savedTask.ProjectId);
        Assert.Equal(owner.Id, savedOwnerTask.AssignedId);
        Assert.Equal(3, await context.Tasks.CountAsync(item => item.ProjectId == project.Id));
        Assert.True(await context.Projects.AnyAsync(item => item.Id == project.Id));
        Assert.True(await context.Users.AnyAsync(item => item.Id == user.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(
            item => item.ProjectId == project.Id && item.UserId == owner.Id));
        Assert.False(await context.ProjectMemberships.AnyAsync(
            item => item.ProjectId == project.Id && item.UserId == user.Id));
    }

    [Fact]
    public async Task DeletingUser_PreservesProjectAndTasksAndClearsTheirReferences()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (owner, project) = await SeedProjectAsync(context);
        var remainingMember = CreateUser("Luis");
        context.Users.Add(remainingMember);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, remainingMember.Id));
        var ownerTask = new DomainTask(project.Id, "De Ana", TaskPriority.High, assignedId: owner.Id);
        var otherTask = new DomainTask(project.Id, "De Luis", TaskPriority.Low, assignedId: remainingMember.Id);
        context.Tasks.AddRange(ownerTask, otherTask);
        await context.SaveChangesAsync();

        var deleted = await context.Users.Where(item => item.Id == owner.Id).ExecuteDeleteAsync();

        context.ChangeTracker.Clear();
        var savedProject = await context.Projects.SingleAsync(item => item.Id == project.Id);
        var savedOwnerTask = await context.Tasks.SingleAsync(item => item.Id == ownerTask.Id);
        var savedOtherTask = await context.Tasks.SingleAsync(item => item.Id == otherTask.Id);

        Assert.Equal(1, deleted);
        Assert.Null(savedProject.CreatedBy);
        Assert.Null(savedOwnerTask.AssignedId);
        Assert.Equal(project.Id, savedOwnerTask.ProjectId);
        Assert.Equal(remainingMember.Id, savedOtherTask.AssignedId);
        Assert.False(await context.ProjectMemberships.AnyAsync(item => item.UserId == owner.Id));
        Assert.True(await context.ProjectMemberships.AnyAsync(
            item => item.ProjectId == project.Id && item.UserId == remainingMember.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task Task_RejectsBlankTitleInDatabase(string title)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var task = await SeedTaskAsync(context, project.Id);

        // SQL parametrizado permite comprobar el CHECK sin pasar por el constructor.
        await AssertSqlFailureAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE task SET title = {title} WHERE id = {task.Id}"),
            "ck_task_title_not_empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task Project_RejectsBlankNameInDatabase(string name)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);

        await AssertSqlFailureAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE project SET name = {name} WHERE id = {project.Id}"),
            "ck_project_name_not_empty");
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("todo")]
    [InlineData("1")]
    public async Task Task_RejectsUnknownStatusInDatabase(string status)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var task = await SeedTaskAsync(context, project.Id);

        await AssertSqlFailureAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE task SET status = {status} WHERE id = {task.Id}"),
            "ck_task_status");
    }

    [Theory]
    [InlineData("Urgent")]
    [InlineData("high")]
    [InlineData("1")]
    public async Task Task_RejectsUnknownPriorityInDatabase(string priority)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var task = await SeedTaskAsync(context, project.Id);

        await AssertSqlFailureAsync(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE task SET priority = {priority} WHERE id = {task.Id}"),
            "ck_task_priority");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Description_RejectsMoreThan5000CharactersInDatabase(bool taskDescription)
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var (_, project) = await SeedProjectAsync(context);
        var task = await SeedTaskAsync(context, project.Id);
        var description = new string('d', 5001);

        if (taskDescription)
        {
            await AssertSqlFailureAsync(
                () => context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE task SET description = {description} WHERE id = {task.Id}"),
                "ck_task_description_length");
        }
        else
        {
            await AssertSqlFailureAsync(
                () => context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE project SET description = {description} WHERE id = {project.Id}"),
                "ck_project_description_length");
        }
    }

    private TaskFlowDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(_postgres.Database.GetConnectionString())
            .Options;

        return new TaskFlowDbContext(options);
    }

    private static AppUser CreateUser(string name)
    {
        return new AppUser(Guid.NewGuid().ToString(), name, null);
    }

    private static async Task<(AppUser Owner, Project Project)> SeedProjectAsync(TaskFlowDbContext context)
    {
        var owner = CreateUser("Ana");
        var project = new Project("Proyecto de Ana", null, owner.Id);
        context.Users.Add(owner);
        context.Projects.Add(project);
        context.ProjectMemberships.Add(new AppUserProject(project.Id, owner.Id));
        await context.SaveChangesAsync();

        return (owner, project);
    }

    private static async Task<DomainTask> SeedTaskAsync(TaskFlowDbContext context, Guid projectId)
    {
        var task = new DomainTask(projectId, "Tarea de prueba", TaskPriority.Medium);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return task;
    }

    private static async Task AssertUpdateFailureAsync(
        Func<Task> operation, string sqlState, string constraintName)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(operation);
        var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(sqlState, postgresError.SqlState);
        Assert.Equal(constraintName, postgresError.ConstraintName);
    }

    private static async Task AssertSqlFailureAsync(Func<Task> operation, string constraintName)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(operation);
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraintName, exception.ConstraintName);
    }
}
