using System.Globalization;
using TaskFlow.Domain;
using DomainTask = TaskFlow.Domain.Task;

namespace TaskFlow.Domain.Test;

public class TaskTests
{
    [Fact]
    public void UpdateDetails_NormalizesFieldsAndPreservesIdentityAssignmentAndStatus()
    {
        var task = CreateTask();
        var id = task.Id;
        var projectId = task.ProjectId;
        var assignedId = task.AssignedId;
        var status = task.Status;
        var createdAt = task.CreatedAt;
        var dueDate = new DateOnly(2030, 4, 12);
        var before = DateTimeOffset.UtcNow;

        task.UpdateDetails("  Título actualizado  ", "  Descripción actualizada  ", TaskPriority.High, dueDate);

        Assert.Equal("Título actualizado", task.Title);
        Assert.Equal("Descripción actualizada", task.Description);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(dueDate, task.DueDate);
        Assert.Equal(id, task.Id);
        Assert.Equal(projectId, task.ProjectId);
        Assert.Equal(assignedId, task.AssignedId);
        Assert.Equal(status, task.Status);
        Assert.Equal(createdAt, task.CreatedAt);
        Assert.InRange(task.UpdatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, task.UpdatedAt.Offset);
    }

    [Fact]
    public void UpdateDetails_AcceptsMaximumLengthsAfterTrimming()
    {
        var task = CreateTask();
        var title = new string('t', 200);
        var description = new string('d', 5000);

        task.UpdateDetails($"  {title}  ", $" \t{description}\r\n ", TaskPriority.Low, null);

        Assert.Equal(title, task.Title);
        Assert.Equal(description, task.Description);
        Assert.Equal(TaskPriority.Low, task.Priority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void UpdateDetails_EmptyDescription_ClearsPreviousValue(string? description)
    {
        var task = CreateTask();

        task.UpdateDetails("Título válido", description, TaskPriority.Medium, null);

        Assert.Null(task.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2000-01-01")]
    [InlineData("2099-12-31")]
    public void UpdateDetails_AcceptsNullPastAndFutureDueDates(string? date)
    {
        var task = CreateTask();
        DateOnly? dueDate = date is null ? null :
            DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        task.UpdateDetails("Título válido", null, TaskPriority.Medium, dueDate);

        Assert.Equal(dueDate, task.DueDate);
    }

    [Theory]
    [InlineData("null-title", "title")]
    [InlineData("empty-title", "title")]
    [InlineData("whitespace-title", "title")]
    [InlineData("long-title", "title")]
    [InlineData("long-description", "description")]
    [InlineData("negative-priority", "priority")]
    [InlineData("unknown-priority", "priority")]
    public void UpdateDetails_InvalidInput_ThrowsWithoutChangingAnyProperty(string scenario, string parameter)
    {
        var task = CreateTask();
        var before = Snapshot(task);
        var title = scenario switch
        {
            "null-title" => null!,
            "empty-title" => "",
            "whitespace-title" => " \t\r\n ",
            "long-title" => new string('t', 201),
            _ => "Otro título"
        };
        var description = scenario == "long-description" ? new string('d', 5001) : "Otra descripción";
        var priority = scenario switch
        {
            "negative-priority" => (TaskPriority)(-1),
            "unknown-priority" => (TaskPriority)99,
            _ => TaskPriority.High
        };

        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            task.UpdateDetails(title, description, priority, new DateOnly(2030, 1, 1)));

        Assert.Equal(parameter, exception.ParamName);
        Assert.Equal(before, Snapshot(task));
    }

    private static DomainTask CreateTask() => new(
        Guid.NewGuid(), "Título inicial", TaskPriority.Medium, "Descripción inicial",
        Guid.NewGuid(), TaskState.InProgress, new DateOnly(2026, 1, 1));

    private static object Snapshot(DomainTask task) => new
    {
        task.Id, task.ProjectId, task.AssignedId, task.Title, task.Description,
        task.Status, task.Priority, task.DueDate, task.CreatedAt, task.UpdatedAt
    };
}
