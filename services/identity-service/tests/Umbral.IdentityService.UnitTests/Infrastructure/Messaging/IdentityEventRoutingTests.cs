using Umbral.IdentityService.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public sealed class IdentityEventRoutingTests
{
    [Theory]
    [InlineData("EquipoEliminado", "identity.equipo-eliminado.v1")]
    [InlineData("LiderazgoEquipoModificado", "identity.liderazgo-equipo-modificado.v1")]
    [InlineData("EquipoDesactivado", "identity.equipo-desactivado.v1")]
    [InlineData("EquipoReactivado", "identity.equipo-reactivado.v1")]
    public void RoutingKeyFor_mapea_los_eventos_de_ciclo_de_vida(string eventType, string expected)
        => Assert.Equal(expected, IdentityEventRouting.RoutingKeyFor(eventType));
}
