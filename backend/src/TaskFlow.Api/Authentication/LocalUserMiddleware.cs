using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Users;

namespace TaskFlow.Api.Authentication;

public sealed class LocalUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context, CurrentUser currentUser, LocalUserService users)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is not null &&
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null &&
            context.User.Identity?.IsAuthenticated == true &&
            (context.User.IsInRole("Admin") || context.User.IsInRole("User")))
        {
            var user = await users.ResolveAsync(
                context.User.FindFirst("sub")!.Value,
                context.User.FindFirst("name")?.Value,
                context.User.FindFirst("preferred_username")?.Value,
                context.User.FindFirst("email")?.Value,
                context.RequestAborted);

            currentUser.Initialize(user, context.User.IsInRole("Admin"));
        }

        await next(context);
    }
}
