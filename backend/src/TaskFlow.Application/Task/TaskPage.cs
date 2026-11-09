using DomainTask = TaskFlow.Domain.Task;

namespace TaskFlow.Application.Tasks;

public class TaskPage
{
    public DomainTask[] Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}