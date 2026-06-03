using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Umbral.TeamService.Infrastructure.Persistence;
using System.Data;

namespace Umbral.TeamService.IntegrationTests;

public sealed class PostgresTeamApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable("TEAM_POSTGRES_TEST_CONNECTION")
        ?? "Host=localhost;Port=55432;Database=umbral_team;Username=umbral;Password=16102005;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TeamDbContext>>();
            services.AddDbContext<TeamDbContext>(options =>
                options.UseNpgsql(_connectionString));

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
        var databaseUser = new NpgsqlConnectionStringBuilder(_connectionString).Username;
        
        // Drop and recreate the database
        var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            DROP SCHEMA IF EXISTS public CASCADE;
            CREATE SCHEMA public;
            GRANT ALL ON SCHEMA public TO {QuoteIdentifier(databaseUser)};
        ";
        await cmd.ExecuteNonQueryAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
