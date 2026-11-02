using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    public async Task<Project?> GetVisibleByIdAsync(Guid projectId, Guid userId,
        bool isAdmin, CancellationToken cancellationToken)
    {
        var query = _context.Projects
            .Where(project => project.Id == projectId);

        if (!isAdmin)
        {
            query = query.Where(project => project.ArchivedAt == null);
            query = query.Where(project => _context.ProjectMemberships.Any(
                membership => membership.ProjectId == project.Id &&
                membership.UserId == userId));
        }
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AppUser[]> GetMembersAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        var query = _context.Users.Where(
            user => _context.ProjectMemberships.Any(
                membership => membership.UserId == user.Id && 
                membership.ProjectId == projectId));

        return await query.ToArrayAsync(cancellationToken);
    }
}