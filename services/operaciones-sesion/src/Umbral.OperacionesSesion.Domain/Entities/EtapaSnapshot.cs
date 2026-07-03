using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class EtapaSnapshot
{
    private readonly List<TesoroQR> _tesoros = new();

    public Guid EtapaId { get; private set; }
    public int Orden { get; private set; }
    public string CodigoQREsperado { get; private set; } = null!;
    public int Puntaje { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public EstadoEtapa Estado { get; private set; } = EstadoEtapa.Pendiente;
    public DateTime? FechaActivacion { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public MotivoCierreEtapa? MotivoCierre { get; private set; }
    public Guid? GanadorParticipanteId { get; private set; }
    public Guid? GanadorEquipoId { get; private set; }
    public long? TiempoResolucionMs { get; private set; }

    public IReadOnlyList<TesoroQR> Tesoros => _tesoros;

    private EtapaSnapshot() { } // EF

    public EtapaSnapshot(Guid etapaId, int orden, string codigoQREsperado, int puntaje, int tiempoLimiteSegundos)
    {
        EtapaId = etapaId;
        Orden = orden;
        CodigoQREsperado = codigoQREsperado;
        Puntaje = puntaje;
        TiempoLimiteSegundos = tiempoLimiteSegundos;
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoEtapa.Pendiente)
            throw new InvalidOperationException($"La etapa {EtapaId} no está pendiente.");
        Estado = EstadoEtapa.Activa;
        FechaActivacion = now;
    }

    internal (bool CerroEtapa, bool Gano, int? Puntaje, long? TiempoResolucionMs) RegistrarTesoro(
        Guid participanteId, Guid? equipoId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime now)
    {
        if (Estado != EstadoEtapa.Activa)
            throw new InvalidOperationException($"La etapa {EtapaId} no está activa.");

        _tesoros.Add(new TesoroQR(participanteId, qrDecodificado, resultado, now, equipoId));

        var dentroDeVentana = now < FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos);
        if (resultado == ResultadoValidacionQR.Valido && dentroDeVentana)
        {
            var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
            Estado = EstadoEtapa.Ganada;
            FechaCierre = now;
            MotivoCierre = MotivoCierreEtapa.Ganador;
            GanadorParticipanteId = participanteId;
            GanadorEquipoId = equipoId;
            TiempoResolucionMs = tiempoMs;
            return (true, true, Puntaje, tiempoMs);
        }
        return (false, false, null, null);
    }

    internal void CerrarPorTiempo(DateTime now) => Cerrar(EstadoEtapa.CerradaPorTiempo, MotivoCierreEtapa.Tiempo, now);
    internal void CerrarPorOperador(DateTime now) => Cerrar(EstadoEtapa.Cerrada, MotivoCierreEtapa.AvanceOperador, now);

    private void Cerrar(EstadoEtapa estado, MotivoCierreEtapa motivo, DateTime now)
    {
        if (Estado != EstadoEtapa.Activa)
            throw new InvalidOperationException($"La etapa {EtapaId} no está activa.");
        Estado = estado;
        FechaCierre = now;
        MotivoCierre = motivo;
    }
}
