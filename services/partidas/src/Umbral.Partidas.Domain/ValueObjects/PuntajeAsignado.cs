namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct PuntajeAsignado
{
    public int Valor { get; }

    private PuntajeAsignado(int valor) => Valor = valor;

    public static PuntajeAsignado Crear(int valor)
    {
        if (valor <= 0)
            throw new ArgumentException("PuntajeAsignado debe ser positivo.", nameof(valor));

        return new PuntajeAsignado(valor);
    }

    public bool EsValido() => Valor > 0;
}
