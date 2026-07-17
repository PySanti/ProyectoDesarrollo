using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class SalirDeEquipoDomainTests
{
    [Fact]
    public void Salir_Should_Remove_NonLeader_Member()
    {
        var lider = Guid.NewGuid();
        var integrante = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(integrante);

        var resultado = equipo.Salir(integrante);

        Assert.Equal(ResultadoSalidaEquipo.SalioDelEquipo, resultado);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.DoesNotContain(equipo.Participantes, x => x.SubjectId == integrante);
        Assert.Single(equipo.Participantes);
        Assert.True(equipo.Participantes.Single().EsLider);
    }

    [Fact]
    public void Salir_Should_Reject_Leader_When_Other_Members_Exist()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());

        var ex = Assert.Throws<LiderDebeTransferirLiderazgoException>(() => equipo.Salir(lider));

        Assert.Contains("debe transferir", ex.Message);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Equal(2, equipo.Participantes.Count);
        Assert.Contains(equipo.Participantes, x => x.SubjectId == lider && x.EsLider);
    }

    [Fact]
    public void Salir_Should_Mark_Team_As_Deleted_When_Only_Leader_Leaves()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var resultado = equipo.Salir(lider);

        Assert.Equal(ResultadoSalidaEquipo.EquipoEliminado, resultado);
        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.Empty(equipo.Participantes);
    }

    [Fact]
    public void Salir_Should_Reject_User_That_Is_Not_A_Member()
    {
        var equipo = Equipo.CrearPorParticipante("Equipo A", Guid.NewGuid());

        var ex = Assert.Throws<ParticipanteNoPerteneceAlEquipoException>(() => equipo.Salir(Guid.NewGuid()));

        Assert.Contains("no pertenece", ex.Message);
    }

    [Fact]
    public void Salir_Should_Reject_When_Team_Is_Not_Active()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.Salir(lider);

        var ex = Assert.Throws<EquipoNoActivoException>(() => equipo.Salir(lider));

        Assert.Contains("no esta activo", ex.Message);
    }
}
