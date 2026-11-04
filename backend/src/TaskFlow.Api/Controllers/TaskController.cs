//debera usar prefijo /v1
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Authorize(Roles = "Admin, User")]
public class TaskController : ControllerBase
{
    private readonly TaskService _taskService;
    private readonly ICurrentUser _currentUser;
    private readonly LocalUserService _localUserService;

    public TaskController(
        TaskService taskService, ICurrentUser currentUser,
        LocalUserService localUserService)
    {
        _taskService = taskService;
        _currentUser = currentUser;
        _localUserService = localUserService;
    }

    [HttpGet("{taskId}")]
    public async Task<IActionResult> GetById(
        Guid taskId, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTaskByIdAsync(
            taskId, _currentUser.UserId, _currentUser.IsAdmin,
            cancellationToken);

        if(result is null)
        {
            return NotFound();
        }

        var task = result.Value.Task;
        var projectName = result.Value.ProjectName;

        UserSummaryResponse? assignedResponse = null;

        if(task.AssignedId != null)
        {
            var assignee = await _localUserService.GetByIdAsync(
                task.AssignedId.Value, cancellationToken);
            
            if(assignee is not null)
            {
                assignedResponse = new UserSummaryResponse
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName
                };
            }
        }
        
        return Ok(new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            Assignee = assignedResponse,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        });
    }

/*     [HttpPost("/api/v1/projects/{projectId}/tasks")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        
    } */
    
}