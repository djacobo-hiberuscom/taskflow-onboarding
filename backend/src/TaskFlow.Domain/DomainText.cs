namespace TaskFlow.Domain;

internal static class DomainText
{
    public static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            description = null;
        }
        else
        {
            description = description.Trim();
            if(description.Length > 5000)
            {
                throw new ArgumentException(
                    "La descripción es demasiado larga.", nameof(description)
                );
            }
        }

        return description;
    }
}