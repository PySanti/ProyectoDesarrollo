using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.UnitTests;

public sealed class PartidaBdtDomainTests
{
    [Fact]
    public void CrearPublicada_Should_Create_Lobby_Game_With_Read_Fields()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });

        Assert.NotEqual(Guid.Empty, partida.PartidaId);
        Assert.Equal("Ruta QR", partida.Nombre);
        Assert.Equal(Modalidad.Individual, partida.Modalidad);
        Assert.Equal(EstadoPartida.Lobby, partida.Estado);
        Assert.Equal("Campus norte", partida.AreaBusqueda.Descripcion);
        Assert.Single(partida.Etapas);
    }

    [Theory]
    [InlineData(Modalidad.Individual)]
    [InlineData(Modalidad.Equipo)]
    public void Modalidad_Should_Support_Individual_And_Equipo(Modalidad modalidad)
    {
        var partida = PartidaBDT.CrearPublicada(
            "BDT",
            modalidad,
            new AreaBusqueda("Area"),
            new[] { EtapaBDT.Crear(1, "QR", 60) });

        Assert.Equal(modalidad, partida.Modalidad);
    }

    [Fact]
    public void CrearPublicada_Should_Reject_Game_Without_Stages()
    {
        Assert.Throws<ArgumentException>(() => PartidaBDT.CrearPublicada(
            "BDT",
            Modalidad.Equipo,
            new AreaBusqueda("Area"),
            Array.Empty<EtapaBDT>()));
    }
}
