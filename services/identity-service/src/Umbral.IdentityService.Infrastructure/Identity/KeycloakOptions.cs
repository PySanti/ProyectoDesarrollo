namespace Umbral.IdentityService.Infrastructure.Identity;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool RequireUpdatePasswordAction { get; set; } = true;
    public string TemporaryPassword { get; set; } = "Temp@123456";
}
