using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class Pregunta
{
    private readonly List<Opcion> _opciones = new();

    public Guid PreguntaId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public PuntajeAsignado PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public int Orden { get; private set; }

    public IReadOnlyList<Opcion> Opciones => _opciones;

    private Pregunta() { } // EF

    internal static Pregunta Crear(
        string texto,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones,
        int puntaje,
        int tiempoLimiteSegundos,
        int orden)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new PreguntaInvalidaException("el texto es requerido.");

        var opcionesList = opciones?.ToList() ?? new List<(string, bool)>();
        if (opcionesList.Count < 2)
            throw new PreguntaInvalidaException("se requieren al menos 2 opciones.");
        if (opcionesList.Any(o => string.IsNullOrWhiteSpace(o.Item1)))
            throw new PreguntaInvalidaException("el texto de cada opcion es requerido.");
        if (opcionesList.Count(o => o.Item2) != 1)
            throw new PreguntaInvalidaException("debe haber exactamente una opcion correcta.");
        if (tiempoLimiteSegundos <= 0)
            throw new PreguntaInvalidaException("el tiempo limite debe ser positivo.");

        PuntajeAsignado puntajeVo;
        try
        {
            puntajeVo = PuntajeAsignado.Crear(puntaje);
        }
        catch (ArgumentException ex)
        {
            throw new PreguntaInvalidaException(ex.Message);
        }

        var pregunta = new Pregunta
        {
            PreguntaId = Guid.NewGuid(),
            Texto = texto.Trim(),
            PuntajeAsignado = puntajeVo,
            TiempoLimiteSegundos = tiempoLimiteSegundos,
            Orden = orden
        };
        var ordenOpcion = 0;
        foreach (var (opcionTexto, esCorrecta) in opcionesList)
            pregunta._opciones.Add(Opcion.Crear(opcionTexto, esCorrecta, ordenOpcion++));

        return pregunta;
    }
}
