using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public class FakeEquipoDirectoryClientTests
{
    [Fact]
    public async Task Devuelve_el_equipo_configurado()
    {
        var equipoId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var fake = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "Halcones",
                new List<MiembroEquipoDto> { new(lider, true) })
        };

        var r = await fake.ObtenerMiEquipoAsync("Bearer x", CancellationToken.None);

        Assert.NotNull(r);
        Assert.Equal(equipoId, r!.EquipoId);
        Assert.True(r.Miembros[0].EsLider);
    }

    [Fact]
    public async Task Sin_configurar_devuelve_null()
    {
        var fake = new FakeEquipoDirectoryClient();
        Assert.Null(await fake.ObtenerMiEquipoAsync(null, CancellationToken.None));
    }
}
