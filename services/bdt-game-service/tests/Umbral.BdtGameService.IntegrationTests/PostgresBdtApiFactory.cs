using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class PostgresBdtApiFactory : WebApplicationFactory<Program>
{
    private readonly string _schemaName = $"bdt_test_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly string _databaseUser;

    public PostgresBdtApiFactory()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("BDT_POSTGRES_TEST_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=umbral_bdt_game_test;Username=postgres;Password=postgres;";
        var builder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            SearchPath = _schemaName
        };
        _connectionString = builder.ConnectionString;
        _databaseUser = builder.Username
            ?? throw new InvalidOperationException("PostgreSQL test connection must define a username.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            EnsureSchemaExists();

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

        var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            DROP SCHEMA IF EXISTS {QuoteIdentifier(_schemaName)} CASCADE;
            CREATE SCHEMA {QuoteIdentifier(_schemaName)};
            GRANT ALL ON SCHEMA {QuoteIdentifier(_schemaName)} TO {QuoteIdentifier(_databaseUser)};
        ";
        await cmd.ExecuteNonQueryAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
        await databaseCreator.CreateTablesAsync();
    }

    private void EnsureSchemaExists()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            CREATE SCHEMA IF NOT EXISTS {QuoteIdentifier(_schemaName)};
            GRANT ALL ON SCHEMA {QuoteIdentifier(_schemaName)} TO {QuoteIdentifier(_databaseUser)};
        ";
        cmd.ExecuteNonQuery();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
