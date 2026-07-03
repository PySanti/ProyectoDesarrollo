namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct SesionPartidaId(Guid Valor)
{
    public static SesionPartidaId New() => new(Guid.NewGuid());
    public static SesionPartidaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
