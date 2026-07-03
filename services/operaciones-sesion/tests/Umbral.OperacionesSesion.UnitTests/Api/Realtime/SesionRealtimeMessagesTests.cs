using System;
using Umbral.OperacionesSesion.Api.Realtime;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SesionRealtimeMessagesTests
{
    [Fact]
    public void GrupoOperadorPartida_tiene_formato_estable()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("operador:partida:11111111-1111-1111-1111-111111111111",
            SesionRealtimeMessages.GrupoOperadorPartida(id));
    }

    [Fact]
    public void GrupoOperadorPartida_difiere_del_grupo_de_partida()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id),
            SesionRealtimeMessages.GrupoOperadorPartida(id));
    }

    [Fact]
    public void GrupoParticipante_tiene_formato_estable()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Assert.Equal("participante:22222222-2222-2222-2222-222222222222",
            SesionRealtimeMessages.GrupoParticipante(id));
    }

    [Fact]
    public void GrupoParticipante_difiere_de_los_otros_grupos()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id), SesionRealtimeMessages.GrupoParticipante(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoOperadorPartida(id), SesionRealtimeMessages.GrupoParticipante(id));
    }

    [Fact]
    public void GrupoEquipo_tiene_formato_estable()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Assert.Equal("equipo:33333333-3333-3333-3333-333333333333",
            SesionRealtimeMessages.GrupoEquipo(id));
    }

    [Fact]
    public void GrupoEquipo_difiere_de_los_otros_grupos()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id), SesionRealtimeMessages.GrupoEquipo(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoParticipante(id), SesionRealtimeMessages.GrupoEquipo(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoOperadorPartida(id), SesionRealtimeMessages.GrupoEquipo(id));
    }
}
