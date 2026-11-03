namespace TaskFlow.Api.Contracts.Users;

public class UserSummaryResponse
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
}
