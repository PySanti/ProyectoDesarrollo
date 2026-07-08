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

    [Fact]
    public async Task GetPartidasTerminadasConMarcadorDeEquipo_filtra_por_estado_modalidad_y_equipo()
    {
        var equipoId = Guid.NewGuid();
        var terminadaReciente = Guid.NewGuid();
        var terminadaAntigua = Guid.NewGuid();
        var iniciada = Guid.NewGuid();
        var individual = Guid.NewGuid();
        var sinMarcador = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IPuntuacionesUnitOfWork>();

            void SembrarPartida(Guid partidaId, Modalidad modalidad, bool terminada, DateTime? fechaFin)
            {
                var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), modalidad);
                if (terminada) { partida.MarcarTerminada(fechaFin!.Value); }
                repo.AddPartida(partida);
            }

            void SembrarMarcador(Guid partidaId, TipoCompetidor tipo)
            {
                var marcador = Marcador.Nuevo(Guid.NewGuid(), equipoId, partidaId, tipo);
                marcador.Acreditar(10, 1000);
                repo.AddMarcador(marcador);
            }

            SembrarPartida(terminadaReciente, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(terminadaReciente, TipoCompetidor.Equipo);
            SembrarPartida(terminadaAntigua, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(terminadaAntigua, TipoCompetidor.Equipo);
            SembrarPartida(iniciada, Modalidad.Equipo, terminada: false, null);
            SembrarMarcador(iniciada, TipoCompetidor.Equipo);
            SembrarPartida(individual, Modalidad.Individual, terminada: true, new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
            SembrarMarcador(individual, TipoCompetidor.Participante);
            SembrarPartida(sinMarcador, Modalidad.Equipo, terminada: true, new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc));

            await uow.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProyeccionesRepository>();

            var partidas = await repo.GetPartidasTerminadasConMarcadorDeEquipoAsync(equipoId, CancellationToken.None);
            var marcadores = await repo.GetMarcadoresDePartidaAsync(terminadaReciente, CancellationToken.None);

            Assert.Equal(new[] { terminadaReciente, terminadaAntigua }, partidas.Select(p => p.PartidaId).ToArray());
            Assert.Single(marcadores);
        }
    }
}
