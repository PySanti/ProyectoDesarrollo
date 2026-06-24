using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class TransferirLiderazgoDomainTests
{
    [Fact]
    public void TransferirLiderazgo_Should_Move_Leadership_To_Another_Member()
    {
        var lider = Guid.NewGuid();
        var nuevoLider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(nuevoLider);

        var result = equipo.TransferirLiderazgo(lider, nuevoLider);

        Assert.Equal(lider, result.LiderAnteriorUserId);
        Assert.Equal(nuevoLider, result.NuevoLiderUserId);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Equal(2, equipo.Participantes.Count);
        Assert.Single(equipo.Participantes.Where(x => x.EsLider));
        Assert.False(equipo.Participantes.Single(x => x.UsuarioId == lider).EsLider);
        Assert.True(equipo.Participantes.Single(x => x.UsuarioId == nuevoLider).EsLider);
    }

    [Fact]
    public void TransferirLiderazgo_Should_Reject_When_Actor_Is_Not_Current_Leader()
    {
        var lider = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(actor);
        equipo.AgregarParticipante(target);

        var ex = Assert.Throws<ActorNoEsLiderEquipoException>(() => equipo.TransferirLiderazgo(actor, target));

        Assert.Contains("no es el lider", ex.Message);
        Assert.True(equipo.Participantes.Single(x => x.UsuarioId == lider).EsLider);
    }

    [Fact]
    public void TransferirLiderazgo_Should_Reject_When_Target_Is_Not_Member()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());

        var ex = Assert.Throws<NuevoLiderNoPerteneceAlEquipoException>(() => equipo.TransferirLiderazgo(lider, Guid.NewGuid()));

        Assert.Contains("no pertenece", ex.Message);
        Assert.True(equipo.Participantes.Single(x => x.UsuarioId == lider).EsLider);
    }

    [Fact]
    public void TransferirLiderazgo_Should_Reject_When_Team_Has_No_Eligible_Target_Member()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);

        var ex = Assert.Throws<NuevoLiderNoPerteneceAlEquipoException>(() => equipo.TransferirLiderazgo(lider, Guid.NewGuid()));

        Assert.Contains("no pertenece", ex.Message);
        Assert.Single(equipo.Participantes);
        Assert.True(equipo.Participantes.Single(x => x.UsuarioId == lider).EsLider);
    }

    [Fact]
    public void TransferirLiderazgo_Should_Reject_When_Target_Is_Current_Leader()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(Guid.NewGuid());

        var ex = Assert.Throws<NuevoLiderDebeSerDiferenteException>(() => equipo.TransferirLiderazgo(lider, lider));

        Assert.Contains("debe ser diferente", ex.Message);
    }

    [Fact]
    public void TransferirLiderazgo_Should_Reject_When_Team_Is_Not_Active()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.Salir(lider);

        var ex = Assert.Throws<EquipoNoActivoException>(() => equipo.TransferirLiderazgo(lider, Guid.NewGuid()));

        Assert.Contains("no esta activo", ex.Message);
    }
}
