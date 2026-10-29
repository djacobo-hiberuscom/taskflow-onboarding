using TaskFlow.Application.Projects;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly TaskFlowDbContext _context;

    public ProjectRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public void Add(Project project)
    {
        _context.Projects.Add(project);
    }

    public void AddMembership(AppUserProject membership)
    {
        _context.ProjectMemberships.Add(membership);
    }
}