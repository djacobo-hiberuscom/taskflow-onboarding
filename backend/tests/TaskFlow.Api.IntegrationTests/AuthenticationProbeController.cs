using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Api.IntegrationTests;

// Solo se incorpora al servidor de pruebas, nunca a la API real.
[ApiController]
[Authorize]
[Route("__tests/current-user")]
public class AuthenticationProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { currentUser.UserId, currentUser.IsAdmin });

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin() => NoContent();
}
