using TaskFlow.Api.Contracts.Tasks;

namespace TaskFlow.Api.Contracts.Tasks;

public class TaskPageResponse
{
    public TaskResponse[] Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
