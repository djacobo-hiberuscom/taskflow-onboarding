using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _context;

    public TaskRepository(TaskFlowDbContext context)
    {
        _context = context;
    }


    public void Add(Domain.Task task)
    {
        _context.Tasks.Add(task);
    }

    public void Remove(Domain.Task task)
    {
        _context.Tasks.Remove(task);
    }

    public async Task<Domain.Task?> GetByIdAsync(
        Guid taskId, CancellationToken cancellationToken)
    {
        return await _context.Tasks.SingleOrDefaultAsync(
            task => task.Id == taskId,
            cancellationToken);
    }

    public async Task<Guid?> GetProjectIdAsync(
        Guid taskId, CancellationToken cancellationToken)
    {
        return await _context.Tasks
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskPage> GetPageAsync(
        TaskListFilter filter, Guid userId, bool isAdmin,
        CancellationToken cancellationToken)
    {
        var visibleProjects = _context.Projects.AsQueryable();

        if(filter.OnlyActiveProjects
            || !isAdmin
            || !filter.ProjectId.HasValue)
        {
            visibleProjects = visibleProjects
                .Where(project => project.ArchivedAt == null);
        }

        if (!isAdmin)
        {
            visibleProjects = visibleProjects.Where(project =>
                _context.ProjectMemberships.Any(membership =>
                    membership.ProjectId == project.Id &&
                    membership.UserId == userId));
        }

        var query = _context.Tasks
        .AsNoTracking()
        .Where(task => visibleProjects.Any(project =>
            project.Id == task.ProjectId));

        //si filtramos por proyecto 
        if (filter.ProjectId.HasValue)
        {
            query = query.Where(task =>
                task.ProjectId == filter.ProjectId.Value);
        }

        //si filtramos por estado
        if(filter.Status.Length > 0)
        {
            query = query.Where(task =>
                filter.Status.Contains(task.Status));
        }

        //si filtramos por prioridad
        if(filter.Priority.Length > 0)
        {
            query =  query.Where(task =>
                filter.Priority.Contains(task.Priority));
        }

        //buscar tareas sin asignado o asignadas a un GUID,
        // si no se ha puesto este filtro saltar
        if (filter.OnlyUnassigned)
        {
            query = query.Where(task =>
                task.AssignedId == null);
        }
        else if(filter.AssigneeId.HasValue)
        {
            query = query.Where(task =>
                task.AssignedId == filter.AssigneeId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var order = filter.Order
            ?? (filter.Sort == "dueDate" ? "asc" : "desc");

        IOrderedQueryable<Domain.Task> orderedQuery;

        if(filter.Sort == "dueDate")
        {
            var nullsLast = query.OrderBy(task => task.DueDate == null);

            orderedQuery = order == "asc"
                ? nullsLast.ThenBy(task => task.DueDate)
                : nullsLast.ThenByDescending(task => task.DueDate);
            
            orderedQuery = orderedQuery
                .ThenByDescending(task => task.CreatedAt)
                .ThenBy(task => task.Id);
        }
        else
        {
            orderedQuery = order == "asc"
                ? query.OrderBy(task => task.CreatedAt)
                : query.OrderByDescending(task => task.CreatedAt);

            orderedQuery = orderedQuery.ThenBy(task => task.Id);
        }

        var skip = ((long)filter.Page - 1) * filter.PageSize;

        Domain.Task[] items = [];

        if(skip < totalCount)
        {
            items = await orderedQuery
                .Skip((int)skip)
                .Take(filter.PageSize)
                .ToArrayAsync(cancellationToken);
        }

        return new TaskPage
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }
}