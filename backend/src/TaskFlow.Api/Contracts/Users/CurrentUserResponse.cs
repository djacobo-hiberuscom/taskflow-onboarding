namespace TaskFlow.Api.Contracts.Users;

public sealed record CurrentUserResponse(Guid Id, string DisplayName, string? Email, string[] Roles);
