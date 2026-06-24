using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class UnirseAEquipoDomainTests
{
    [Fact]
    public void AgregarParticipante_Should_Add_NonLeader_Member()
    {
        var creador = Guid.NewGuid();
        var nuevo = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", creador);

        equipo.AgregarParticipante(nuevo);

        Assert.Equal(2, equipo.Participantes.Count);
        var nuevoIntegrante = equipo.Participantes.Single(x => x.UsuarioId == nuevo);
        Assert.False(nuevoIntegrante.EsLider);
    }

    [Fact]
    public void AgregarParticipante_Should_Throw_When_Member_Already_Exists()
    {
        var creador = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", creador);

        var ex = Assert.Throws<InvalidOperationException>(() => equipo.AgregarParticipante(creador));

        Assert.Contains("ya pertenece", ex.Message);
    }

    [Fact]
    public void AgregarParticipante_Should_Throw_When_Team_Is_Full()
    {
        var equipo = Equipo.CrearPorParticipante("Equipo A", Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());
        equipo.AgregarParticipante(Guid.NewGuid());

        var ex = Assert.Throws<InvalidOperationException>(() => equipo.AgregarParticipante(Guid.NewGuid()));

        Assert.Contains("maximo de 5", ex.Message);
    }
}
