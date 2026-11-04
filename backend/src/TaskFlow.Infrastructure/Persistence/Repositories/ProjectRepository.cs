using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
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

    public async System.Threading.Tasks.Task EnsureMembershipAsync(
        Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if(_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "El alta de pertenencia requiere una transacción activa.");
        }

        var membership = new AppUserProject(projectId, userId);

        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app_user_project (project_id, user_id, created_at)
            VALUES ({membership.ProjectId}, {membership.UserId}, {membership.CreatedAt})
            ON CONFLICT (project_id, user_id) DO NOTHING
            """, cancellationToken);
    }

    public async Task<ProjectPage> GetPageAsync(
        Guid? memberUserId, bool archived, int page, 
        int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsQueryable();
        if(archived == true)
        {
            query = query.Where(project => project.ArchivedAt != null);
        }
        else
        {
            query = query.Where(project => project.ArchivedAt == null);
        }

        if(memberUserId != null)
        {
            query = query.Where(project => 
                _context.ProjectMemberships.Any(membership => 
                membership.ProjectId == project.Id &&
                membership.UserId == memberUserId.Value));
        }

        var TotalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = query
            .OrderByDescending(project => project.CreatedAt)
            .ThenBy(project => project.Id);

        var skip = ((long)page - 1) * pageSize;
        Project[] items = [];
        if(skip < TotalCount)
        {
            items = await orderedQuery
                .Skip((int)skip)
                .Take(pageSize)
                .ToArrayAsync(cancellationToken);
        }

        return new ProjectPage
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = TotalCount
        };
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

    public async Task<Project?> GetByIdForUpdateAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        if(_context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException(
                "El bloqueo del proyecto requiere una transaccion activa.");
        }

        var projects =  await _context.Projects
            .FromSql($"SELECT * FROM project WHERE id = {projectId} FOR UPDATE")
            .ToArrayAsync(cancellationToken);

        return projects.SingleOrDefault();
    }

    public void Remove(Project project)
    {
        _context.Projects.Remove(project);
    }
}