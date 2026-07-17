using Microsoft.AspNetCore.Authorization;
using Umbral.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);

// Enciende el reverse proxy (YARP) y le carga el mapa de rutas desde appsettings.json:
// que direccion de entrada va a que microservicio. El gateway solo reenvia, no tiene logica propia.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Prepara la revision del token de Keycloak: comprueba que sea autentico (firma, quien lo emitio,
// para quien es, que no este vencido). La configuracion sale de variables de entorno.
builder.Services.AddKeycloakJwtAuth(builder.Configuration, builder.Environment);

// Define las "reglas de acceso" por rol. Cada regla exige que el token traiga cierto rol.
// Los tres primeros son los roles de la persona; los dos ultimos son permisos (tambien viajan
// como rol dentro del token). El fallback es la red de seguridad: cualquier ruta que se olvide
// de poner una regla igual exige, como minimo, estar autenticado. Nunca queda abierta por error.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Permite que la web (que corre en otra direccion, :5173) pueda llamar al gateway desde el navegador.
// Sin esto el navegador bloquearia las llamadas por seguridad. Las direcciones permitidas vienen de
// una variable de entorno. AllowCredentials hace falta para la conexion en tiempo real (SignalR).
var corsOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

// Orden del proceso para cada peticion que llega: primero CORS, luego revisar quien es (token),
// luego revisar si tiene permiso. El orden importa: no se autoriza a nadie sin antes identificarlo.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Chequeo de "sigo vivo" del propio gateway. Es publico (sin token) y no toca ningun microservicio.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }))
   .AllowAnonymous();

// Ultimo paso: reenviar la peticion al microservicio que le toca. Solo se llega aca si paso las
// revisiones de arriba. Las rutas sin regla propia caen en el fallback (hay que estar autenticado).
app.MapReverseProxy();

app.Run();

public partial class Program
{
}
