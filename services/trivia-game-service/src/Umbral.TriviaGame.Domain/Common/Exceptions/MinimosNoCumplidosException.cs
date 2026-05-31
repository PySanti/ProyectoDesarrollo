namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class MinimosNoCumplidosException : DomainValidationException
{
    public int Inscriptos { get; }
    public int MinimoRequerido { get; }

    public MinimosNoCumplidosException(int inscriptos, int minimoRequerido)
        : base($"No se puede iniciar la partida: hay {inscriptos} participantes inscritos, " +
               $"pero se requiere un mínimo de {minimoRequerido}.")
    {
        Inscriptos = inscriptos;
        MinimoRequerido = minimoRequerido;
    }
}
