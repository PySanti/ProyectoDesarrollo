using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed class ListarPartidasBdtPublicadasQueryHandler
    : IRequestHandler<ListarPartidasBdtPublicadasQuery, IReadOnlyList<PartidaBdtPublicadaItem>>
{
    private readonly IPartidaBdtReadRepository _repository;

    public ListarPartidasBdtPublicadasQueryHandler(IPartidaBdtReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PartidaBdtPublicadaItem>> Handle(
        ListarPartidasBdtPublicadasQuery request,
        CancellationToken cancellationToken)
    {
        if (!ModalidadFilterParser.TryParse(request.Modalidad, out var modalidad))
        {
            throw new ArgumentException("La modalidad debe ser Individual o Equipo.", nameof(request.Modalidad));
        }

        return await _repository.ListPublishedAsync(modalidad, cancellationToken);
    }
}
