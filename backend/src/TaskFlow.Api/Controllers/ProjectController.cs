//debera usar prefijo /v1
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Abstractions;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Contracts.Users;


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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _projectService.CreateProjectAsync(request.Name,
            request.Description, _currentUser.UserId, cancellationToken);

        var response = new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = new UserSummaryResponse
            {
                Id = _currentUser.UserId,
                DisplayName = _currentUser.DisplayName
            },
            ArchivedAt = project.ArchivedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
        return Created($"/api/v1/projects/{project.Id}", response);
    }

}
