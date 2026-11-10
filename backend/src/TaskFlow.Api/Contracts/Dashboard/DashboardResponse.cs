namespace TaskFlow.Api.Contracts.Dashboard;

public class DashboardResponse
{
    public int ActiveProjects { get; init; }
    public int OpenTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OverdueTasks { get; init; }
    public DateOnly BusinessDate { get; init; }
    public required string BusinessTimeZone { get; init;}
    
}