namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct JuegoId(Guid Valor)
{
    public static JuegoId New() => new(Guid.NewGuid());
    public static JuegoId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
