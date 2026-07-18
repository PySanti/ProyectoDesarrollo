namespace Umbral.OperacionesSesion.Domain.Enums;
// TodosRespondieron: todos los elegibles respondieron y ninguno acertó → cierre anticipado
// (revela la correcta y avanza) sin esperar el timeout.
public enum MotivoCierrePregunta { RespuestaCorrecta, AvanceOperador, Tiempo, TodosRespondieron }
