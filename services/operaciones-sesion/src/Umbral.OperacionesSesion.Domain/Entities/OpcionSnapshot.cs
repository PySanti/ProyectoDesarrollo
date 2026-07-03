namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class OpcionSnapshot
{
    public Guid OpcionId { get; private set; }
    public string Texto { get; private set; } = null!;
    public bool EsCorrecta { get; private set; }

    private OpcionSnapshot() { } // EF

    public OpcionSnapshot(Guid opcionId, string texto, bool esCorrecta)
    {
        OpcionId = opcionId;
        Texto = texto;
        EsCorrecta = esCorrecta;
    }
}
