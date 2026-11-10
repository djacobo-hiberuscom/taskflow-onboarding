namespace TaskFlow.Application.Dashboard;

public class DashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly TimeProvider _timeProvider;

    public DashboardService(IDashboardRepository dashboardRepository,
        TimeProvider timeProvider)
    {
        _dashboardRepository = dashboardRepository;
        _timeProvider = timeProvider;
    }

    public async Task<(
        DashboardCounts Counts,
        DateOnly BusinessDate,
        string BusinessTimeZone)> GetDashboardAsync(
            Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        const string businessTimeZone = "Europe/Madrid";

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(businessTimeZone);

        var utcNow = _timeProvider.GetUtcNow();

        var madridNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);

        var businessDate = DateOnly.FromDateTime(madridNow.DateTime);

        var counts = await _dashboardRepository.GetCountsAsync(
            userId, isAdmin, businessDate, cancellationToken);

        return (counts, businessDate, businessTimeZone);
    }
 
}