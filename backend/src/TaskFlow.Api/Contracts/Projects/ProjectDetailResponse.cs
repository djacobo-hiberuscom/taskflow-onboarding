using TaskFlow.Api.Contracts.Users;

namespace TaskFlow.Api.Contracts.Projects;

public class ProjectDetailResponse : ProjectResponse
{
    public UserSummaryResponse [] Members { get; init; } = [];
}