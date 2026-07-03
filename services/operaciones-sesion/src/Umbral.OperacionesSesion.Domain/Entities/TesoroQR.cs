using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class TesoroQR
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public string? QrDecodificado { get; private set; }
    public ResultadoValidacionQR Resultado { get; private set; }
    public DateTime FechaEnvio { get; private set; }

    private TesoroQR() { } // EF

    public TesoroQR(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime fechaEnvio, Guid? equipoId = null)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        EquipoId = equipoId;
        QrDecodificado = qrDecodificado;
        Resultado = resultado;
        FechaEnvio = fechaEnvio;
    }
}
