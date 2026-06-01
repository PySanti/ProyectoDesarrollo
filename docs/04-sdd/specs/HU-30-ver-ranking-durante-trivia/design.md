# HU-30 — Design

## Contexto acotado

Trivia Context — Trivia Game Service.

## Agregado raíz

`PartidaTrivia` — provee métodos `ObtenerPuntajeAcumulado()`, `ObtenerTiempoRespuestaAcumulado()` y colección `Respuestas`.

`TriviaInscripcion` — lista de participantes registrados en la partida.

## Diseño

### RankingEntryDto

```csharp
public sealed record RankingEntryDto(
    string UsuarioId,
    string NombreUsuario,
    int PuntajeAcumulado,
    int TiempoAcumuladoSegundos,
    int RespuestasCorrectas,
    int TotalRespuestas,
    int Posicion
);
```

### GetRankingQuery

```csharp
public sealed record GetRankingQuery(Guid PartidaId) : IRequest<IReadOnlyList<RankingEntryDto>>;
```

### GetRankingQueryHandler

1. Carga `PartidaTrivia` con respuestas (via `GetByIdWithRespuestasAsync`)
2. Carga todas las `TriviaInscripcion` de la partida
3. Para cada inscripción, calcula puntaje y tiempo
4. Ordena por puntaje DESC, luego tiempo ASC
5. Asigna posición

### Ranking calculation (domain)

El ordenamiento se realiza en el handler de aplicación (es una consulta, no modifica estado). No se agrega lógica de dominio nueva; se reusan `ObtenerPuntajeAcumulado()` y `ObtenerTiempoRespuestaAcumulado()` existentes.

### SignalR

Se crea:

- `TriviaRankingHub` — hub vacío para grupos por partida
- `ITriviaRankingNotifier` — puerto en Application.Ports
- `TriviaRankingNotifier` — implementación que usa `IHubContext<TriviaRankingHub>`
- Se inyecta `ITriviaRankingNotifier` en `AnswerTriviaQuestionCommandHandler` y se llama tras procesar la respuesta

### Eventos

| Evento | Tipo | Trigger |
|---|---|---|
| `RankingUpdated` | SignalR event | Después de `RegistrarRespuestaDefinitiva` + `UpdateAsync` |

### Flujo

```
POST /answer
  → Handler procesa respuesta
  → UpdateAsync(partida)
  → _rankingNotifier.NotifyRankingUpdated(partidaId)
  → Hub emite "RankingUpdated" a group "game-{partidaId}"
  → Frontend recibe evento y llama GET /ranking
```

## Patrones de diseño

| Patrón | Ubicación | Problema resuelto |
|---|---|---|
| CQRS | GetRankingQuery + Handler | Separar consulta de escritura |
| Adapter | ITriviaRankingNotifier / TriviaRankingNotifier | Desacoplar SignalR de aplicación |
| Observer / PubSub | SignalR hub | Notificar cambios en tiempo real |
