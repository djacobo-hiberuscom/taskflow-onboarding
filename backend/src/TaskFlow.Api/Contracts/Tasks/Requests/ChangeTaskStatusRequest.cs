using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Contracts.Tasks;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]

public class ChangeTaskStatusRequest : IValidatableObject
{
    public required string Status { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors =  new List<ValidationResult>();

        if (Status is not ("ToDo" or "InProgress" or "Done"))
        {
            errors.Add(new ValidationResult(
                "El estado es desconocido.", new [] {"status"}));
        }

        return errors;
    }
}