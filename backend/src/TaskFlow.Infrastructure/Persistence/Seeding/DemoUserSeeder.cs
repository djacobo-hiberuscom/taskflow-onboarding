using TaskFlow.Application.Users;
using System.Text.Json;

namespace TaskFlow.Infrastructure.Persistence.Seeding;

public class DemoUserSeeder
{
    private readonly LocalUserService _localUserService;

    public DemoUserSeeder(LocalUserService localUserService)
    {
        _localUserService = localUserService;
    }


    public async Task SeedAsync(
        string realmFilePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(realmFilePath);

        using var document = await JsonDocument.ParseAsync(
            stream, cancellationToken: cancellationToken);

        var users = document.RootElement.GetProperty("users").EnumerateArray();

        foreach (var user in users)
        {
            var subject = user.GetProperty("id").GetString();

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new InvalidDataException(
                    "Hay un usuario del realm sin identificador");
            }

            var username = user.GetProperty("username").GetString();
            var firstName = user.GetProperty("firstName").GetString();
            var lastName = user.GetProperty("lastName").GetString();
            var email =  user.GetProperty("email").GetString();

            var displayName = $"{firstName} {lastName}".Trim();

            await _localUserService.ResolveAsync(
                subject, displayName, username, email, cancellationToken);
        }
    }
}