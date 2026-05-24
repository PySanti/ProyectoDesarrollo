**HISTORIAS DE USUARIO PARA PRIMERA ENTREGA**

## 

| ID | Módulo | Historia de usuario | Actor principal | Criterios de aceptación | Prioridad |
| ----- | ----- | ----- | ----- | ----- | ----- |
| **HU-01** | Acceso y usuarios | Como **Administrador**, quiero gestionar usuarios y roles, para controlar el acceso a las funcionalidades del sistema. | Administrador | El administrador puede crear, editar, consultar y desactivar usuarios. Cada usuario debe tener un rol asignado. El sistema restringe funcionalidades según el rol autenticado. | Alta |
| **HU-02** | Equipos | Como **Administrador**, quiero gestionar equipos, para mantener organizada la participación en las sesiones. | Administrador | El administrador puede  consultar y desactivar equipos. Los equipos inactivos no pueden asociarse a nuevas sesiones. La información histórica se conserva. | Alta |
| **HU-03** | Equipos | Como **Administrador**, quiero asociar participantes a equipos, para definir qué usuarios participarán dentro de cada equipo. | Administrador | El sistema permite asignar participantes a un equipo. Un participante solo puede acceder al equipo que le corresponda dentro de una sesión. La asociación queda registrada. | Alta |
| **HU-04** | Sesiones | Como **Operador**, quiero crear una sesión seleccionando el modo **Trivia** o **Búsqueda del Tesoro**, para ejecutar una dinámica válida del sistema. | Operador | Toda sesión debe estar asociada exactamente a un modo de juego. El sistema solo permite seleccionar Trivia o Búsqueda del Tesoro. No se pueden crear modos adicionales. | Alta |
| **HU-05** | Sesiones | Como **Operador**, quiero asociar equipos a una sesión, para preparar la participación antes de iniciar la dinámica. | Operador | El operador puede asociar equipos activos a una sesión. La sesión no puede iniciar si no tiene al menos un equipo asociado. | Alta |
| **HU-06** | Sesiones | Como **Operador**, quiero controlar el estado de una sesión, para gestionar correctamente su ciclo de vida. | Operador | La sesión puede manejar estados como programada, en preparación, activa, pausada, finalizada y cancelada. El sistema rechaza transiciones inválidas e informa el motivo. | Alta |
| **HU-07** | Participación | Como **Participante**, quiero acceder a la sesión asignada a mi equipo, para participar únicamente en el contexto que me corresponde. | Participante | El sistema valida la identidad del participante y su pertenencia al equipo. No se permite acceder a sesiones o equipos no asignados. | Alta |

| HU-11 | Auditoría | Como Operador, quiero consultar el historial de eventos de una sesión, para auditar lo ocurrido durante la operación. | Operador | El historial incluye cambios de estado, respuestas, evidencias, pistas, penalizaciones y variaciones de puntaje. Las sesiones finalizadas siguen siendo consultables. | Alta |
| :---- | :---- | :---- | :---- | :---- | :---- |

# 

| HU-13 | Búsqueda del Tesoro | Como Administrador, quiero gestionar misiones de Búsqueda del Tesoro, para preparar el contenido base de ese modo de juego. | Administrador | El administrador puede crear, editar, consultar, activar, desactivar y archivar misiones. Solo misiones activas y válidas pueden usarse en sesiones. | Alta |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **HU-14** | Búsqueda del Tesoro | Como **Administrador**, quiero estructurar una misión en etapas, nodos, objetivos y pistas, para definir el recorrido de la Búsqueda del Tesoro. | Administrador | La misión permite registrar una estructura válida. Las pistas quedan asociadas a etapas u objetivos. El sistema valida que la estructura sea coherente. | Alta |

## 

| HU-21 | Trivia | Como Administrador, quiero gestionar preguntas de Trivia con opciones, respuesta correcta, puntaje y tiempo límite, para configurar cada ronda de juego. | Administrador | Cada pregunta tiene opciones de respuesta, una respuesta correcta, puntaje asignado y temporizador. El sistema valida que la pregunta esté completa antes de publicarla. | Alta |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **HU-22** | Trivia | Como **Operador**, quiero crear una sesión de **Trivia** desde un quiz publicado, para ejecutar una competencia válida. | Operador | El sistema solo permite seleccionar quizzes publicados. La sesión queda vinculada a un único quiz fuente. Se rechazan quizzes en borrador, archivados o incompletos. | Alta |
| **HU-25** | Trivia | Como **Participante**, quiero conocer el resultado de cada pregunta cerrada, para saber si mi equipo respondió correctamente. | Participante | Al cerrar la pregunta, el sistema muestra si la respuesta fue correcta o incorrecta. El puntaje y ranking se actualizan según el resultado. | Media |

