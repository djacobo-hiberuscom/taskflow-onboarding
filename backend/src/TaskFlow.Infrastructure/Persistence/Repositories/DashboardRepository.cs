using TaskFlow.Application.Dashboard;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly TaskFlowDbContext _context;

    public DashboardRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardCounts> GetCountsAsync(
        Guid userId, bool isAdmin, DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        //sleccionar solo proyectos activos
        var visibleProjects = _context.Projects
            .Where(project => project.ArchivedAt == null);

        if (!isAdmin)
        {
            //si no es admin seleccionar solo proyectos que le pertenezcan
            visibleProjects = visibleProjects.Where(project =>
                _context.ProjectMemberships.Any(membership =>
                membership.ProjectId == project.Id &&
                membership.UserId == userId));
        }

        //consulta de tareas
        var visibleTasks = _context.Tasks.Where(task =>
            visibleProjects.Any(project =>
            project.Id == task.ProjectId));
        
        var activeProjects = await visibleProjects.CountAsync(cancellationToken);

        var openTasks = await visibleTasks.CountAsync(task =>
            task.Status == TaskState.ToDo ||
            task.Status == TaskState.InProgress,
            cancellationToken);
        
        var completedTasks = await visibleTasks.CountAsync(task =>
            task.Status == TaskState.Done,
            cancellationToken);

        var overdueTasks = await visibleTasks.CountAsync(task =>
            task.DueDate.HasValue &&
            task.DueDate.Value < businessDate &&
            task.Status != TaskState.Done,
            cancellationToken);

        return new DashboardCounts
        {
            ActiveProjects = activeProjects,
            OpenTasks = openTasks,
            CompletedTasks = completedTasks,
            OverdueTasks = overdueTasks
        };
    }
}