using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Ports;

/// <summary>
/// Puerto de repositorio para el aggregate root TriviaForm.
/// La capa Infrastructure implementa esta interface mediante EF Core.
/// Los handlers de aplicación dependen de este puerto, no de detalles de persistencia.
/// </summary>
public interface ITriviaFormRepository
{
    /// <summary>
    /// Persiste un nuevo formulario de Trivia.
    /// </summary>
    /// <param name="form">Aggregate root TriviaForm a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task AddAsync(TriviaForm form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un formulario por su identificador, incluyendo todas sus preguntas y opciones.
    /// </summary>
    /// <param name="id">Identificador del formulario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El aggregate TriviaForm completo, o null si no existe.</returns>
    Task<TriviaForm?> GetByIdAsync(TriviaFormId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un formulario existente (título y/o preguntas).
    /// </summary>
    /// <param name="form">Aggregate root TriviaForm con los cambios aplicados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task UpdateAsync(TriviaForm form, CancellationToken cancellationToken = default);
}
