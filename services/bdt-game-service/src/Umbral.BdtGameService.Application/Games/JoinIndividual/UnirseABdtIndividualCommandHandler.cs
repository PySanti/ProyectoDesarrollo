using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;

namespace Umbral.BdtGameService.Application.Games.JoinIndividual;

public sealed class UnirseABdtIndividualCommandHandler : IRequestHandler<UnirseABdtIndividualCommand, UnirseABdtIndividualResponse>
{
    private readonly IPartidaBdtRepository _repository;

    public UnirseABdtIndividualCommandHandler(IPartidaBdtRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnirseABdtIndividualResponse> Handle(UnirseABdtIndividualCommand request, CancellationToken cancellationToken)
    {
        return await _repository.ExecuteWithPartidaRegistrationLockAsync(
            request.PartidaId,
            async lockedCancellationToken =>
            {
                var partida = await _repository.GetByIdWithExploradoresAsync(request.PartidaId, lockedCancellationToken);
                if (partida is null)
                {
                    throw new KeyNotFoundException("Partida BDT no encontrada.");
                }

                var explorador = partida.RegistrarParticipanteIndividual(request.ParticipanteUserId, DateTime.UtcNow);
                await _repository.UpdateAsync(partida, lockedCancellationToken);

                return new UnirseABdtIndividualResponse(
                    partida.PartidaId,
                    partida.Nombre,
                    partida.Modalidad.ToString(),
                    partida.Estado.ToString(),
                    explorador.ExploradorId,
                    request.ParticipanteUserId,
                    partida.ObtenerPosicionEnLobby(explorador.ExploradorId),
                    "Te uniste a la BDT. Espera el inicio de la partida.");
            },
            cancellationToken);
    }
}
