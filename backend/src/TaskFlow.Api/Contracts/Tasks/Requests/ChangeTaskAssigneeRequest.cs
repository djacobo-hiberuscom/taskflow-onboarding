using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Contracts.Tasks;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]

public class ChangeTaskAssigneeRequest : IValidatableObject
{
    public required Guid? AssigneeId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors =  new List<ValidationResult>();

        if (AssigneeId == Guid.Empty)
        {
            errors.Add(new ValidationResult(
                "El usuario asignado no puede estar vacío.",
                new [] {"assigneeId"}));
        }

        return errors;
    }
}