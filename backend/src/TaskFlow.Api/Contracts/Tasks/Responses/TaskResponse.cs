using TaskFlow.Api.Contracts.Users;

namespace TaskFlow.Api.Contracts.Tasks;

public class TaskResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string Priority { get; init; }
    public DateOnly? DueDate { get; init; }
    public UserSummaryResponse? Assignee { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }


}