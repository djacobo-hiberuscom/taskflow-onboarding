using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Users;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public class AppUserRepository(TaskFlowDbContext context) : IAppUserRepository
{
    public async Task<AppUser> GetOrCreateAsync(
        AppUser candidate, CancellationToken cancellationToken = default)
    {
        var existing = await context.Users.SingleOrDefaultAsync(
            user => user.KeycloakSub == candidate.KeycloakSub, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        // La unicidad de keycloak_sub decide el ganador si llegan dos primeros accesos a la vez.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app_user (id, keycloak_sub, display_name, email, created_at)
            VALUES ({candidate.Id}, {candidate.KeycloakSub}, {candidate.DisplayName},
                    {candidate.Email}, {candidate.CreatedAt})
            ON CONFLICT (keycloak_sub) DO NOTHING
            """, cancellationToken);

        return await context.Users.SingleAsync(
            user => user.KeycloakSub == candidate.KeycloakSub, cancellationToken);
    }

    public async Task<AppUser?> GetByIdAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(
            user => user.Id == userId, cancellationToken);
        
        return user;
    }

    public async Task<AppUser[]> GetByIdsAsync(Guid[] userIds, CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return [];
        }

        return await context.Users
            .Where(user => userIds.Contains(user.Id))
            .ToArrayAsync(cancellationToken);
    }
}
