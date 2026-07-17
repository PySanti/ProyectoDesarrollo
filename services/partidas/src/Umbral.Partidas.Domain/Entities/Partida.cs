using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class Partida
{
    private readonly List<JuegoReferencia> _juegos = new();

    public PartidaId PartidaId { get; private set; }
    public NombrePartida NombrePartida { get; private set; } = null!;
    // Siempre null: ADR-0010 dejo el estado de runtime en Operaciones de Sesion y este
    // servicio nunca lo escribe. La columna "Estado" del listado web es un problema
    // abierto, fuera del alcance de este slice (ver el spec de 2026-07-16, seccion Alcance).
    public EstadoPartida? Estado { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public ModoInicioPartida ModoInicioPartida { get; private set; }
    public DateTime? TiempoInicio { get; private set; }
    public int MinimosParticipacion { get; private set; }
    public int MaximosParticipacion { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public IReadOnlyList<JuegoReferencia> Juegos => _juegos;

    private Partida() { } // EF

    private Partida(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos,
        DateTime fechaCreacion)
    {
        PartidaId = PartidaId.New();
        NombrePartida = nombre;
        Modalidad = modalidad;
        ModoInicioPartida = modo;
        TiempoInicio = tiempoInicio;
        MinimosParticipacion = minimos;
        MaximosParticipacion = maximos;
        FechaCreacion = fechaCreacion;
        Estado = null;

        ValidarParametrosParticipacion();
        ValidarParametrosInicio();
    }

    // fechaCreacion entra como parametro y no se lee del reloj aqui: el dominio no depende
    // del ambiente, y por eso los tests fijan el instante sin maquinaria (patron de
    // Operaciones, donde la fecha siempre va al final).
    public static Partida Crear(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos,
        DateTime fechaCreacion)
        => new(nombre, modalidad, modo, tiempoInicio, minimos, maximos, fechaCreacion);

    public void AgregarJuego(JuegoId juegoId, int orden, TipoJuego tipoJuego)
    {
        if (!juegoId.EsValido())
            throw new ArgumentException("JuegoId invalido.", nameof(juegoId));
        if (orden < 1)
            throw new ArgumentException("El orden debe ser mayor o igual a 1.", nameof(orden));
        if (_juegos.Any(j => j.JuegoId == juegoId))
            throw new JuegoDuplicadoException(juegoId.Valor);
        if (_juegos.Any(j => j.Orden == orden))
            throw new OrdenJuegoDuplicadoException(orden);

        _juegos.Add(new JuegoReferencia(juegoId, orden, tipoJuego));
    }

    public void ValidarIntegridadJuegos()
    {
        if (_juegos.Count == 0)
            throw new PartidaSinJuegosException(PartidaId.Valor);

        var ordenes = _juegos.Select(j => j.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new OrdenJuegosNoContiguoException(PartidaId.Valor);
        }
    }

    private void ValidarParametrosParticipacion()
    {
        if (MinimosParticipacion < 1)
            throw new ArgumentException("MinimosParticipacion debe ser mayor o igual a 1.");
        if (MaximosParticipacion < MinimosParticipacion)
            throw new ArgumentException("MaximosParticipacion debe ser mayor o igual a MinimosParticipacion.");
    }

    private void ValidarParametrosInicio()
    {
        var requiereTiempo = ModoInicioPartida is ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico;
        if (requiereTiempo && TiempoInicio is null)
            throw new ArgumentException("TiempoInicio es requerido para inicio Automatico o ManualYAutomatico.");
        if (!requiereTiempo && TiempoInicio is not null)
            throw new ArgumentException("TiempoInicio no aplica para inicio Manual.");
    }
}
