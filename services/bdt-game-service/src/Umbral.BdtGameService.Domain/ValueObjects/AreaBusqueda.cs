namespace Umbral.BdtGameService.Domain.ValueObjects;

public sealed class AreaBusqueda
{
    public string Descripcion { get; private set; }

    private AreaBusqueda()
    {
        Descripcion = string.Empty;
    }

    public AreaBusqueda(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("AreaBusqueda requerida", nameof(descripcion));
        }

        Descripcion = descripcion.Trim();
    }
}
