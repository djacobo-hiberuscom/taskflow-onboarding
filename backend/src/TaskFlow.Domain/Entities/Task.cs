namespace TaskFlow.Domain;

public class Task
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? AssignedId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskState Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Task(Guid projectId, string title, TaskPriority priority,
                string? description = null, Guid? assignedId = null,
                TaskState status = TaskState.ToDo,
                DateOnly? dueDate = null)
    {
        if(projectId == Guid.Empty)
        {
            throw new ArgumentException("El ID del proyecto es obligatorio. ",
                nameof(projectId));
        }

        title = ValidateTitle(title);

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority),
                "La prioridad no es válida.");
        }
        description = DomainText.ValidateDescription(description);

        if(assignedId == Guid.Empty)
        {
            throw new ArgumentException("El ID del usuario asignado no puede estar vacío. ",
                nameof(assignedId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status),
                "El estado no es válido.");
        }
        
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Title = title;
        Priority = priority;
        Description = description;
        AssignedId = assignedId;
        Status = status;
        DueDate = dueDate;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "El título de la tarea es obligatorio.", nameof(title)
            );
        }

        title = title.Trim();
        if(title.Length > 200)
        {
            throw new ArgumentException(
                "El título de la tarea es demasiado largo.", nameof(title)
            );
        }

        return title;
    }

    public void UpdateDetails(
        string title, string? description, TaskPriority priority,
        DateOnly? dueDate)
    {
        title = ValidateTitle(title);
        description = DomainText.ValidateDescription(description);

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority),
                "La prioridad no es válida.");
        }

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}