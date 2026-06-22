# Evidencia de Runtime Mobile - HU-04

## Fecha y Hora
2026-06-01 15:30:00

## Dispositivo
- Emulador: Android API 30
- Versión de la app: 1.0.0

## Escenario A: Código válido (200)
- Acción: Ingresar código válido y enviar
- Resultado: 
  - Pantalla muestra "Te uniste al equipo con exito."
  - Equipo aparece en la lista de equipos del usuario
- Logs:
  ```
  [INFO]  Submitting join team request with code: ABCD1234
  [INFO]  Join team request successful
  [INFO]  Team joined successfully: Equipo Exploradores
  ```

## Escenario B: Código inválido (404)
- Acción: Ingresar código inexistente y enviar
- Resultado: 
  - Pantalla muestra "El codigo ingresado no corresponde a un equipo activo."
- Logs:
  ```
  [INFO]  Submitting join team request with code: XYZ9999
  [ERROR] Join team request failed: Not Found
  [INFO]  Displaying error message: El codigo ingresado no corresponde a un equipo activo.
  ```

## Escenario C: Usuario ya en equipo (409)
- Acción: Intentar unirse a un equipo cuando ya pertenece a otro
- Resultado: 
  - Pantalla muestra "No puedes unirte a este equipo."
- Logs:
  ```
  [INFO]  Submitting join team request with code: ABCD1234
  [ERROR] Join team request failed: Conflict
  [INFO]  Displaying error message: No puedes unirte a este equipo.
  ```

## Notas adicionales
- Todos los escenarios fueron ejecutados en un solo intento cada uno.
- No se observaron errores inesperados durante la ejecución.
- La conexión a la API fue estable durante toda la prueba.