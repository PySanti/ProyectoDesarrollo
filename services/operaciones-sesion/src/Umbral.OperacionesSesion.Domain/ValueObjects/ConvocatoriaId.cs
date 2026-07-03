namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct ConvocatoriaId(Guid Valor)
{
    public static ConvocatoriaId New() => new(Guid.NewGuid());
    public static ConvocatoriaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
