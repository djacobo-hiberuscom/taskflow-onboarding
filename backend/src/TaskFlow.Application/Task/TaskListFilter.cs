namespace TaskFlow.Application.Tasks;
using TaskFlow.Domain;

public class TaskListFilter
{
    public Guid? ProjectId { get; init; }
    public TaskState[] Status { get; init; } = [];
    public TaskPriority[] Priority { get; init; } = [];
    public Guid? AssigneeId { get; init; }
    public bool OnlyUnassigned { get; init; } = false;
    public string Sort { get; init; } = "createdAt";
    public string? Order { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool OnlyActiveProjects { get; init; } = false;

}
