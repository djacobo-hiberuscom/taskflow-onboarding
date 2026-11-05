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
}