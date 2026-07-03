using System.IO;
using Umbral.OperacionesSesion.Api.Realtime;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class RealtimeContractTests
{
    private static string LeerContrato()
    {
        // Subir desde el bin de test hasta la raíz del repo y abrir el contrato.
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "http", "operaciones-sesion-api.md")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "http", "operaciones-sesion-api.md"));
    }

    [Theory]
    [InlineData(nameof(SesionRealtimeMessages.PartidaEnLobby))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaIniciada))]
    [InlineData(nameof(SesionRealtimeMessages.JuegoActivado))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaCancelada))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaFinalizada))]
    [InlineData(nameof(SesionRealtimeMessages.PreguntaActivada))]
    [InlineData(nameof(SesionRealtimeMessages.PreguntaCerrada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaActivada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaCerrada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaGanada))]
    [InlineData(nameof(SesionRealtimeMessages.UbicacionActualizada))]
    [InlineData(nameof(SesionRealtimeMessages.PistaEnviada))]
    [InlineData(nameof(SesionRealtimeMessages.ConvocatoriaCreada))]
    public void Cada_mensaje_del_codigo_esta_documentado(string mensaje)
    {
        var contrato = LeerContrato();
        Assert.Contains(mensaje, contrato);
    }

    [Fact]
    public void El_hub_esta_documentado()
    {
        var contrato = LeerContrato();
        Assert.Contains("/operaciones-sesion/hubs/sesion", contrato);
        Assert.Contains("access_token", contrato);
    }
}
