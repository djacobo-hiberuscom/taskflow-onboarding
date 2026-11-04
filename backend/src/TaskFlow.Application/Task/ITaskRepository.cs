using DomainTask = TaskFlow.Domain.Task;

namespace TaskFlow.Application.Tasks;

public interface ITaskRepository
{
    void Add(DomainTask task);

    Task<DomainTask?> GetByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    void Remove(DomainTask task);
}