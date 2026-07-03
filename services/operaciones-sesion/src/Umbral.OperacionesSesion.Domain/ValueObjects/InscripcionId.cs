namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct InscripcionId(Guid Valor)
{
    public static InscripcionId New() => new(Guid.NewGuid());
    public static InscripcionId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
