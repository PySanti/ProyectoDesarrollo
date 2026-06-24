using Umbral.OperacionesSesion.Api.Middleware;
using Umbral.OperacionesSesion.Application;
using Umbral.OperacionesSesion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOperacionesSesionApplication();
builder.Services.AddOperacionesSesionInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
