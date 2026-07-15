using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Persistence;
using Xunit;

namespace Umbral.IdentityService.IntegrationTests;

public sealed class DirectoryEndpointIntegrationTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public DirectoryEndpointIntegrationTests(IdentityApiFactory factory) => _factory = factory;

    private HttpClient ClienteComo(string rol)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", rol);
        return client;
    }

    [Fact]
    public async Task Resuelve_nombres_de_usuario_y_equipo_realmente_persistidos()
    {
        var sub = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Los Cazadores", Guid.NewGuid());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            // KeycloakId = sub: es el espacio de ids en el que viaja competidorId.
            db.Usuarios.Add(Usuario.Crear(sub.ToString(), "María González", $"{sub}@umbral.test", RolUsuario.Participante));
            db.Equipos.Add(equipo);
            await db.SaveChangesAsync();
        }

        var response = await ClienteComo("Participante").PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { sub }, equipoIds = new[] { equipo.EquipoId } });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var participantes = doc.RootElement.GetProperty("participantes");
        Assert.Equal(1, participantes.GetArrayLength());
        Assert.Equal(sub, participantes[0].GetProperty("participanteId").GetGuid());
        Assert.Equal("María González", participantes[0].GetProperty("nombre").GetString());
        var equipos = doc.RootElement.GetProperty("equipos");
        Assert.Equal(1, equipos.GetArrayLength());
        Assert.Equal(equipo.EquipoId, equipos[0].GetProperty("equipoId").GetGuid());
        Assert.Equal("Los Cazadores", equipos[0].GetProperty("nombreEquipo").GetString());
    }

    [Fact]
    public async Task Id_inexistente_se_omite_y_el_conocido_se_resuelve()
    {
        var sub = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Usuarios.Add(Usuario.Crear(sub.ToString(), "Ana", $"{sub}@umbral.test", RolUsuario.Participante));
            await db.SaveChangesAsync();
        }
        var desconocido = Guid.NewGuid();

        var response = await ClienteComo("Operador").PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { sub, desconocido }, equipoIds = Array.Empty<Guid>() });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var participantes = doc.RootElement.GetProperty("participantes");
        Assert.Equal(1, participantes.GetArrayLength());
        Assert.Equal(sub, participantes[0].GetProperty("participanteId").GetGuid());
    }
}
