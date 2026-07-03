using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class BdtLeafTypesTests
{
    [Fact]
    public void TesoroQR_self_generates_id_and_holds_fields()
    {
        var participante = Guid.NewGuid();
        var t = new TesoroQR(participante, "QR-1", ResultadoValidacionQR.Valido, new DateTime(2026, 6, 28));
        Assert.NotEqual(Guid.Empty, t.Id);
        Assert.Equal(participante, t.ParticipanteId);
        Assert.Equal("QR-1", t.QrDecodificado);
        Assert.Equal(ResultadoValidacionQR.Valido, t.Resultado);
    }

    [Fact]
    public void Enums_have_expected_members()
    {
        Assert.Equal(5, Enum.GetValues<EstadoEtapa>().Length);
        Assert.Equal(4, Enum.GetValues<ResultadoValidacionQR>().Length);
        Assert.Equal(3, Enum.GetValues<MotivoCierreEtapa>().Length);
    }
}
