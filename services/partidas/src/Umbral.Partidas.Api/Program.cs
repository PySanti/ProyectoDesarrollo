using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Application;
using Umbral.Partidas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPartidasApplication();
builder.Services.AddPartidasInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
