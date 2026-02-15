using Microsoft.AspNetCore.Authentication;

namespace CloudFileManager.Presentation.WebApi.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";

    public string ApiKey { get; set; } = string.Empty;
}
