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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;


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

    [HttpGet]
    [ProducesResponseType<ProjectPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken,
        [FromQuery, Range(1, int.MaxValue, ErrorMessage =
            "La página debe ser mayor o igual a 1.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage =
            "El tamaño de página debe estar entre 1 y 100.")] int pageSize = 20,
        [FromQuery] bool archived = false)
    {
        if(archived && !_currentUser.IsAdmin)
        {
            return Forbid();
        }

        var projectPage = await _projectService.GetProjectsAsync(
            _currentUser.UserId, _currentUser.IsAdmin, archived,
            page, pageSize, cancellationToken);

        // Obtener juntos los creadores de los proyectos de esta página.
        var creatorIds = projectPage.Items.Select(project => project.CreatedBy)
            .OfType<Guid>().Distinct().ToArray();
        var creators = await _localUserService.GetByIdsAsync(creatorIds, cancellationToken);
        var creatorsById = creators.ToDictionary(creator => creator.Id);

        var projectResponses = projectPage.Items.Select(project =>
        {
            UserSummaryResponse? creatorResponse = null;
            if (project.CreatedBy.HasValue &&
                creatorsById.TryGetValue(project.CreatedBy.Value, out var creator))
            {
                creatorResponse = new UserSummaryResponse
                {
                    Id = creator.Id,
                    DisplayName = creator.DisplayName
                };
            }

            return new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedBy = creatorResponse,
                ArchivedAt = project.ArchivedAt,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };
        }).ToArray();

        return Ok(new ProjectPageResponse
        {
            Items = projectResponses,
            Page = projectPage.Page,
            PageSize = projectPage.PageSize,
            TotalCount = projectPage.TotalCount
        });
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

    [HttpPut("{projectId}")]
    public async Task<IActionResult> Update(
        Guid projectId, 
        [FromBody] UpdateProjectRequest request, 
        CancellationToken cancellationToken)
    {
        var project = await _projectService.UpdateProjectAsync(
            projectId, request.Name, request.Description,
            _currentUser.UserId, _currentUser.IsAdmin, cancellationToken);
        
        if(project == null)
        {
            return NotFound();
        }
        
        UserSummaryResponse? creatorResponse = null;
        if (project.CreatedBy.HasValue)
        {
            var creator = await _localUserService.GetByIdAsync(
                project.CreatedBy.Value, cancellationToken);
            
            if(creator is not null)
            {
                creatorResponse = new UserSummaryResponse
                {
                    Id = creator.Id,
                    DisplayName = creator.DisplayName
                };
            }
        }

        var response = new ProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = creatorResponse,
            ArchivedAt = project.ArchivedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return Ok(response);
    }

    [HttpPost("{projectId}/archive")]
    public async Task<IActionResult> Archive(
        Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _projectService.ArchiveProjectAsync(
            projectId, _currentUser.UserId, _currentUser.IsAdmin,
            cancellationToken);

        switch (result)
        {
            case ProjectOperationResult.Success:
                return NoContent();
            
            case ProjectOperationResult.NotFound:
                return NotFound();
            
            case ProjectOperationResult.Forbidden:
                return Forbid();
            
            default:
                throw new InvalidOperationException(
                    "Resultado de archivado no reconocido.");
        }
    }

    [HttpPost("{projectId}/restore")]
    public async Task<IActionResult> Restore(
        Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _projectService.RestoreProjectAsync(
            projectId, _currentUser.UserId, _currentUser.IsAdmin,
            cancellationToken);

        switch (result)
        {
            case ProjectOperationResult.Success:
                return NoContent();
            
            case ProjectOperationResult.NotFound:
                return NotFound();
            
            case ProjectOperationResult.Forbidden:
                return Forbid();
            
            default:
                throw new InvalidOperationException(
                    "Resultado de desarchivado no reconocido.");
        }
    }

    [HttpDelete("{projectId}")]
    public async Task<IActionResult> Delete(
        Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _projectService.DeleteProjectAsync(
            projectId, _currentUser.UserId, _currentUser.IsAdmin,
            cancellationToken);

        switch (result)
        {
            case ProjectOperationResult.Success:
                return NoContent();
            
            case ProjectOperationResult.NotFound:
                return NotFound();
            
            case ProjectOperationResult.Forbidden:
                return Forbid();
            
            default:
                throw new InvalidOperationException(
                    "Resultado de borrado no reconocido.");
        }
    }
    

}
