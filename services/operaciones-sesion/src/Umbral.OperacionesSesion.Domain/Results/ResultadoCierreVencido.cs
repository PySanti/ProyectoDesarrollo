namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoCierreVencido { Ninguna, Trivia, Bdt }

public sealed record ResultadoCierreVencido(
    TipoCierreVencido Tipo,
    ResultadoAvancePregunta? Pregunta,
    ResultadoAvanceEtapa? Etapa,
    ResultadoAvance? JuegoFinalizado)
{
    public static ResultadoCierreVencido Ninguna { get; } = new(TipoCierreVencido.Ninguna, null, null, null);
    public static ResultadoCierreVencido Trivia(ResultadoAvancePregunta pregunta, ResultadoAvance? juegoFinalizado) =>
        new(TipoCierreVencido.Trivia, pregunta, null, juegoFinalizado);
    public static ResultadoCierreVencido Bdt(ResultadoAvanceEtapa etapa, ResultadoAvance? juegoFinalizado) =>
        new(TipoCierreVencido.Bdt, null, etapa, juegoFinalizado);

    public bool HuboCambio => Tipo != TipoCierreVencido.Ninguna;
}
