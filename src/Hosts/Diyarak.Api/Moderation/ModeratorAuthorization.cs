using Diyarak.Api.Authentication;
using Diyarak.Market.Application;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Diyarak.Api.Moderation;

internal sealed class ModeratorRequirement : IAuthorizationRequirement;

/// <summary>Moderators are listed by verified phone number in configuration.</summary>
internal sealed class ModeratorOptions
{
    public const string SectionName = "Moderation";

    public IReadOnlySet<string> ModeratorPhoneNumbers { get; init; } = new HashSet<string>();

    internal static ModeratorOptions Load(IConfiguration configuration)
    {
        string[] configured =
            configuration.GetSection($"{SectionName}:ModeratorPhoneNumbers").Get<string[]>() ?? [];

        var phones = new HashSet<string>(StringComparer.Ordinal);

        foreach (string phone in configured)
        {
            if (!PhoneNumber.TryCreate(phone, out PhoneNumber? parsed))
            {
                throw new InvalidOperationException(
                    $"'{SectionName}:ModeratorPhoneNumbers' contains a value that is not an E.164 phone number.");
            }

            phones.Add(parsed.Value);
        }

        return new ModeratorOptions { ModeratorPhoneNumbers = phones };
    }
}

/// <summary>
/// Succeeds when the token's external identity maps to an internal user whose verified phone
/// number is a configured moderator number.
/// </summary>
internal sealed class ModeratorAuthorizationHandler(
    IExternalIdentityResolver identityResolver,
    IPublisherPhoneLookup phoneLookup,
    ModeratorOptions options)
    : AuthorizationHandler<ModeratorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModeratorRequirement requirement)
    {
        if (options.ModeratorPhoneNumbers.Count == 0 ||
            context.Resource is not HttpContext httpContext ||
            !context.User.TryGetExternalIdentity(out ExternalIdentity? identity) ||
            identity is null)
        {
            return;
        }

        Guid? userId = await identityResolver.ResolveUserIdAsync(identity, httpContext.RequestAborted);
        if (userId is null || userId == Guid.Empty)
            return;

        PhoneNumber? phone = await phoneLookup.FindVerifiedPhoneAsync(userId.Value, httpContext.RequestAborted);
        if (phone is null || !options.ModeratorPhoneNumbers.Contains(phone.Value))
            return;

        httpContext.Features.Set(new AuthenticatedActorFeature(userId.Value));
        context.Succeed(requirement);
    }
}
