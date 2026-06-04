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

    [Fact]
    public void IniciarManualmente_Should_Start_Lobby_Game_With_Minimum_Participation()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        var startedAt = DateTime.UtcNow;

        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), startedAt);

        Assert.Equal(EstadoPartida.Iniciada, partida.Estado);
        Assert.Equal(EstadoEtapa.Activa, etapaActiva.Estado);
        Assert.Equal(1, etapaActiva.Orden);
        Assert.Equal(startedAt, etapaActiva.IniciadaEnUtc);
        Assert.Equal(startedAt.AddSeconds(etapaActiva.TiempoLimiteSegundos), etapaActiva.CierraEnUtc);
        Assert.Single(partida.Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa));
        Assert.All(partida.Exploradores, explorador =>
        {
            Assert.Equal(0, explorador.EtapasGanadas);
            Assert.Equal(0, explorador.TiempoAcumuladoEtapasGanadasSegundos);
        });
    }

    [Fact]
    public void IniciarManualmente_Should_Reject_NonLobby_Game()
    {
        var partida = PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("lobby", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IniciarManualmente_Should_Reject_When_Minimum_Participation_Is_Not_Met()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 2,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("minimo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IniciarManualmente_Should_Reject_Strictly_Automatic_Game()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Automatico,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow));

        Assert.Contains("automatica", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IniciarManualmente_Should_Activate_First_Stage_By_Order_Only()
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.ManualYAutomatico,
            new[] { EtapaBDT.Crear(2, "QR-2", 90), EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);

        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(1, etapaActiva.Orden);
        Assert.Single(partida.Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa));
        Assert.Contains(partida.Etapas, etapa => etapa.Orden == 2 && etapa.Estado == EstadoEtapa.Pendiente);
    }

    [Fact]
    public void ObtenerEtapaActivaParaParticipante_Should_Return_Registered_Explorer_And_Active_Stage()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        var explorador = partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var result = partida.ObtenerEtapaActivaParaParticipante(participanteId);

        Assert.Equal(explorador.ExploradorId, result.Explorador.ExploradorId);
        Assert.Equal(etapaActiva.EtapaId, result.EtapaActiva.EtapaId);
    }

    [Fact]
    public void ObtenerEtapaActivaParaParticipante_Should_Reject_Unregistered_Participant()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<UnauthorizedAccessException>(() => partida.ObtenerEtapaActivaParaParticipante(Guid.NewGuid()));

        Assert.Contains("no esta registrado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObtenerEtapaActivaParaParticipante_Should_Reject_NonInitiated_Game()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.ObtenerEtapaActivaParaParticipante(participanteId));

        Assert.Contains("iniciada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObtenerEtapaActivaParaParticipante_Should_Reject_Initiated_Game_Without_Active_Stage()
    {
        var partida = PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus norte"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.ObtenerEtapaActivaParaParticipante(Guid.NewGuid()));

        Assert.Contains("etapa activa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistrarTesoroQr_Should_Record_Decoded_Attempt_For_Active_Stage()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        var explorador = partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var tesoro = partida.RegistrarTesoroQr(etapaActiva.EtapaId, participanteId, "bdt/tesoro.jpg", "QR-ETAPA-1", DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, tesoro.TesoroId);
        Assert.Equal(partida.PartidaId, tesoro.PartidaId);
        Assert.Equal(etapaActiva.EtapaId, tesoro.EtapaId);
        Assert.Equal(explorador.ExploradorId, tesoro.ExploradorId);
        Assert.Equal("QR-ETAPA-1", tesoro.QrDecodificado);
        Assert.Equal(EstadoProcesamientoTesoroQr.Decodificado, tesoro.EstadoProcesamiento);
        Assert.Single(partida.Tesoros);
        Assert.All(partida.Exploradores, candidate =>
        {
            Assert.Equal(0, candidate.EtapasGanadas);
            Assert.Equal(0, candidate.TiempoAcumuladoEtapasGanadasSegundos);
        });
    }

    [Fact]
    public void RegistrarTesoroQr_Should_Record_Unreadable_Attempt_With_Null_Qr()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var tesoro = partida.RegistrarTesoroQr(etapaActiva.EtapaId, participanteId, "bdt/tesoro.jpg", null, DateTime.UtcNow);

        Assert.Null(tesoro.QrDecodificado);
        Assert.Equal(EstadoProcesamientoTesoroQr.NoLegible, tesoro.EstadoProcesamiento);
        Assert.Single(partida.Tesoros);
    }

    [Fact]
    public void RegistrarTesoroQr_Should_Allow_Multiple_Attempts_While_Stage_Is_Active()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        partida.RegistrarTesoroQr(etapaActiva.EtapaId, participanteId, "bdt/tesoro-1.jpg", null, DateTime.UtcNow);
        partida.RegistrarTesoroQr(etapaActiva.EtapaId, participanteId, "bdt/tesoro-2.jpg", "QR-2", DateTime.UtcNow);

        Assert.Equal(2, partida.Tesoros.Count);
    }

    [Fact]
    public void RegistrarTesoroQr_Should_Reject_Wrong_Stage()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => partida.RegistrarTesoroQr(Guid.NewGuid(), participanteId, "bdt/tesoro.jpg", "QR", DateTime.UtcNow));

        Assert.Contains("etapa activa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistrarTesoroQr_Should_Reject_Unregistered_Participant()
    {
        var partida = CreateIndividualGame(maximoParticipantes: 2);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        var etapaActiva = partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);

        var exception = Assert.Throws<UnauthorizedAccessException>(() => partida.RegistrarTesoroQr(etapaActiva.EtapaId, Guid.NewGuid(), "bdt/tesoro.jpg", "QR", DateTime.UtcNow));

        Assert.Contains("no esta registrado", exception.Message, StringComparison.OrdinalIgnoreCase);
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
