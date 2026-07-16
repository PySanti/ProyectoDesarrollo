namespace Umbral.IdentityService.Application.DTOs;

/// <summary>
/// Resultado de eliminar un equipo desde administración. No reporta el desenlace de la
/// notificación a los integrantes: el correo se envía de forma asíncrona (evento
/// <c>EquipoEliminado</c> vía RabbitMQ), fuera del request.
/// </summary>
public sealed record EliminarEquipoAdminResponse(
    Guid EquipoId,
    string NombreEquipo);
