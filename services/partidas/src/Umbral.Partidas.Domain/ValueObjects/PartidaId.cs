namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct PartidaId(Guid Valor)
{
    public static PartidaId New() => new(Guid.NewGuid());
    public static PartidaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
