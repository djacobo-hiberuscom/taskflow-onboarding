using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/ping")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new {message = "TaskFlow API funciona"});
    }

    [HttpGet("saludo")]
    public IActionResult GetName([FromQuery] string nombre)
    {
        return Ok(new {message = $"Hola {nombre}"});
    }
}
