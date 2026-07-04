using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Domain;

public class PartidaProyectadaTests
{
    [Fact]
    public void DesdePublicacion_queda_en_lobby_con_modalidad()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Equipo);

        Assert.Equal(EstadoPartidaProyectada.Lobby, p.Estado);
        Assert.Equal(Modalidad.Equipo, p.Modalidad);
        Assert.Null(p.FechaInicio);
        Assert.Null(p.FechaFin);
    }

    [Fact]
    public void Transiciones_normales_avanzan_estado_y_fechas()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var inicio = new DateTime(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        var fin = inicio.AddMinutes(30);

        p.MarcarIniciada(inicio);
        p.MarcarTerminada(fin);

        Assert.Equal(EstadoPartidaProyectada.Terminada, p.Estado);
        Assert.Equal(inicio, p.FechaInicio);
        Assert.Equal(fin, p.FechaFin);
    }

    [Fact]
    public void El_estado_nunca_retrocede_ante_eventos_desordenados()
    {
        // PartidaFinalizada llegó primero (stub), luego llegan Iniciada y la publicación.
        var p = PartidaProyectada.Stub(Guid.NewGuid(), Guid.NewGuid());
        var fin = new DateTime(2026, 7, 4, 11, 0, 0, DateTimeKind.Utc);

        p.MarcarTerminada(fin);
        p.MarcarIniciada(fin.AddMinutes(-30));
        p.RegistrarPublicacion(Modalidad.Individual);

        Assert.Equal(EstadoPartidaProyectada.Terminada, p.Estado);
        Assert.Equal(Modalidad.Individual, p.Modalidad);
        Assert.Equal(fin, p.FechaFin);
        Assert.Equal(fin.AddMinutes(-30), p.FechaInicio);
    }

    [Fact]
    public void Stub_no_tiene_modalidad_hasta_registrar_publicacion()
    {
        var p = PartidaProyectada.Stub(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(p.Modalidad);
        Assert.Equal(EstadoPartidaProyectada.Lobby, p.Estado);
    }

    [Fact]
    public void Cancelada_prevalece_sobre_iniciada_tardia()
    {
        var p = PartidaProyectada.DesdePublicacion(Guid.NewGuid(), Guid.NewGuid(), Modalidad.Individual);
        var t = new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);

        p.MarcarCancelada(t);
        p.MarcarIniciada(t.AddMinutes(-1));

        Assert.Equal(EstadoPartidaProyectada.Cancelada, p.Estado);
    }
}
