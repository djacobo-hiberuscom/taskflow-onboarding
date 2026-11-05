using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Users;
using TaskFlow.Domain;
using DomainTask = TaskFlow.Domain.Task;

namespace TaskFlow.Application.Tasks;

public class TaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectRepository _projectRepository;
    private readonly IAppUserRepository _appUserRepository;

    public TaskService(
        ITaskRepository taskRepository, IProjectRepository projectRepository,
        IAppUserRepository appUserRepository, IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _appUserRepository = appUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(DomainTask Task, string ProjectName)?> GetTaskByIdAsync(
        Guid taskId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);

        if(task is null)
        {
            return null;
        }

        var visibleProject = await _projectRepository.GetVisibleByIdAsync(
            task.ProjectId, userId, isAdmin, cancellationToken);

        if(visibleProject is null)
        {
            return null;
        }

        return (Task: task, ProjectName: visibleProject.Name);
    }

    public async Task<(
        TaskOperationResult Result, DomainTask? Task, string? ProjectName)>
        CreateTaskAsync(Guid projectId, string title, string? description,
            TaskPriority priority, TaskState status, DateOnly? dueDate,
            Guid? assigneeId, Guid userId, bool isAdmin,
            CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<(
            TaskOperationResult Result, DomainTask? Task, string? ProjectName)>(
                async token =>
        {
            //Proteger al asignado, si se ha indicado.
            AppUser? assignee = null;
            
            if(assigneeId is not null)
            {
                assignee = await _appUserRepository.GetByIdForKeyShareAsync(
                    assigneeId.Value, token);
            }

            //Bloquear el proyecto, haya usuario asignado o no.
            var project = await _projectRepository.GetByIdForUpdateAsync(
                projectId, token);
        
            if(project is null)
            {
                return (TaskOperationResult.NotFound, null, null);
            }

            //Comprobar la visibilidad después del bloqueo
            var visibleProject = await _projectRepository.GetVisibleByIdAsync(
                projectId, userId, isAdmin, token);
        
            if(visibleProject is null)
            {
                return (TaskOperationResult.NotFound, null, null);
            }

            //Ahora podemos comunicar que el asignado no existe
            //comprobar que el usuario asignado es válido
            //esta comprobacion se hace despues de la del proycto porque en la
            //especificacion tiene prioridad el error de visibilidad sobre el de 
            //asignado inválido.
            if(assigneeId.HasValue && assignee is null)
            {
                return (TaskOperationResult.InvalidAssignee, null, null);
            }

            //se crea la tarea
            var task = new DomainTask(
                projectId, title, priority,
                description, assigneeId, status,
                dueDate);
            
            //mirar si hay alguien a quien añadir como miembro
            //asegurar la pertenencia
            if (assigneeId.HasValue)
            {
                await _projectRepository.EnsureMembershipAsync(
                    projectId, assigneeId.Value, token);
            }

            //añadir tarea guardar y devolver resultado 
            _taskRepository.Add(task);

            await _unitOfWork.SaveChangesAsync(token);

            return (TaskOperationResult.Success, task, visibleProject.Name);
        }, cancellationToken);
    }

    public async Task<(
        TaskOperationResult Result,
        DomainTask? Task,
        string? ProjectName)> UpdateTaskAsync(
            Guid taskId, string title, string? description,
            TaskPriority priority, DateOnly? dueDate, Guid userId,
            bool isAdmin, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<(
            TaskOperationResult Result, 
            DomainTask? Task, 
            string? ProjectName)>(async token =>
            {
                var projectId = await _taskRepository.GetProjectIdAsync(
                    taskId, token);
                
                if(projectId is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var project = await _projectRepository.GetByIdForUpdateAsync(
                    projectId.Value, token);

                if(project is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var visibleProject = await _projectRepository.GetVisibleByIdAsync(
                    projectId.Value, userId, isAdmin, token);
                
                if(visibleProject is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var task = await _taskRepository.GetByIdAsync(
                    taskId, token);
                
                if(task is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                task.UpdateDetails(title, description, priority, dueDate);

                await _unitOfWork.SaveChangesAsync(token);

                return (TaskOperationResult.Success, task, visibleProject.Name);

            }, cancellationToken);
    }

        public async Task<(
        TaskOperationResult Result,
        DomainTask? Task,
        string? ProjectName)> ChangeTaskStatusAsync(
            Guid taskId, TaskState status, Guid userId,
            bool isAdmin, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<(
            TaskOperationResult Result, 
            DomainTask? Task, 
            string? ProjectName)>(async token =>
            {
                var projectId = await _taskRepository.GetProjectIdAsync(
                    taskId, token);
                
                if(projectId is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var project = await _projectRepository.GetByIdForUpdateAsync(
                    projectId.Value, token);

                if(project is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var visibleProject = await _projectRepository.GetVisibleByIdAsync(
                    projectId.Value, userId, isAdmin, token);
                
                if(visibleProject is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var task = await _taskRepository.GetByIdAsync(
                    taskId, token);
                
                if(task is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                task.ChangeStatus(status);

                await _unitOfWork.SaveChangesAsync(token);

                return (TaskOperationResult.Success, task, visibleProject.Name);

            }, cancellationToken);
    }

     public async Task<(
        TaskOperationResult Result,
        DomainTask? Task,
        string? ProjectName)> ChangeTaskAssigneeAsync(
            Guid taskId, Guid? assigneeId, Guid userId,
            bool isAdmin, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<(
            TaskOperationResult Result, 
            DomainTask? Task, 
            string? ProjectName)>(async token =>
            {
                AppUser? assignee = null;

                if(assigneeId is not null)
                {
                    assignee = await _appUserRepository.GetByIdForKeyShareAsync(
                        assigneeId.Value, token);
                }
                var projectId = await _taskRepository.GetProjectIdAsync(
                    taskId, token);
                
                if(projectId is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var project = await _projectRepository.GetByIdForUpdateAsync(
                    projectId.Value, token);

                if(project is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var visibleProject = await _projectRepository.GetVisibleByIdAsync(
                    projectId.Value, userId, isAdmin, token);
                
                if(visibleProject is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                var task = await _taskRepository.GetByIdAsync(
                    taskId, token);
                
                if(task is null)
                {
                    return (TaskOperationResult.NotFound, null, null);
                }

                if(assigneeId.HasValue)
                {
                    if(assignee is null)
                    {
                        return (TaskOperationResult.InvalidAssignee, null, null);
                    }

                    await _projectRepository.EnsureMembershipAsync(
                        projectId.Value, assigneeId.Value, token);
                }

                task.ChangeAssignee(assigneeId);

                await _unitOfWork.SaveChangesAsync(token);

                return (TaskOperationResult.Success, task, visibleProject.Name);

            }, cancellationToken);
    }

    public async Task<TaskOperationResult> DeleteTaskAsync(
        Guid taskId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<TaskOperationResult>(
            async token =>
            {
                var projectId = await _taskRepository.GetProjectIdAsync(
                    taskId, token);
                
                if(projectId is null)
                {
                    return TaskOperationResult.NotFound;
                }

                var project = await _projectRepository.GetByIdForUpdateAsync(
                    projectId.Value, token);

                if(project is null)
                {
                    return TaskOperationResult.NotFound;
                }

                var visibleProject = await _projectRepository.GetVisibleByIdAsync(
                    projectId.Value, userId, isAdmin, token);
                
                if(visibleProject is null)
                {
                    return TaskOperationResult.NotFound;
                }

                var task = await _taskRepository.GetByIdAsync(
                    taskId, token);
                
                if(task is null)
                {
                    return TaskOperationResult.NotFound;
                }

                _taskRepository.Remove(task);

                await _unitOfWork.SaveChangesAsync(token);

                return TaskOperationResult.Success;

            }, cancellationToken);
    }

}