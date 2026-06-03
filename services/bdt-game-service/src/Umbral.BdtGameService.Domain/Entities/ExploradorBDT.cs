using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Domain.Entities;

public sealed class ExploradorBDT
{
    public Guid ExploradorId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid CompetidorId { get; private set; }
    public TipoCompetidor TipoCompetidor { get; private set; }
    public DateTime FechaInscripcionUtc { get; private set; }
    public int EtapasGanadas { get; private set; }
    public int TiempoAcumuladoEtapasGanadasSegundos { get; private set; }

    private ExploradorBDT()
    {
    }

    private ExploradorBDT(Guid partidaId, Guid competidorId, TipoCompetidor tipoCompetidor, DateTime fechaInscripcionUtc)
    {
        if (partidaId == Guid.Empty)
        {
            throw new ArgumentException("PartidaId requerido", nameof(partidaId));
        }

        if (competidorId == Guid.Empty)
        {
            throw new ArgumentException("CompetidorId requerido", nameof(competidorId));
        }

        ExploradorId = Guid.NewGuid();
        PartidaId = partidaId;
        CompetidorId = competidorId;
        TipoCompetidor = tipoCompetidor;
        FechaInscripcionUtc = fechaInscripcionUtc;
        EtapasGanadas = 0;
        TiempoAcumuladoEtapasGanadasSegundos = 0;
    }

    public static ExploradorBDT CrearIndividual(Guid partidaId, Guid participanteUserId, DateTime fechaInscripcionUtc)
    {
        return new ExploradorBDT(partidaId, participanteUserId, TipoCompetidor.Usuario, fechaInscripcionUtc);
    }
}
