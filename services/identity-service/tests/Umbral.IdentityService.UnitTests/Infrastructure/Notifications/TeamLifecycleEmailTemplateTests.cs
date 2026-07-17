using Umbral.IdentityService.Infrastructure.Services.Notifications;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Notifications;

public sealed class TeamLifecycleEmailTemplateTests
{
    [Fact]
    public void Eliminado_menciona_el_nombre_del_equipo()
    {
        var (subject, body) = TeamLifecycleEmailTemplate.BuildEquipoEliminado("Titanes");
        Assert.Contains("Titanes", body);
        Assert.False(string.IsNullOrWhiteSpace(subject));
    }

    [Fact]
    public void Liderazgo_distingue_nuevo_lider_de_anterior()
    {
        var (_, nuevo) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: true);
        var (_, anterior) = TeamLifecycleEmailTemplate.BuildLiderazgo(esNuevoLider: false);
        Assert.NotEqual(nuevo, anterior);
    }
}
