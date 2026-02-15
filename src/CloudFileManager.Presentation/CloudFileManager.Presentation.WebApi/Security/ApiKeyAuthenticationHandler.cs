using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CloudFileManager.Presentation.WebApi.Security;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is not configured."));
        }

        if (!Request.Headers.TryGetValue(Options.HeaderName, out var providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key header."));
        }

        bool isValid = string.Equals(providedApiKey.ToString(), Options.ApiKey, StringComparison.Ordinal);
        if (!isValid)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, "api-key-client"),
            new Claim(ClaimTypes.Name, "ApiKeyClient")
        ];

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
