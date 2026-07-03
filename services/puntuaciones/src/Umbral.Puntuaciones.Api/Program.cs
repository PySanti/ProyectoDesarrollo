using Umbral.Puntuaciones.Api.Middleware;
using Umbral.Puntuaciones.Application;
using Umbral.Puntuaciones.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPuntuacionesApplication();
builder.Services.AddPuntuacionesInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var rabbitOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions();
builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.OperacionesSesionEventsConsumer>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}
