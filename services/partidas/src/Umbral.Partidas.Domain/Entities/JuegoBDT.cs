using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed record EtapaSpec(int Orden, string CodigoQREsperado, int Puntaje, int TiempoLimiteSegundos);

public sealed class JuegoBDT
{
    private readonly List<EtapaBDT> _etapas = new();

    public JuegoId JuegoId { get; private set; }
    public PartidaId PartidaId { get; private set; }
    public int Orden { get; private set; }
    public EstadoJuego Estado { get; private set; }
    public string AreaBusqueda { get; private set; } = string.Empty;

    public IReadOnlyList<EtapaBDT> Etapas => _etapas;

    private JuegoBDT() { } // EF

    private JuegoBDT(PartidaId partidaId, int orden, string areaBusqueda)
    {
        JuegoId = JuegoId.New();
        PartidaId = partidaId;
        Orden = orden;
        Estado = EstadoJuego.Pendiente;
        AreaBusqueda = areaBusqueda.Trim();
    }

    public static JuegoBDT Crear(PartidaId partidaId, int orden, string areaBusqueda, IEnumerable<EtapaSpec> etapas)
    {
        if (string.IsNullOrWhiteSpace(areaBusqueda))
            throw new AreaBusquedaRequeridaException();

        var juego = new JuegoBDT(partidaId, orden, areaBusqueda);
        foreach (var e in etapas ?? Enumerable.Empty<EtapaSpec>())
            juego.AgregarEtapa(e.Orden, e.CodigoQREsperado, e.Puntaje, e.TiempoLimiteSegundos);

        if (juego._etapas.Count == 0)
            throw new JuegoBDTSinEtapasException();

        juego.ValidarOrdenContiguo();
        return juego;
    }

    public void AgregarEtapa(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)
    {
        if (_etapas.Any(e => e.Orden == orden))
            throw new EtapaBDTInvalidaException($"ya existe una etapa con el orden {orden}.");

        _etapas.Add(EtapaBDT.Crear(orden, codigoQr, puntaje, tiempoLimiteSegundos));
    }

    private void ValidarOrdenContiguo()
    {
        var ordenes = _etapas.Select(e => e.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new EtapaBDTInvalidaException("el orden de las etapas debe ser una secuencia contigua desde 1.");
        }
    }
}
