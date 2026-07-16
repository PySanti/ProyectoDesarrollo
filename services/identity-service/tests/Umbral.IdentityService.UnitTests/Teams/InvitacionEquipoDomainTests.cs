using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class InvitacionEquipoDomainTests
{
    [Fact]
    public void Crear_Should_StartPendiente()
    {
        var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(EstadoInvitacion.Pendiente, inv.Estado);
    }

    [Fact]
    public void Aceptar_Should_SetAceptada()
    {
        var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inv.Aceptar();
        Assert.Equal(EstadoInvitacion.Aceptada, inv.Estado);
    }

    [Fact]
    public void Aceptar_Should_Throw_When_NotPendiente()
    {
        var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inv.Rechazar();
        Assert.Throws<InvitacionNoPendienteException>(() => inv.Aceptar());
    }

    [Fact]
    public void Rechazar_Should_SetRechazada()
    {
        var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inv.Rechazar();
        Assert.Equal(EstadoInvitacion.Rechazada, inv.Estado);
    }

    [Fact]
    public void Rechazar_Should_Throw_When_NotPendiente()
    {
        var inv = InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        inv.Aceptar();
        Assert.Throws<InvitacionNoPendienteException>(() => inv.Rechazar());
    }

    [Fact]
    public void Crear_Should_Assign_CorrectIds()
    {
        var equipoId = Guid.NewGuid();
        var invitadoUserId = Guid.NewGuid();
        var invitadoPorUserId = Guid.NewGuid();

        var inv = InvitacionEquipo.Crear(equipoId, invitadoUserId, invitadoPorUserId);

        Assert.Equal(equipoId, inv.EquipoId);
        Assert.Equal(invitadoUserId, inv.InvitadoSubjectId);
        Assert.Equal(invitadoPorUserId, inv.InvitadoPorSubjectId);
        Assert.NotEqual(Guid.Empty, inv.InvitacionEquipoId);
    }

    [Fact]
    public void Crear_Should_Throw_When_EquipoId_IsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            InvitacionEquipo.Crear(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Crear_Should_Throw_When_InvitadoSubjectId_IsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            InvitacionEquipo.Crear(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Crear_Should_Throw_When_InvitadoPorSubjectId_IsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            InvitacionEquipo.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }
}
