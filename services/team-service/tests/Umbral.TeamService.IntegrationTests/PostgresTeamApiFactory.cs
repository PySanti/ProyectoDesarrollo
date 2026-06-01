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
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=umbral_team_test;Username=umbral_user;Password=umbral_pass;";

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
        
        // Drop and recreate the database
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DROP SCHEMA IF EXISTS public CASCADE;
            CREATE SCHEMA public;
            GRANT ALL ON SCHEMA public TO umbral_user;
        ";
        await cmd.ExecuteNonQueryAsync();
    }
}