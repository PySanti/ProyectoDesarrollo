using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ProyeccionesRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProyeccionesRepositoryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Repositorio_persiste_y_recupera_proyecciones()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IPuntuacionesUnitOfWork>();

            repo.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));
            repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, TipoJuego.Trivia));
            var marcador = Marcador.Nuevo(juegoId, competidorId, partidaId, TipoCompetidor.Participante);
            marcador.Acreditar(10, 1200);
            repo.AddMarcador(marcador);
            repo.RegistrarEventoProcesado(EventoProcesado.Registrar(eventId, "PuntajeTriviaIncrementado", DateTime.UtcNow, DateTime.UtcNow));
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();

            var partida = await repo.GetPartidaAsync(partidaId, CancellationToken.None);
            var juego = await repo.GetJuegoAsync(juegoId, CancellationToken.None);
            var marcador = await repo.GetMarcadorAsync(juegoId, competidorId, CancellationToken.None);
            var lista = await repo.GetMarcadoresDeJuegoAsync(juegoId, CancellationToken.None);

            Assert.NotNull(partida);
            Assert.Equal(TipoJuego.Trivia, juego!.TipoJuego);
            Assert.Equal(10, marcador!.PuntosAcumulados);
            Assert.Single(lista);
            Assert.True(await repo.EventoYaProcesadoAsync(eventId, CancellationToken.None));
            Assert.False(await repo.EventoYaProcesadoAsync(Guid.NewGuid(), CancellationToken.None));
        }
    }
}
