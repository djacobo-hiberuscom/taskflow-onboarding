using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Contracts.Dashboard;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Dashboard;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Admin, User")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly ICurrentUser _currentUser;

    public DashboardController(
        DashboardService dashboardService, ICurrentUser currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DashboardResponse>> Get(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardAsync(
            _currentUser.UserId, _currentUser.IsAdmin, cancellationToken);
        
        return Ok(new DashboardResponse
        {
            ActiveProjects = result.Counts.ActiveProjects,
            OpenTasks = result.Counts.OpenTasks,
            CompletedTasks = result.Counts.CompletedTasks,
            OverdueTasks = result.Counts.OverdueTasks,
            BusinessDate = result.BusinessDate,
            BusinessTimeZone = result.BusinessTimeZone
        });
    }
}