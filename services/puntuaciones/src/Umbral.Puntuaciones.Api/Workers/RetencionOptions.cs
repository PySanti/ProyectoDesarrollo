namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RetencionOptions
{
    public const string SectionName = "Retencion";

    public int EventosProcesadosDias { get; set; } = 30;
}
