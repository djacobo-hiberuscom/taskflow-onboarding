namespace TaskFlow.Api.Contracts.Projects;

public class ProjectPageResponse
{
    public ProjectResponse[] Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
