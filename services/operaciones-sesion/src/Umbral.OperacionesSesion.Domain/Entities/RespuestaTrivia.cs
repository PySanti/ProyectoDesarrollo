namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class RespuestaTrivia
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid OpcionId { get; private set; }
    public bool EsCorrecta { get; private set; }
    public DateTime Instante { get; private set; }
    public Guid? EquipoId { get; private set; }

    private RespuestaTrivia() { } // EF

    public RespuestaTrivia(Guid participanteId, Guid opcionId, bool esCorrecta, DateTime instante, Guid? equipoId = null)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        OpcionId = opcionId;
        EsCorrecta = esCorrecta;
        Instante = instante;
        EquipoId = equipoId;
    }
}
