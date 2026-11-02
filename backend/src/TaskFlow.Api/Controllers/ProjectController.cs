//debera usar prefijo /v1
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Projects;
using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Abstractions;
using TaskFlow.Api.Contracts.Projects;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Domain;
using TaskFlow.Application.Users;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;
using System.IO.Compression;


namespace TaskFlow.Api.Controllers;


[ApiController]
[Route("api/v1/projects")]
[Authorize(Roles = "Admin,User")]
public class ProjectController : ControllerBase
{
    private readonly ProjectService _projectService;
    private readonly ICurrentUser _currentUser;
    private readonly LocalUserService _localUserService;

    public ProjectController(ProjectService projectService,
        ICurrentUser currentUser, LocalUserService localUserService)
    {
        _projectService = projectService;
        _currentUser = currentUser;
        _localUserService = localUserService;
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

    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetById(Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetProjectByIdAsync(projectId,
            _currentUser.UserId, _currentUser.IsAdmin,
            cancellationToken);
        
        if(result == null)
        {
            return NotFound();
        }

        AppUser? creator = null;

        var members = await _projectService.GetMembersAsync(
            projectId, cancellationToken);
        
        var membersResponse = members.Select(member => new UserSummaryResponse
        {
            Id = member.Id,
            DisplayName = member.DisplayName
        }).ToArray();

        if(result.CreatedBy != null)
        {
            creator = await _localUserService.GetByIdAsync(
                result.CreatedBy.Value, cancellationToken);
        }

        UserSummaryResponse? creatorResponse = null;
        if (creator != null)
        {
            creatorResponse = new UserSummaryResponse
            {
                Id = creator.Id,
                DisplayName = creator.DisplayName
            };
        }

        return Ok(new ProjectDetailResponse
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            CreatedBy = creatorResponse,
            ArchivedAt = result.ArchivedAt,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt,
            Members = membersResponse

        });
    }
    

}
