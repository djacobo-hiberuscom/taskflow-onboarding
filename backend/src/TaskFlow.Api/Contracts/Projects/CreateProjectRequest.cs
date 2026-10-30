using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Contracts.Projects;


[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateProjectRequest : IValidatableObject
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ValidationResult("El nombre es obligatorio.",
                new [] {"name"}));
        }
        else if(Name.Trim().Length > 150)
        {
            errors.Add(new ValidationResult("El nombre es demasiado largo.",
                new [] {"name"}));   
        }

        if(!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 5000)
        {
            errors.Add(new ValidationResult("La descripción es demasiado larga",
                new [] {"description"}));
        }

        return errors;
    }
}