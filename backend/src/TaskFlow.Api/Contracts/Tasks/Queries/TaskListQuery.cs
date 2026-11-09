using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Contracts.Tasks;

public class TaskListQuery : IValidatableObject
{
    public Guid? ProjectId { get; init; }
    public string[] Status { get; init; } = [];
    public string[] Priority { get; init; } = [];
    public string? AssigneeId { get; init; }
    public string Sort { get; init; } = "createdAt";
    public string? Order { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        if (Status.Any(status => status is not ("ToDo" or "InProgress" or "Done")))
        {
            errors.Add(new ValidationResult(
                "Algún estado no es válido.", new[] { "status" }));
        }

        if (Priority.Any(priority => priority is not ("Low" or "Medium" or "High")))
        {
            errors.Add(new ValidationResult(
                "Alguna prioridad no es válida.", new[] { "priority" }));
        }

        if (AssigneeId is not null &&
            AssigneeId != "unassigned" &&
            !Guid.TryParse(AssigneeId, out _))
        {
            errors.Add(new ValidationResult(
                "El usuario asignado debe ser un GUID válido o 'unassigned'.",
                new[] { "assigneeId" }));
        }

        if (Sort is not ("createdAt" or "dueDate"))
        {
            errors.Add(new ValidationResult(
                "La ordenación no es válida.", new[] { "sort" }));
        }

        if (Order is not null && Order is not ("asc" or "desc"))
        {
            errors.Add(new ValidationResult(
                "La dirección de ordenación debe ser 'asc' o 'desc'.",
                new[] { "order" }));
        }

        if (Page < 1)
        {
            errors.Add(new ValidationResult(
                "La página debe ser mayor o igual a 1.",
                new[] { "page" }));
        }

        if (PageSize < 1 || PageSize > 100)
        {
            errors.Add(new ValidationResult(
                "El tamaño de página debe estar entre 1 y 100.",
                new[] { "pageSize" }));
        }

        return errors;
    }
}
