using System;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
        => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}
