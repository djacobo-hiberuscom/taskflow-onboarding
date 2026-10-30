using TaskFlow.Application.Abstractions;
using TaskFlow.Domain;

namespace TaskFlow.Api.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private AppUser? _user;

    internal AppUser User => _user ?? throw new InvalidOperationException(
        "El usuario local todavía no se ha resuelto para esta petición.");

    public Guid UserId => User.Id;
    public bool IsAdmin { get; private set; }

    internal void Initialize(AppUser user, bool isAdmin)
    {
        _user = user;
        IsAdmin = isAdmin;
    }
}
