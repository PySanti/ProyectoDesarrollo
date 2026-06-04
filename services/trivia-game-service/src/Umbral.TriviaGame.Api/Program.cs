using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Umbral.TriviaGame.Api.Constants;
using Umbral.TriviaGame.Api.Hubs;
using Umbral.TriviaGame.Api.Middleware;
using Umbral.TriviaGame.Api.Services;
using Umbral.TriviaGame.Application;
using Umbral.TriviaGame.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Umbral.TriviaGame.Infrastructure;
using Umbral.TriviaGame.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            SignatureValidator = (token, _) => new JsonWebToken(token),
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.Operador, policy =>
        policy.RequireRole(PolicyNames.Operador));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:8081", "http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT:",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TriviaLobbyHub>("/hubs/trivia-lobby");
app.MapHub<TriviaRankingHub>("/hubs/trivia-ranking");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TriviaGameDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
