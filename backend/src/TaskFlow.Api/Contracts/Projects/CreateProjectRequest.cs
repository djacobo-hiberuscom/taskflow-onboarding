namespace TaskFlow.Api.Contracts.Projects;

public class CreateProjectRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}