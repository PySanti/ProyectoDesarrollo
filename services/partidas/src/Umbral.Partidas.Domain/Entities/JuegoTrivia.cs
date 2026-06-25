using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed record OpcionSpec(string Texto, bool EsCorrecta);

public sealed record PreguntaSpec(
    string Texto,
    IReadOnlyList<OpcionSpec> Opciones,
    int Puntaje,
    int TiempoLimiteSegundos);

public sealed class JuegoTrivia
{
    private readonly List<Pregunta> _preguntas = new();

    public JuegoId JuegoId { get; private set; }
    public PartidaId PartidaId { get; private set; }
    public int Orden { get; private set; }
    public EstadoJuego Estado { get; private set; }

    public IReadOnlyList<Pregunta> Preguntas => _preguntas;

    private JuegoTrivia() { } // EF

    private JuegoTrivia(PartidaId partidaId, int orden)
    {
        JuegoId = JuegoId.New();
        PartidaId = partidaId;
        Orden = orden;
        Estado = EstadoJuego.Pendiente;
    }

    public static JuegoTrivia Crear(PartidaId partidaId, int orden, IEnumerable<PreguntaSpec> preguntas)
    {
        var juego = new JuegoTrivia(partidaId, orden);
        foreach (var p in preguntas ?? Enumerable.Empty<PreguntaSpec>())
        {
            juego.AgregarPregunta(
                p.Texto,
                p.Opciones.Select(o => (o.Texto, o.EsCorrecta)),
                p.Puntaje,
                p.TiempoLimiteSegundos);
        }

        if (juego._preguntas.Count == 0)
            throw new JuegoTriviaSinPreguntasException();

        return juego;
    }

    public void AgregarPregunta(
        string texto,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones,
        int puntaje,
        int tiempoLimiteSegundos)
    {
        _preguntas.Add(Pregunta.Crear(texto, opciones, puntaje, tiempoLimiteSegundos));
    }
}
