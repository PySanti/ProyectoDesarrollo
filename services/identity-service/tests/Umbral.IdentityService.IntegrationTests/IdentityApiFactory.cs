using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.IdentityService.Application.Abstractions.Identity;
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
        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, CancellationToken cancellationToken)
            => Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}
