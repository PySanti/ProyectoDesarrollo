using Umbral.TeamService.Domain.Entities;
using Umbral.TeamService.Domain.Enums;

namespace Umbral.TeamService.UnitTests;

public sealed class CrearEquipoDomainTests
{
    [Fact]
    public void CrearPorParticipante_Should_Create_ActiveTeam_With_OneLeaderMember()
    {
        var actorUserId = Guid.NewGuid();

        var equipo = Equipo.CrearPorParticipante("Equipo A", "ABCD2345", actorUserId);

        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Single(equipo.Participantes);
        Assert.Equal("ABCD2345", equipo.CodigoAcceso);

        var creador = equipo.Participantes.Single();
        Assert.Equal(actorUserId, creador.UsuarioId);
        Assert.True(creador.EsLider);
    }
}
