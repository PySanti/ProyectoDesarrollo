using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// S6 — defensa en profundidad de la separación lectura/mutación en Identity:
/// el GET del listado (dropdowns de la web) se amplía, pero las mutaciones siguen cerradas.
/// - Directorio de usuarios: leer con GestionarEquipos; mutar sigue Administrador.
/// - Listado de equipos: leer con GestionarPartidas; mutar sigue GestionarEquipos.
/// </summary>
public sealed class S6LecturasListadoContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public S6LecturasListadoContractTests(IdentityApiFactory factory) => _factory = factory;

    // ── /identity/users — lectura DirectorioUsuarios, mutación AdminOnly ──────────

    [Fact]
    public async Task Usuarios_lista_GET_con_GestionarEquipos_pasa()
    {
        // Antes era 403 (controller AdminOnly completo): el dropdown de líder ya puede leer.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Usuarios_lista_GET_con_Operador_es_403()
    {
        // El directorio no se abre al Operador simple: solo Administrador/GestionarEquipos.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador");

        var response = await client.GetAsync("/identity/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Usuarios_mutacion_con_GestionarEquipos_sigue_403()
    {
        // Solo se amplió el GET del listado: desactivar (mutar) sigue Administrador-only.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.PatchAsync($"/identity/users/{Guid.NewGuid()}/deactivation", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── /identity/admin/teams — lectura ListadoEquipos, mutación GestionarEquipos ──

    [Fact]
    public async Task Equipos_lista_GET_con_GestionarPartidas_pasa()
    {
        // Antes era 403 (controller GestionarEquipos): el dropdown de rendimiento (M6) ya lista.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarPartidas");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Equipos_mutacion_con_GestionarPartidas_sigue_403()
    {
        // Solo se amplió el GET del listado: borrar equipo (mutar) sigue GestionarEquipos-only.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarPartidas");

        var response = await client.DeleteAsync($"/identity/admin/teams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
