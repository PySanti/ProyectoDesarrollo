using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.IdentityService.Application.Abstractions.Identity;
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

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private sealed class TestKeycloakIdentityPort : IKeycloakIdentityPort
    {
        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, CancellationToken cancellationToken)
            => Task.FromResult(Guid.NewGuid().ToString("N"));
    }
}
