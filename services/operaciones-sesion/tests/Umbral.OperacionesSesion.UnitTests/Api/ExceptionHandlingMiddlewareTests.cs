using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Middleware;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<int> StatusFor(Exception ex)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    [Fact]
    public async Task Maps_config_not_found_to_404()
        => Assert.Equal((int)HttpStatusCode.NotFound, await StatusFor(new PartidaConfigNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_session_not_found_to_404()
        => Assert.Equal((int)HttpStatusCode.NotFound, await StatusFor(new SesionNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_config_inaccesible_to_502()
        => Assert.Equal((int)HttpStatusCode.BadGateway, await StatusFor(new PartidasConfigInaccesibleException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_already_published_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new SesionYaPublicadaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_not_publishable_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new PartidaNoPublicableException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_cupo_lleno_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new CupoLlenoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_participante_no_identificado_to_401()
        => Assert.Equal((int)HttpStatusCode.Unauthorized, await StatusFor(new ParticipanteNoIdentificadoException()));

    [Fact]
    public async Task Maps_validation_exception_to_400()
        => Assert.Equal(400, await StatusFor(new ValidationException("x")));

    [Fact]
    public async Task Maps_unknown_exception_to_500()
        => Assert.Equal(500, await StatusFor(new InvalidOperationException("boom")));

    [Fact]
    public async Task Maps_modo_inicio_no_compatible_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new ModoInicioNoCompatibleException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_sesion_no_iniciada_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new SesionNoIniciadaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_participante_no_inscrito_to_403()
        => Assert.Equal((int)HttpStatusCode.Forbidden, await StatusFor(new ParticipanteNoInscritoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_juego_activo_no_trivia_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new JuegoActivoNoEsTriviaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_no_hay_pregunta_activa_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new NoHayPreguntaActivaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_respuesta_duplicada_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new RespuestaDuplicadaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_pregunta_fuera_de_tiempo_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new PreguntaFueraDeTiempoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_juego_con_preguntas_pendientes_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new JuegoConPreguntasPendientesException(Guid.NewGuid())));
}
