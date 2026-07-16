using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class EquipoCicloVidaDomainTests
{
    private static Equipo EquipoConLiderYMiembro(out Guid lider, out Guid miembro)
    {
        lider = Guid.NewGuid();
        miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Andes", lider);
        equipo.AgregarParticipante(miembro);
        return equipo;
    }

    [Fact]
    public void CrearPorAdmin_asigna_lider_como_unico_integrante()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorAdmin("Equipo Admin", lider);

        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.Single(equipo.Participantes);
        Assert.True(equipo.Participantes[0].EsLider);
        Assert.Equal(lider, equipo.Participantes[0].SubjectId);
    }

    [Fact]
    public void EliminarPorLider_con_integrantes_elimina_y_devuelve_todos_los_miembros()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var afectados = equipo.EliminarPorLider(lider);

        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.Contains(lider, afectados);
        Assert.Contains(miembro, afectados);
        Assert.Equal(2, afectados.Count);
    }

    [Fact]
    public void EliminarPorLider_cuando_actor_no_es_lider_lanza()
    {
        var equipo = EquipoConLiderYMiembro(out _, out var miembro);
        Assert.Throws<ActorNoEsLiderEquipoException>(() => equipo.EliminarPorLider(miembro));
    }

    [Fact]
    public void EliminarPorAdmin_elimina_sin_validar_actor_y_devuelve_miembros()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var afectados = equipo.EliminarPorAdmin();

        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.Equal(new[] { lider, miembro }.OrderBy(x => x), afectados.OrderBy(x => x));
    }

    [Fact]
    public void Desactivar_y_Reactivar_alternan_estado()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        equipo.Desactivar();
        Assert.Equal(EstadoEquipo.Desactivado, equipo.Estado);
        equipo.Reactivar();
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
    }

    [Fact]
    public void Operaciones_sobre_equipo_eliminado_lanzan()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);
        equipo.EliminarPorAdmin();

        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.Desactivar());
        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.Renombrar("X"));
        Assert.Throws<EquipoEliminadoInmutableException>(() => equipo.ReasignarLiderazgoPorAdmin(miembro));
    }

    [Fact]
    public void Renombrar_cambia_el_nombre()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        equipo.Renombrar("  Nuevo Nombre  ");
        Assert.Equal("Nuevo Nombre", equipo.NombreEquipo);
    }

    [Fact]
    public void ReasignarLiderazgoPorAdmin_mueve_el_liderazgo_a_un_integrante()
    {
        var equipo = EquipoConLiderYMiembro(out var lider, out var miembro);

        var (anterior, nuevo) = equipo.ReasignarLiderazgoPorAdmin(miembro);

        Assert.Equal(lider, anterior);
        Assert.Equal(miembro, nuevo);
        Assert.True(equipo.Participantes.Single(p => p.SubjectId == miembro).EsLider);
        Assert.False(equipo.Participantes.Single(p => p.SubjectId == lider).EsLider);
    }

    [Fact]
    public void ReasignarLiderazgoPorAdmin_a_no_integrante_lanza()
    {
        var equipo = EquipoConLiderYMiembro(out _, out _);
        Assert.Throws<NuevoLiderNoPerteneceAlEquipoException>(
            () => equipo.ReasignarLiderazgoPorAdmin(Guid.NewGuid()));
    }
}
