using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authentication;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Application.Users;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController(CurrentUser currentUser, LocalUserService users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CurrentUserResponse>> Get(CancellationToken cancellationToken)
    {
        var user = currentUser.User;
        await users.SynchronizeProfileAsync(user,
            User.FindFirst("name")?.Value,
            User.FindFirst("preferred_username")?.Value,
            User.FindFirst("email")?.Value,
            cancellationToken);

        var roles = new[] { "Admin", "User" }.Where(User.IsInRole).ToArray();
        return Ok(new CurrentUserResponse(user.Id, user.DisplayName, user.Email, roles));
    }
}
