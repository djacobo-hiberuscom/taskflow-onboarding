using System.ComponentModel;
using TaskFlow.Domain;
namespace TaskFlow.Application.Projects;

public class ProjectPage
{
    public Project[] Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}