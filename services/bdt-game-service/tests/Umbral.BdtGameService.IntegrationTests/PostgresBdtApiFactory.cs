using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class PostgresBdtApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable("BDT_POSTGRES_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=umbral_bdt_game;Username=postgres;Password=postgres;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BdtDbContext>>();
            services.AddDbContext<BdtDbContext>(options => options.UseNpgsql(_connectionString));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var databaseUser = new NpgsqlConnectionStringBuilder(_connectionString).Username
            ?? throw new InvalidOperationException("PostgreSQL test connection must define a username.");

        var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            DROP SCHEMA IF EXISTS public CASCADE;
            CREATE SCHEMA public;
            GRANT ALL ON SCHEMA public TO {QuoteIdentifier(databaseUser)};
        ";
        await cmd.ExecuteNonQueryAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
