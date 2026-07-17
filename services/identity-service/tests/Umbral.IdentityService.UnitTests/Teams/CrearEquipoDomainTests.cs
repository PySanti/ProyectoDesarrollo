using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class CrearEquipoDomainTests
{
    [Fact]
    public void CrearPorParticipante_Should_Create_ActiveTeam_With_OneLeaderMember()
    {
        var actorUserId = Guid.NewGuid();

        var equipo = Equipo.CrearPorParticipante("Equipo A", actorUserId);

        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Single(equipo.Participantes);

        var creador = equipo.Participantes.Single();
        Assert.Equal(actorUserId, creador.SubjectId);
        Assert.True(creador.EsLider);
    }
}
