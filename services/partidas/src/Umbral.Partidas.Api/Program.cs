using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Application;
using Umbral.Partidas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPartidasApplication();
builder.Services.AddPartidasInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
