using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Umbral.TriviaGame.Api.Constants;
using Umbral.TriviaGame.Api.Hubs;
using Umbral.TriviaGame.Api.Middleware;
using Umbral.TriviaGame.Api.Services;
using Umbral.TriviaGame.Application;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        options.Authority = jwtSection["Authority"];
        options.Audience = jwtSection["Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(options.Authority),
            ValidateAudience = !string.IsNullOrEmpty(options.Audience),
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
        };

        if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(options.Authority))
        {
            options.TokenValidationParameters.ValidateIssuer = false;
            options.TokenValidationParameters.ValidateAudience = false;
            options.TokenValidationParameters.ValidateLifetime = false;
            options.TokenValidationParameters.SignatureValidator = (token, _) => new JwtSecurityToken(token);
        }
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.Operador, policy =>
        policy.RequireRole(PolicyNames.Operador));
});

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString("TriviaGameDb")
    ?? "Host=localhost;Port=5432;Database=umbral_trivia;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddScoped<ITriviaLobbyNotifier, TriviaLobbyNotifier>();
builder.Services.AddScoped<ITriviaRankingNotifier, TriviaRankingNotifier>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TriviaLobbyHub>("/hubs/trivia-lobby");
app.MapHub<TriviaRankingHub>("/hubs/trivia-ranking");

app.Run();

public partial class Program { }
