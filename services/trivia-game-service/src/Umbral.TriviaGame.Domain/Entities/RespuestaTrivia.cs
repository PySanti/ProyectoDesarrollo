using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Entities;

public sealed class RespuestaTrivia : Entity<RespuestaTriviaId>
{
    public PartidaId PartidaId { get; }
    public QuestionId PreguntaId { get; }
    public string UsuarioId { get; }
    public int OpcionSeleccionadaIndex { get; }
    public bool EsCorrecta { get; }
    public int PuntajeObtenido { get; }
    public DateTimeOffset FechaRespuesta { get; }

    private RespuestaTrivia() : base(RespuestaTriviaId.New()) { }

    private RespuestaTrivia(
        RespuestaTriviaId id,
        PartidaId partidaId,
        QuestionId preguntaId,
        string usuarioId,
        int opcionSeleccionadaIndex,
        bool esCorrecta,
        int puntajeObtenido,
        DateTimeOffset fechaRespuesta)
        : base(id)
    {
        PartidaId = partidaId;
        PreguntaId = preguntaId;
        UsuarioId = usuarioId;
        OpcionSeleccionadaIndex = opcionSeleccionadaIndex;
        EsCorrecta = esCorrecta;
        PuntajeObtenido = puntajeObtenido;
        FechaRespuesta = fechaRespuesta;
    }

    public static RespuestaTrivia Create(
        PartidaId partidaId,
        QuestionId preguntaId,
        string usuarioId,
        int opcionSeleccionadaIndex,
        bool esCorrecta,
        int assignedScore)
    {
        if (partidaId is null)
            throw new DomainValidationException("El identificador de la partida es obligatorio.");
        if (preguntaId is null)
            throw new DomainValidationException("El identificador de la pregunta es obligatorio.");
        if (string.IsNullOrWhiteSpace(usuarioId))
            throw new DomainValidationException("El identificador del usuario es obligatorio.");
        if (opcionSeleccionadaIndex < 0 || opcionSeleccionadaIndex > 3)
            throw new DomainValidationException("El índice de la opción seleccionada debe estar entre 0 y 3.");

        var id = RespuestaTriviaId.New();
        var now = DateTimeOffset.UtcNow;
        var puntaje = esCorrecta ? assignedScore : 0;

        return new RespuestaTrivia(id, partidaId, preguntaId, usuarioId,
            opcionSeleccionadaIndex, esCorrecta, puntaje, now);
    }

    public override string ToString() =>
        $"RespuestaTrivia {{ Id: {Id}, PartidaId: {PartidaId.Value}, PreguntaId: {PreguntaId.Value}, " +
        $"UsuarioId: \"{UsuarioId}\", EsCorrecta: {EsCorrecta}, Puntaje: {PuntajeObtenido} }}";
}
