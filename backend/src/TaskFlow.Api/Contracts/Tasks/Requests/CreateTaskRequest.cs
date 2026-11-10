using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Contracts.Tasks;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]

public class CreateTaskRequest : IValidatableObject
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "ToDo";
    public required string Priority { get; init; }
    public DateOnly? DueDate { get; init; }
    public Guid? AssigneeId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors =  new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add(new ValidationResult(
                "El título es obligatorio.", new [] {"title"}));
        }
        else if(Title.Trim().Length > 200)
        {
            errors.Add(new ValidationResult(
                "El título es demasiado largo.", new [] {"title"}));
        }

        if(Description is not null && Description.Trim().Length > 5000)
        {
            errors.Add(new ValidationResult(
                "La descripción es demasiado larga.", new [] {"description"}));
        }

        if (Status is not ("ToDo" or "InProgress" or "Done"))
        {
            errors.Add(new ValidationResult(
                "El estado es desconocido.", new [] {"status"}));
        }

        if (Priority is not ("Low" or "Medium" or "High"))
        {
            errors.Add(new ValidationResult(
                "La prioridad es desconocida.", new [] {"priority"}));
        }

        if(AssigneeId == Guid.Empty)
        {
            errors.Add(new ValidationResult(
                "El usuario asignado no puede estar vacío.", new [] {"assigneeId"}));
        }

        return errors;
    }
}