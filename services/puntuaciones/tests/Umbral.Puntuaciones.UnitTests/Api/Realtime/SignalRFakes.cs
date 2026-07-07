using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public sealed class FakeGroupManager : IGroupManager
{
    public List<(string ConnectionId, string Group)> Added { get; } = new();
    public List<(string ConnectionId, string Group)> Removed { get; } = new();

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Removed.Add((connectionId, groupName));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubCallerContext : HubCallerContext
{
    public FakeHubCallerContext(string connectionId) => ConnectionId = connectionId;

    public override string ConnectionId { get; }
    public override string? UserIdentifier => null;
    public override System.Security.Claims.ClaimsPrincipal? User => null;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() { }
}
