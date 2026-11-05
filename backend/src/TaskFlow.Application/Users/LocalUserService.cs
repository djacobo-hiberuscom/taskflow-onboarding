using TaskFlow.Application.Abstractions;
using TaskFlow.Domain;

namespace TaskFlow.Application.Users;

public class LocalUserService(IAppUserRepository users, IUnitOfWork unitOfWork)
{
    public async Task<AppUser> ResolveAsync(
        string subject, string? name, string? preferredUsername, string? email,
        CancellationToken cancellationToken = default)
    {
        var candidate = new AppUser(subject, "Usuario", email);
        candidate.UpdateProfile(GetDisplayName(name, preferredUsername, candidate.Id), email);

        return await users.GetOrCreateAsync(candidate, cancellationToken);
    }

    public async System.Threading.Tasks.Task SynchronizeProfileAsync(
        AppUser user, string? name, string? preferredUsername, string? email,
        CancellationToken cancellationToken = default)
    {
        if (user.UpdateProfile(GetDisplayName(name, preferredUsername, user.Id), email))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetDisplayName(string? name, string? preferredUsername, Guid userId)
    {
        var displayName = !string.IsNullOrWhiteSpace(name) ? name : preferredUsername;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return $"Usuario {userId.ToString("N")[..8]}";
        }

        displayName = displayName.Trim();
        return displayName.Length > 200 ? displayName[..200] : displayName;
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);

        return user;
    }

    public async Task<AppUser[]> GetByIdsAsync(Guid[] userIds, CancellationToken cancellationToken)
    {
        return await users.GetByIdsAsync(userIds, cancellationToken);
    }

    public async Task<AppUser[]> GetAssignableAsync(
        CancellationToken cancellationToken)
    {
        return await users.GetAssignableAsync(cancellationToken);
    }
}
