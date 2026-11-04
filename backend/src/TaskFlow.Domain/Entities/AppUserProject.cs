
namespace TaskFlow.Domain;

public class AppUserProject
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt {get; private set;}

    public AppUserProject(Guid projectId, Guid userId)
    {
        if(projectId == Guid.Empty)
        {
            throw new ArgumentException("El ID del proyecto no puede estar vacío.",
                nameof(projectId));
        }

        if(userId == Guid.Empty)
        {
            throw new ArgumentException("El ID del usuario no puede estar vacío.",
                nameof(userId));
        }

        ProjectId = projectId;
        UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}