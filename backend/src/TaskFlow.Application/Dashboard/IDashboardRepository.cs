namespace TaskFlow.Application.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardCounts> GetCountsAsync(
        Guid userId, bool isAdmin, DateOnly businessDate,
        CancellationToken cancellationToken);
}