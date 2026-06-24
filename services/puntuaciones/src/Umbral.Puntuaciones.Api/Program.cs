using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application;
using Umbral.Puntuaciones.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPuntuacionesApplication();
builder.Services.AddPuntuacionesInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
