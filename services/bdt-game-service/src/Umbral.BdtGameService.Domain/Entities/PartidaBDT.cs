using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;

namespace Umbral.BdtGameService.Domain.Entities;

public sealed class PartidaBDT
{
    public Guid PartidaId { get; private set; }
    public string Nombre { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public EstadoPartida Estado { get; private set; }
    public AreaBusqueda AreaBusqueda { get; private set; }
    public List<EtapaBDT> Etapas { get; private set; } = new();

    private PartidaBDT()
    {
        Nombre = string.Empty;
        AreaBusqueda = new AreaBusqueda("Sin area configurada");
    }

    private PartidaBDT(string nombre, Modalidad modalidad, AreaBusqueda areaBusqueda, IEnumerable<EtapaBDT> etapas, EstadoPartida estado)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("Nombre requerido", nameof(nombre));
        }

        var etapasList = etapas.ToList();
        if (etapasList.Count == 0)
        {
            throw new ArgumentException("Una partida BDT debe tener al menos una etapa.", nameof(etapas));
        }

        PartidaId = Guid.NewGuid();
        Nombre = nombre.Trim();
        Modalidad = modalidad;
        Estado = estado;
        AreaBusqueda = areaBusqueda;
        Etapas = etapasList;
    }

    public static PartidaBDT CrearPublicada(string nombre, Modalidad modalidad, AreaBusqueda areaBusqueda, IEnumerable<EtapaBDT> etapas)
    {
        return new PartidaBDT(nombre, modalidad, areaBusqueda, etapas, EstadoPartida.Lobby);
    }

    public static PartidaBDT CrearNoPublicada(string nombre, Modalidad modalidad, AreaBusqueda areaBusqueda, IEnumerable<EtapaBDT> etapas, EstadoPartida estado)
    {
        if (estado == EstadoPartida.Lobby)
        {
            throw new ArgumentException("Use CrearPublicada para partidas en Lobby.", nameof(estado));
        }

        return new PartidaBDT(nombre, modalidad, areaBusqueda, etapas, estado);
    }
}
