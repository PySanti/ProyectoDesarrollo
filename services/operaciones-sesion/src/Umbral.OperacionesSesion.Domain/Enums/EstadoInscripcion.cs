namespace Umbral.OperacionesSesion.Domain.Enums;

// Valores explícitos: EF persiste como int. Activa/Cancelada mantienen sus valores
// históricos (0/1) para no corromper filas existentes; los nuevos se anexan.
public enum EstadoInscripcion
{
    Activa = 0,
    Cancelada = 1,
    Pendiente = 2,
    Rechazada = 3
}
