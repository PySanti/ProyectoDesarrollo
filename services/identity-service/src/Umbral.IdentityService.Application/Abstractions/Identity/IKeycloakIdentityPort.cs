namespace Umbral.IdentityService.Application.Abstractions.Identity;

public interface IKeycloakIdentityPort
{
    Task<string> CreateUserWithInitialRoleAsync(
        string name,
        string email,
        string initialRole,
        CancellationToken cancellationToken);
}
