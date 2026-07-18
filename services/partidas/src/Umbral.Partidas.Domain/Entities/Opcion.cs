using Umbral.Partidas.Domain.Exceptions;

namespace Umbral.Partidas.Domain.Entities;

public sealed class Opcion
{
    public Guid OpcionId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public bool EsCorrecta { get; private set; }
    public int Orden { get; private set; }

    private Opcion() { } // EF

    internal static Opcion Crear(string texto, bool esCorrecta, int orden)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new PreguntaInvalidaException("el texto de cada opcion es requerido.");

        return new Opcion
        {
            OpcionId = Guid.NewGuid(),
            Texto = texto.Trim(),
            EsCorrecta = esCorrecta,
            Orden = orden
        };
    }
}
