using TaskFlow.Api.Contracts.Users;

namespace TaskFlow.Api.Contracts.Projects;

public class ProjectResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public UserSummaryResponse? CreatedBy { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
