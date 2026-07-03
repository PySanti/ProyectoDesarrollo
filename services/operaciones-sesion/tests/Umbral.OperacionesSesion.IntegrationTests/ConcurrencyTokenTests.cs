using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class ConcurrencyTokenTests
{
    // Construir el modelo con Npgsql NO abre conexión: solo se inspecciona ctx.Model.
    private static OperacionesSesionDbContext NpgsqlCtx() =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseNpgsql("Host=localhost;Database=x;Username=x;Password=x").Options);

    [Fact]
    public void SesionPartida_tiene_token_de_concurrencia_en_npgsql()
    {
        using var ctx = NpgsqlCtx();
        var et = ctx.Model.FindEntityType(typeof(SesionPartida))!;
        Assert.Contains(et.GetProperties(), p => p.IsConcurrencyToken);
    }
}
