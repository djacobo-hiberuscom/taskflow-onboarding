namespace TaskFlow.Domain;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt  { get; private set; }

    // La persistencia rellena las propiedades al reconstruir un proyecto existente.
    private Project()
    {
        Name = null!;
    }

    public Project(string name, string? description, Guid createdBy)
    {
        name = ValidateName(name);

        description = DomainText.ValidateDescription(description);

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException(
                "Se debe asignar un creador.", nameof(createdBy)
            );
        }

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre del proyecto es obligatorio.", nameof(name)
            );
        }

        name = name.Trim();
        if(name.Length > 150)
        {
            throw new ArgumentException(
                "El nombre del proyecto es demasiado largo.", nameof(name)
            );
        }

        return name;
    }

    public void UpdateDetails(string name, string? description)
    {
        name = ValidateName(name);
        description = DomainText.ValidateDescription(description);

        Name = name;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

