using Umbral.BdtGameService.Application.Abstractions.Persistence;
using Umbral.BdtGameService.Application.Games.ListPublished;
using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.UnitTests;

public sealed class ListarPartidasBdtPublicadasHandlerTests
{
    [Fact]
    public async Task Handle_Should_Request_Published_Games_Without_Modality_Filter()
    {
        var repository = new FakePartidaBdtReadRepository(new[]
        {
            new PartidaBdtPublicadaItem(Guid.NewGuid(), "A", "Individual", "Lobby", "Area", 1),
            new PartidaBdtPublicadaItem(Guid.NewGuid(), "B", "Equipo", "Lobby", "Area", 2)
        });
        var handler = new ListarPartidasBdtPublicadasQueryHandler(repository);

        var result = await handler.Handle(new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Null(repository.LastRequestedModalidad);
        Assert.Equal(1, repository.CallCount);
    }

    [Theory]
    [InlineData("Individual", Modalidad.Individual)]
    [InlineData("Equipo", Modalidad.Equipo)]
    public async Task Handle_Should_Request_Published_Games_With_Modality_Filter(string rawModalidad, Modalidad expected)
    {
        var repository = new FakePartidaBdtReadRepository(Array.Empty<PartidaBdtPublicadaItem>());
        var handler = new ListarPartidasBdtPublicadasQueryHandler(repository);

        await handler.Handle(new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), rawModalidad), CancellationToken.None);

        Assert.Equal(expected, repository.LastRequestedModalidad);
    }

    [Fact]
    public async Task Handle_Should_Reject_Invalid_Modality()
    {
        var handler = new ListarPartidasBdtPublicadasQueryHandler(new FakePartidaBdtReadRepository(Array.Empty<PartidaBdtPublicadaItem>()));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new ListarPartidasBdtPublicadasQuery(Guid.NewGuid(), "Mixta"),
            CancellationToken.None));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Individual", true)]
    [InlineData("Equipo", true)]
    [InlineData("individual", false)]
    [InlineData("Mixta", false)]
    public void ModalidadFilterParser_Should_Validate_Contract_Values(string? value, bool expected)
    {
        Assert.Equal(expected, ModalidadFilterParser.IsValid(value));
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
