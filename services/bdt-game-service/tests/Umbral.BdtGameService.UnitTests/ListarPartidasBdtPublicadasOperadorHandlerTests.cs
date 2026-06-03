using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.UnitTests;

public sealed class ListarPartidasBdtPublicadasOperadorHandlerTests
{
    [Fact]
    public async Task Handle_Should_Request_Published_Games_Without_Modality_Filter()
    {
        var repository = new FakePartidaBdtReadRepository(new[]
        {
            new PartidaBdtPublicadaItem(Guid.NewGuid(), "A", "Individual", "Lobby", "Area", 1),
            new PartidaBdtPublicadaItem(Guid.NewGuid(), "B", "Equipo", "Lobby", "Area", 2)
        });
        var handler = new ListarPartidasBdtPublicadasOperadorQueryHandler(repository);

        var result = await handler.Handle(new ListarPartidasBdtPublicadasOperadorQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Null(repository.LastRequestedModalidad);
        Assert.Equal(1, repository.CallCount);
    }

    [Fact]
    public void Validator_Should_Reject_Empty_Actor_User_Id()
    {
        var validator = new ListarPartidasBdtPublicadasOperadorQueryValidator();

        var result = validator.Validate(new ListarPartidasBdtPublicadasOperadorQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    private sealed class FakePartidaBdtReadRepository : IPartidaBdtReadRepository
    {
        private readonly IReadOnlyList<PartidaBdtPublicadaItem> _items;

        public FakePartidaBdtReadRepository(IReadOnlyList<PartidaBdtPublicadaItem> items)
        {
            _items = items;
        }

        public Modalidad? LastRequestedModalidad { get; private set; }
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<PartidaBdtPublicadaItem>> ListPublishedAsync(Modalidad? modalidad, CancellationToken cancellationToken)
        {
            LastRequestedModalidad = modalidad;
            CallCount++;
            return Task.FromResult(_items);
        }
    }
}
