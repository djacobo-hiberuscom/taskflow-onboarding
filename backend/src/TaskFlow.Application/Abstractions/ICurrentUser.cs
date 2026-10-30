namespace TaskFlow.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
    string DisplayName { get; }
}