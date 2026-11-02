using TaskFlow.Domain;

namespace TaskFlow.Application.Users;

public interface IAppUserRepository
{
    Task<AppUser> GetOrCreateAsync(AppUser candidate, CancellationToken cancellationToken = default);

    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
