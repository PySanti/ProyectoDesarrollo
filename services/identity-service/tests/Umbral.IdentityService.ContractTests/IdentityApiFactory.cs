using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.ContractTests;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"identity-contract-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IKeycloakIdentityPort>();
            services.AddSingleton<IKeycloakIdentityPort, TestKeycloakIdentityPort>();

            services.RemoveAll<IUserWelcomeEmailSender>();
            services.AddSingleton<IUserWelcomeEmailSender, NoOpWelcomeEmailSender>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private sealed class TestKeycloakIdentityPort : IKeycloakIdentityPort
    {
        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken cancellationToken)
            => Task.FromResult(Guid.NewGuid().ToString("N"));

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public HttpClient CreateClientAs(string role, Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    // Task 5: roles literales sin la expansión composite de CreateClientAs — necesario para probar
    // el AND de una policy compuesta (rol correcto sin el privilegio, o viceversa), combinaciones que
    // el reconciliador real nunca produce pero que la policy debe rechazar igual.
    public HttpClient CreateClientWithRoles(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    private sealed class NoOpWelcomeEmailSender : IUserWelcomeEmailSender
    {
        public Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
