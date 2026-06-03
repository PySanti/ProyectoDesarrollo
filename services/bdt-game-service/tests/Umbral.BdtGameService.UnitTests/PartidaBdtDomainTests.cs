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

    [Fact]
    public void CrearPublicada_Should_Create_Hu34_Individual_Game_With_Limits_And_Start_Mode()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 2,
            maximoParticipantes: 20,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });

        Assert.Equal(2, partida.MinimoParticipantes);
        Assert.Equal(20, partida.MaximoParticipantes);
        Assert.Null(partida.MaximoEquipos);
        Assert.Null(partida.MinimoJugadoresPorEquipo);
        Assert.Equal(ModoInicioPartida.Manual, partida.ModoInicio);
    }

    [Fact]
    public void CrearPublicada_Should_Reject_Individual_Game_Without_Max_Players()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 2,
            maximoParticipantes: null,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) }));

        Assert.Contains("maximo de jugadores", exception.Message);
    }

    [Fact]
    public void CrearPublicada_Should_Reject_Team_Game_Without_Team_Limits()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Equipo,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 2,
            maximoParticipantes: null,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) }));

        Assert.Contains("maximo de equipos", exception.Message);
    }

    [Fact]
    public void CrearPublicada_Should_Reject_Duplicate_Stage_Order()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 2,
            maximoParticipantes: 20,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[]
            {
                EtapaBDT.Crear(1, "QR-1", 60),
                EtapaBDT.Crear(1, "QR-2", 60)
            }));

        Assert.Contains("orden duplicado", exception.Message);
    }

    [Fact]
    public void CrearPublicada_Should_Reject_Team_Game_When_Minimum_Exceeds_Max_Teams()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Equipo,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 4,
            maximoParticipantes: null,
            maximoEquipos: 3,
            minimoJugadoresPorEquipo: 1,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) }));

        Assert.Contains("maximo de equipos", exception.Message);
    }

    [Theory]
    [InlineData("", 60)]
    [InlineData("   ", 60)]
    public void Etapa_Should_Reject_Empty_Expected_Qr(string codigoQr, int tiempoLimite)
    {
        Assert.Throws<ArgumentException>(() => EtapaBDT.Crear(1, codigoQr, tiempoLimite));
    }

    [Fact]
    public void Etapa_Should_Reject_Non_Positive_Time_Limit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EtapaBDT.Crear(1, "QR", 0));
    }

    [Fact]
    public void RegistrarParticipanteIndividual_Should_Add_User_Explorer_In_Lobby_With_Capacity()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        var participanteId = Guid.NewGuid();

        var explorador = partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, explorador.ExploradorId);
        Assert.Equal(partida.PartidaId, explorador.PartidaId);
        Assert.Equal(participanteId, explorador.CompetidorId);
        Assert.Equal(TipoCompetidor.Usuario, explorador.TipoCompetidor);
        Assert.Equal(1, partida.ObtenerPosicionEnLobby(explorador.ExploradorId));
    }

    [Fact]
    public void RegistrarParticipanteIndividual_Should_Reject_NonLobby_Game()
    {
        var partida = PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("lobby", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistrarParticipanteIndividual_Should_Reject_Team_Modality()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Equipo,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: null,
            maximoEquipos: 2,
            minimoJugadoresPorEquipo: 1,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });

        var exception = Assert.Throws<InvalidOperationException>(() => partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("individual", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistrarParticipanteIndividual_Should_Reject_Duplicate_User()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        var participanteId = Guid.NewGuid();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow));

        Assert.Contains("ya esta inscrito", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistrarParticipanteIndividual_Should_Reject_Full_Capacity()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 1);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("cupos", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PartidaBDT CreateIndividualGame(int maximoParticipantes)
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: maximoParticipantes,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }
}
