using MediatR;
using Umbral.BdtGameService.Application.Abstractions.Persistence;

namespace Umbral.BdtGameService.Application.Games.ListPublished;

public sealed class ListarPartidasBdtPublicadasOperadorQueryHandler
    : IRequestHandler<ListarPartidasBdtPublicadasOperadorQuery, IReadOnlyList<PartidaBdtPublicadaItem>>
{
    private readonly IPartidaBdtReadRepository _repository;

    public ListarPartidasBdtPublicadasOperadorQueryHandler(IPartidaBdtReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PartidaBdtPublicadaItem>> Handle(
        ListarPartidasBdtPublicadasOperadorQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.ListPublishedAsync(null, cancellationToken);
    }
}
