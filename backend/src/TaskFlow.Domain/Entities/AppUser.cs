using System.ComponentModel.DataAnnotations;
namespace TaskFlow.Domain;
public class AppUser
{
    public Guid Id { get; private set; }
    public string KeycloakSub { get; private set; }
    public string DisplayName { get; private set; }
    public string? Email { get; private set;}
    public DateTimeOffset CreatedAt {get; private set;}

    private static string ValidateKeycloakSub(string keycloakSub)
    {
        if (string.IsNullOrWhiteSpace(keycloakSub))
        {
            throw new ArgumentException("No se ha encontrado el ID de KeyCloak.",
                nameof(keycloakSub));
        }

        if(keycloakSub.Length > 255)
        {
            throw new ArgumentException("El ID de KeyCloak es demasiado largo.",
                nameof(keycloakSub));
        }

        return keycloakSub;
    }

    private static string ValidateDisplayName(string displayName)
    {
        
        if(String.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("El nombre no puede estar vacío.",
                nameof(displayName));
        }

        displayName = displayName.Trim();

        if(displayName.Length > 200)
        {
            throw new ArgumentException("El nombre es demasiado largo.",
                nameof(displayName));
        }

        return displayName;
    }

    private static string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        if(email.Length > 320)
        {
            return null;
        }

        var validator = new EmailAddressAttribute();
        
        if (!validator.IsValid(email))
        {
            return null;
        }

        return email;
    }

    public AppUser(string keycloakSub, string displayName, string? email)
    {
        keycloakSub = ValidateKeycloakSub(keycloakSub);
        displayName = ValidateDisplayName(displayName);
        email = ValidateEmail(email);

        Id = Guid.NewGuid();
        KeycloakSub = keycloakSub;
        DisplayName = displayName;
        Email = email;
        CreatedAt = DateTimeOffset.UtcNow;

    }

    public bool UpdateProfile(string displayName, string? email)
    {
        displayName = ValidateDisplayName(displayName);
        email = ValidateEmail(email);

        if (DisplayName == displayName && Email == email)
        {
            return false;
        }

        DisplayName = displayName;
        Email = email;
        return true;
    }

}
