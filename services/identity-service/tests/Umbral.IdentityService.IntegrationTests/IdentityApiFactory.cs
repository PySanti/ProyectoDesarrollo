using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Persistence;

namespace Umbral.IdentityService.IntegrationTests;

public class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"identity-integration-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IKeycloakIdentityPort>();
            services.AddSingleton<IKeycloakIdentityPort>(CreateKeycloakIdentityPort());

            services.RemoveAll<IUserWelcomeEmailSender>();
            services.AddSingleton<IUserWelcomeEmailSender, NoOpWelcomeEmailSender>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public int GetPersistedUsersCount()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return dbContext.Usuarios.Count();
    }

    protected virtual IKeycloakIdentityPort CreateKeycloakIdentityPort()
    {
        return new TestKeycloakIdentityPort();
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

    private sealed class NoOpWelcomeEmailSender : IUserWelcomeEmailSender
    {
        public Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
