//debera usar prefijo /v1
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Api.Controllers;


[ApiController]
[Route("api/v1/projects")]
[Authorize(Roles = "Admin,User")]
public class ProjectController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly ICurrentUser _currentUser;

    public ProjectController(ProjectService projectService, ICurrentUser currentUser)
    {
        _projectService = projectService;
        _currentUser = currentUser;
    }

}
