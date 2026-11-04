using TaskFlow.Domain;

namespace TaskFlow.Application.Projects;

public interface IProjectRepository
{
    void Add(Project project);

    void AddMembership(AppUserProject membership);

    Task<Project?> GetVisibleByIdAsync(
        Guid projectId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken);

    Task<AppUser[]> GetMembersAsync(
        Guid projectId, CancellationToken cancellationToken);

    Task<ProjectPage> GetPageAsync(
        Guid? memberUserId, bool archived, int page, int pageSize,
        CancellationToken cancellationToken);

    Task<Project?> GetByIdForUpdateAsync(
        Guid projectId, CancellationToken cancellationToken = default);
    
    void Remove(Project project);

    System.Threading.Tasks.Task EnsureMembershipAsync(
        Guid projectId, Guid userId, CancellationToken cancellationToken);
}

