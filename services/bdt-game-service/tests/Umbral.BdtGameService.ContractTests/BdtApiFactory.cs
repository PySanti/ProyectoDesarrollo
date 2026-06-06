using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.BdtGameService.Application.Abstractions.Qr;
using Umbral.BdtGameService.Infrastructure.Qr;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public class BdtApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"bdt-contract-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BdtDbContext>>();
            services.AddDbContext<BdtDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IQrImageDecoder>();
            services.AddScoped<IQrImageDecoder, DeterministicQrImageDecoder>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
