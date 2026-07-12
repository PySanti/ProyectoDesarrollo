using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Api.Middleware;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Api;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, string body)> InvokeWith(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw ex,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    // ── 409 Conflict bucket ──────────────────────────────────────────────────

    [Fact]
    public async Task AlreadyBelongsToActiveTeamException_Returns_409()
    {
        var (status, _) = await InvokeWith(new AlreadyBelongsToActiveTeamException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task ConcurrentTeamCreationException_Returns_409()
    {
        var (status, _) = await InvokeWith(new ConcurrentTeamCreationException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task UsuarioYaEnEquipoException_Returns_409()
    {
        var (status, _) = await InvokeWith(new UsuarioYaEnEquipoException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task LeaveTeamConflictException_Returns_409()
    {
        var (status, _) = await InvokeWith(new LeaveTeamConflictException("conflict"));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task TransferirLiderazgoConflictException_Returns_409()
    {
        var (status, _) = await InvokeWith(new TransferirLiderazgoConflictException("conflict"));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task EquipoLlenoException_Returns_409()
    {
        var (status, _) = await InvokeWith(new EquipoLlenoException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task InvitacionPendienteYaExisteException_Returns_409()
    {
        var (status, _) = await InvokeWith(new InvitacionPendienteYaExisteException(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task DuplicateEmailException_Returns_409()
    {
        var (status, _) = await InvokeWith(new DuplicateEmailException("test@example.com"));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task EquipoConParticipacionActivaException_Returns_409()
    {
        var (status, _) = await InvokeWith(new EquipoConParticipacionActivaException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    // ── 404 Not Found bucket ─────────────────────────────────────────────────

    [Fact]
    public async Task NoActiveTeamForParticipantException_Returns_404()
    {
        var (status, _) = await InvokeWith(new NoActiveTeamForParticipantException(Guid.NewGuid()));
        Assert.Equal(404, status);
    }

    [Fact]
    public async Task InvitacionNoEncontradaException_Returns_404()
    {
        var (status, _) = await InvokeWith(new InvitacionNoEncontradaException(Guid.NewGuid()));
        Assert.Equal(404, status);
    }

    [Fact]
    public async Task UserNotFoundException_Returns_404()
    {
        var (status, _) = await InvokeWith(new UserNotFoundException(Guid.NewGuid()));
        Assert.Equal(404, status);
    }

    // ── 403 Forbidden bucket ─────────────────────────────────────────────────

    [Fact]
    public async Task NoEsLiderException_Returns_403()
    {
        var (status, _) = await InvokeWith(new NoEsLiderException(Guid.NewGuid()));
        Assert.Equal(403, status);
    }

    // ── 502 Bad Gateway bucket ───────────────────────────────────────────────

    [Fact]
    public async Task KeycloakIntegrationException_Returns_502()
    {
        var (status, _) = await InvokeWith(new KeycloakIntegrationException("keycloak down"));
        Assert.Equal(502, status);
    }

    [Fact]
    public async Task EmailDeliveryException_Returns_502()
    {
        var (status, _) = await InvokeWith(new EmailDeliveryException("mail failed"));
        Assert.Equal(502, status);
    }

    // ── 500 Internal Server Error bucket ────────────────────────────────────

    [Fact]
    public async Task PersistenceException_Returns_500()
    {
        var (status, _) = await InvokeWith(new PersistenceException("db error"));
        Assert.Equal(500, status);
    }

    // ── Unmapped → 500 ──────────────────────────────────────────────────────

    [Fact]
    public async Task Unmapped_Exception_Returns_500()
    {
        var (status, _) = await InvokeWith(new InvalidOperationException("boom"));
        Assert.Equal(500, status);
    }

    // ── Body shape ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Body_Has_Message_Field_With_Correct_Value()
    {
        const string expectedMessage = "db error from persistence";
        var (_, body) = await InvokeWith(new PersistenceException(expectedMessage));
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(expectedMessage, doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Body_Has_Message_Field_For_Guid_Ctor_Exception()
    {
        var userId = Guid.NewGuid();
        var (_, body) = await InvokeWith(new UserNotFoundException(userId));
        using var doc = JsonDocument.Parse(body);
        var message = doc.RootElement.GetProperty("message").GetString();
        Assert.NotNull(message);
        Assert.Contains(userId.ToString(), message);
    }

    [Fact]
    public async Task Response_ContentType_Is_ApplicationJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new PersistenceException("err"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("application/json", context.Response.ContentType);
    }
}
