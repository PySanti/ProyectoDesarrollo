using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class JuegoResumen
{
    private readonly List<PreguntaSnapshot> _preguntas = new();
    private readonly List<EtapaSnapshot> _etapas = new();

    public Guid JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }
    public EstadoJuego Estado { get; private set; } = EstadoJuego.Pendiente;
    public string AreaBusqueda { get; private set; } = string.Empty;

    public IReadOnlyList<PreguntaSnapshot> Preguntas => _preguntas;
    public PreguntaSnapshot? PreguntaActiva => _preguntas.FirstOrDefault(p => p.Estado == EstadoPregunta.Activa);
    public bool TienePreguntasAbiertas =>
        _preguntas.Any(p => p.Estado is EstadoPregunta.Activa or EstadoPregunta.Pendiente);

    public IReadOnlyList<EtapaSnapshot> Etapas => _etapas;
    public EtapaSnapshot? EtapaActiva => _etapas.FirstOrDefault(e => e.Estado == EstadoEtapa.Activa);
    public bool TieneEtapasAbiertas =>
        _etapas.Any(e => e.Estado is EstadoEtapa.Activa or EstadoEtapa.Pendiente);

    private JuegoResumen() { } // EF

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego)
        : this(juegoId, orden, tipoJuego, Enumerable.Empty<PreguntaSnapshot>()) { }

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, IEnumerable<PreguntaSnapshot> preguntas)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
        _preguntas.AddRange(preguntas);
    }

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, string areaBusqueda, IEnumerable<EtapaSnapshot> etapas)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
        AreaBusqueda = areaBusqueda;
        _etapas.AddRange(etapas);
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoJuego.Pendiente)
            throw new InvalidOperationException($"El juego {JuegoId} no está pendiente.");
        Estado = EstadoJuego.Activo;
        if (TipoJuego == TipoJuego.Trivia)
            ActivarSiguientePregunta(now);
        else if (TipoJuego == TipoJuego.BusquedaDelTesoro)
            ActivarSiguienteEtapa(now);
    }

    internal void Finalizar()
    {
        if (Estado != EstadoJuego.Activo)
            throw new InvalidOperationException($"El juego {JuegoId} no está activo.");
        Estado = EstadoJuego.Finalizado;
    }

    internal PreguntaSnapshot? ActivarSiguientePregunta(DateTime now)
    {
        var siguiente = _preguntas
            .Where(p => p.Estado == EstadoPregunta.Pendiente)
            .OrderBy(p => p.Orden)
            .FirstOrDefault();
        siguiente?.Activar(now);
        return siguiente;
    }

    internal EtapaSnapshot? ActivarSiguienteEtapa(DateTime now)
    {
        var siguiente = _etapas
            .Where(e => e.Estado == EstadoEtapa.Pendiente)
            .OrderBy(e => e.Orden)
            .FirstOrDefault();
        siguiente?.Activar(now);
        return siguiente;
    }
}
