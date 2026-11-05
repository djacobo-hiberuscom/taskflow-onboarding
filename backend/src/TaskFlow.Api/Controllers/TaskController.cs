//debera usar prefijo /v1
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using TaskFlow.Api.Contracts.Tasks;
using TaskFlow.Api.Contracts.Users;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using TaskFlow.Domain;

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

    [HttpPost("/api/v1/projects/{projectId}/tasks")]
    public async Task<IActionResult> Create(
        Guid projectId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var priority = Enum.Parse<TaskPriority>(request.Priority);
        var status = Enum.Parse<TaskState>(request.Status);

        var result = await _taskService.CreateTaskAsync(
            projectId, request.Title, request.Description, 
            priority, status, request.DueDate, 
            request.AssigneeId, _currentUser.UserId, _currentUser.IsAdmin, 
            cancellationToken);

        if(result.Result == TaskOperationResult.NotFound)
        {
            return NotFound();
        }

        if(result.Result == TaskOperationResult.InvalidAssignee)
        {
            ModelState.AddModelError(
                "assigneeId", "El usuario asignado no existe.");

            return ValidationProblem();
        }

        if(result.Result != TaskOperationResult.Success ||
            result.Task is null ||
            result.ProjectName is null)
        {
            throw new InvalidOperationException(
                "El resultado de la creación de la tarea no es válido.");
        }

        var task = result.Task;
        var projectName = result.ProjectName;

        UserSummaryResponse? userSummary = null;

        if(task.AssignedId is not null)
        {
            var assignee = await _localUserService.GetByIdAsync(
                task.AssignedId.Value, cancellationToken);
            
            if(assignee is not null)
            {
                userSummary = new UserSummaryResponse
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName
                };
            }
        }

        var taskResponse = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            Assignee = userSummary,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        return Created($"/api/v1/tasks/{task.Id}", taskResponse);
    }

    [HttpPut("{taskId}")]
    public async Task<IActionResult> Update(
        Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var priority = Enum.Parse<TaskPriority>(request.Priority);

        var result = await _taskService.UpdateTaskAsync(
            taskId, request.Title, request.Description,
            priority, request.DueDate, _currentUser.UserId,
            _currentUser.IsAdmin, cancellationToken);
        
        if(result.Result == TaskOperationResult.NotFound)
        {
            return NotFound();
        }

        if(result.Result != TaskOperationResult.Success ||
            result.Task is null ||
            result.ProjectName is null)
        {
            throw new InvalidOperationException(
                "El resultado de la actualización de la tarea no es válido");
        }
        
        var task = result.Task;
        var projectName = result.ProjectName;

        UserSummaryResponse? userSummary = null;

        if(task.AssignedId is not null)
        {
            var assignee = await _localUserService.GetByIdAsync(
                task.AssignedId.Value, cancellationToken);
            
            if(assignee is not null)
            {
                userSummary = new UserSummaryResponse
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName
                };
            }
        }

        var taskResponse = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            Assignee = userSummary,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        return Ok(taskResponse);
    }


     [HttpPut("{taskId}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid taskId,
        [FromBody] ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var status = Enum.Parse<TaskState>(request.Status);

        var result = await _taskService.ChangeTaskStatusAsync(
            taskId, status, _currentUser.UserId,
            _currentUser.IsAdmin, cancellationToken);
        
        if(result.Result == TaskOperationResult.NotFound)
        {
            return NotFound();
        }

        if(result.Result != TaskOperationResult.Success ||
            result.Task is null ||
            result.ProjectName is null)
        {
            throw new InvalidOperationException(
                "El resultado de la actualización de la tarea no es válido");
        }
        
        var task = result.Task;
        var projectName = result.ProjectName;

        UserSummaryResponse? userSummary = null;

        if(task.AssignedId is not null)
        {
            var assignee = await _localUserService.GetByIdAsync(
                task.AssignedId.Value, cancellationToken);
            
            if(assignee is not null)
            {
                userSummary = new UserSummaryResponse
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName
                };
            }
        }

        var taskResponse = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            Assignee = userSummary,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        return Ok(taskResponse);
    }
    
    [HttpPut("{taskId}/assignee")]
    public async Task<IActionResult> ChangeAssignee(
        Guid taskId,
        [FromBody] ChangeTaskAssigneeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.ChangeTaskAssigneeAsync(
            taskId, request.AssigneeId, _currentUser.UserId,
            _currentUser.IsAdmin, cancellationToken);
        
        if(result.Result == TaskOperationResult.NotFound)
        {
            return NotFound();
        }

        if(result.Result == TaskOperationResult.InvalidAssignee)
        {
            ModelState.AddModelError(
                "assgneeId", "El usuario asignado no existe.");

            return ValidationProblem();
        }

        if(result.Result != TaskOperationResult.Success ||
            result.Task is null ||
            result.ProjectName is null)
        {
            throw new InvalidOperationException(
                "El resultado de la actualización de la tarea no es válido");
        }
        
        var task = result.Task;
        var projectName = result.ProjectName;

        UserSummaryResponse? userSummary = null;

        if(task.AssignedId is not null)
        {
            var assignee = await _localUserService.GetByIdAsync(
                task.AssignedId.Value, cancellationToken);
            
            if(assignee is not null)
            {
                userSummary = new UserSummaryResponse
                {
                    Id = assignee.Id,
                    DisplayName = assignee.DisplayName
                };
            }
        }

        var taskResponse = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = projectName,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            Assignee = userSummary,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };

        return Ok(taskResponse);
    }
}