namespace Umbral.Partidas.Domain.ValueObjects;

public sealed record NombrePartida
{
    public const int LongitudMaxima = 120;

    public string Valor { get; }

    private NombrePartida(string valor) => Valor = valor;

    public static NombrePartida Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("NombrePartida es requerido.", nameof(valor));

        var trimmed = valor.Trim();
        if (trimmed.Length > LongitudMaxima)
            throw new ArgumentException($"NombrePartida no puede exceder {LongitudMaxima} caracteres.", nameof(valor));

        return new NombrePartida(trimmed);
    }

    public bool EsValido() => !string.IsNullOrWhiteSpace(Valor) && Valor.Length <= LongitudMaxima;
}
