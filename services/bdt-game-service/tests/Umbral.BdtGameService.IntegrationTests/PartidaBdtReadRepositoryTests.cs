using Microsoft.EntityFrameworkCore;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class PartidaBdtReadRepositoryTests
{
    [Fact]
    public async Task ListPublishedAsync_Should_Project_Lobby_Games_Only()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Partidas.AddRangeAsync(
            PartidaBDT.CrearPublicada("Lobby", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()),
            PartidaBDT.CrearNoPublicada("Terminada", Modalidad.Equipo, new AreaBusqueda("Area"), OneStage(), EstadoPartida.Terminada));
        await dbContext.SaveChangesAsync();
        var repository = new PartidaBdtReadRepository(dbContext);

        var result = await repository.ListPublishedAsync(null, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Lobby", item.Nombre);
        Assert.Equal("Lobby", item.Estado);
        Assert.Equal(1, item.CantidadEtapas);
    }

    [Fact]
    public async Task ListPublishedAsync_Should_Apply_Optional_Modality_Filter()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Partidas.AddRangeAsync(
            PartidaBDT.CrearPublicada("Individual", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo", Modalidad.Equipo, new AreaBusqueda("Area"), OneStage()));
        await dbContext.SaveChangesAsync();
        var repository = new PartidaBdtReadRepository(dbContext);

        var result = await repository.ListPublishedAsync(Modalidad.Equipo, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Equipo", item.Modalidad);
    }

    [Fact]
    public async Task ListPublishedAsync_Should_Return_Both_Modalities_When_Filter_Is_Omitted()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Partidas.AddRangeAsync(
            PartidaBDT.CrearPublicada("Individual", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo", Modalidad.Equipo, new AreaBusqueda("Area"), OneStage()));
        await dbContext.SaveChangesAsync();
        var repository = new PartidaBdtReadRepository(dbContext);

        var result = await repository.ListPublishedAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Modalidad == "Individual");
        Assert.Contains(result, item => item.Modalidad == "Equipo");
    }

    [Fact]
    public async Task ListPublishedAsync_Should_Not_Mutate_Persisted_Partidas_When_Filtering()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Partidas.AddRangeAsync(
            PartidaBDT.CrearPublicada("Individual", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo", Modalidad.Equipo, new AreaBusqueda("Area"), OneStage()));
        await dbContext.SaveChangesAsync();
        var before = await dbContext.Partidas.CountAsync();
        var repository = new PartidaBdtReadRepository(dbContext);

        await repository.ListPublishedAsync(Modalidad.Equipo, CancellationToken.None);

        var after = await dbContext.Partidas.CountAsync();
        Assert.Equal(before, after);
    }

    private static BdtDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BdtDbContext>()
            .UseInMemoryDatabase($"bdt-repository-tests-{Guid.NewGuid():N}")
            .Options;

        return new BdtDbContext(options);
    }

    private static EtapaBDT[] OneStage() => new[] { EtapaBDT.Crear(1, "QR-1", 60) };
}
