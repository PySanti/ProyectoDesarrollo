using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TriviaGame.Api.Tests.Testing;
using Umbral.TriviaGame.Infrastructure.Data;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<TriviaGameDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            var ctxDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(TriviaGameDbContext));
            if (ctxDescriptor is not null) services.Remove(ctxDescriptor);

            services.AddDbContext<TriviaGameDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.AddScoped(_ => TestClaimsProvider.WithOperadorRole());
        });
    }
}
