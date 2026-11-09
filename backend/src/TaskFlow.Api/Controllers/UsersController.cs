using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Users;
using TaskFlow.Api.Contracts.Users;

namespace TaskFlow.Api.Controllers;


[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin, User")]
public class UsersController : ControllerBase
{
    private readonly LocalUserService _localUserService;

    public UsersController(LocalUserService localUserService)
    {
        _localUserService = localUserService;
    }

    [HttpGet("assignable")]
    public async Task<IActionResult> GetAssignable(
        CancellationToken cancellationToken)
    {
        var users = await _localUserService.GetAssignableAsync(cancellationToken);

        var response = users.Select(user => new UserSummaryResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName
        }).ToArray();

        return Ok(response);
    }
}