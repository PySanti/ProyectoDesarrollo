### **1\. Actores, Conceptos/Objetos y Acciones (Negocio)**

* **Actores:**  
  * `Administrador`, `Operador`, `Participante` (como actor genérico que interactúa, ya sea de forma individual o como `Líder` / `Miembro` de un equipo).  
* **Conceptos / Objetos Core:**  
  * `Usuario`, `Equipo`, `Código de Acceso`, `Formulario`, `Pregunta`, `Opción`, `Partida`, `Etapa BDT`, `Tesoro (QR)`, `Ubicación Geográfica`, `Pista`, **`Puntaje Acumulado`**, **`Tiempo de Respuesta Acumulado`**, `Ranking Final`, `Registro de Auditoría`.  
* **Acciones (Comandos):**  
  * `RegistrarUsuario`, `CrearEquipo`, `UnirseAEquipo`, `AbandonarEquipo`, `CrearFormulario`, `InstanciarPartida`, `SolicitarInscripcionEquipo`, `IniciarPartida`, **`RegistrarRespuestaDefinitiva`**, **`ValidarEnvioQR`**, `DespacharPista`, `CancelarPartida`.

### **2\. Subdominios (Espacio del Problema)**

* **Core (Diferenciadores):**  
  * **Subdominio de Trivia:** Evaluación síncrona de respuestas correctas versus incorrectas, gestión de temporizadores y acumulación progresiva de puntajes por precisión.  
  * **Subdominio de Búsqueda del Tesoro (BDT):** Geolocalización y validación de hitos (QR) con asignación de puntajes por descubrimiento de etapas.  
* **Soporte:**  
  * **Gestión de Equipos:** Agrupación social de participantes, validación de membresías y límites.  
  * **Registro de Auditoría:** Historial inmutable para revisión posterior de la partida.  
* **Genérico:**  
  * **IAM (Identidad y Accesos):** Cuentas de usuario y roles base.

### **3\. Contextos Acotados (Espacio de la Solución)**

Aquí aplicamos tu decisión: el nombre `Participante` se mantiene en los módulos que lo necesitan, pero representando cosas totalmente distintas mediante Namespaces separados.

1. `Contexto de Identidad (Identity Context)`  
2. `Contexto de Equipos (Team Context)`  
3. `Contexto de Trivia (Trivia Context)`  
4. `Contexto de Búsqueda del Tesoro (BDT Context)`  
5. `Contexto de Auditoría (Auditing Context)`

### **4\. Agregados (Modelado Táctico del Dominio)**

Aquí es donde el **Puntaje Acumulado** y la separación de la clase **`Participante`** toman su lugar definitivo en las reglas del negocio:

#### **A. Contexto de Equipos (`Umbral.Equipos.Domain`)**

* **Agregado Raíz:** `Equipo`  
  * **Entidades Hijas:** `Participante` (Aquí el participante solo tiene: `Id`, `FechaUnion`, y un booleano `EsLider`).  
  * **Value Objects:** `EquipoId`, `NombreEquipo`, `CodigoAcceso`.  
  * **Invariantes:**  
    * La colección de `Participantes` dentro del equipo debe ser siempre ≥2 y ≤5.

#### **B. Contexto de Trivia (`Umbral.Trivias.Domain`)**

* **Agregado Raíz 1:** `Formulario` (La plantilla de preguntas)  
  * **Entidades Hijas:** `Pregunta`.  
  * **Value Objects:** `FormularioId`, `Opcion` (Texto, `EsCorrecta`), `PuntajeAsignado` (Puntos específicos que vale esa pregunta).  
* **Agregado Raíz 2:** `PartidaTrivia` (Maneja el juego activo)  
  * **Entidades Hijas:** `Participante` (Aquí la clase participante representa a un **Competidor Activo**. Sus propiedades lógicas son: `Id`, **`PuntajeAcumulado`**, **`TiempoRespuestaAcumulado`**).  
  * **Value Objects:** `PartidaId`, `EstadoPartida`, `Modalidad` (Individual/Equipos).  
  * **Reglas e Invariantes de Puntaje:**  
    * *Mapeo de Identidad:* Si la modalidad es *Individual*, el `Id` del `Participante` en este contexto coincide con su `UsuarioId`. Si la modalidad es por *Equipos*, el `Id` del `Participante` en este contexto coincide con el `EquipoId`. El motor de Trivia no necesita saber si es un humano o un grupo; para él es una entidad que acumula puntos.  
    * *Regla de Acumulación:* Al procesarse una respuesta correcta, el método de dominio `PartidaTrivia.AcumularPuntaje(participanteId, preguntaId)` extrae el `PuntajeAsignado` del formulario y lo **suma directamente** al atributo `PuntajeAcumulado` de ese `Participante`.

#### **C. Contexto de Búsqueda del Tesoro (`Umbral.Bdt.Domain`)**

* **Agregado Raíz:** `PartidaBDT`  
  * **Entidades Hijas:** \* `EtapaBDT` (Fases del juego. Contiene un `PuntajeEtapa`).  
    * `Participante` (Aquí la clase participante representa al explorador en movimiento. Propiedades: `Id`, **`PuntajeAcumulado`**, `UbicacionActual`).  
  * **Value Objects:** `PartidaId`, `EstadoPartida`, `UbicacionSecreta`, `CodigoQREsperado`.  
  * **Reglas e Invariantes de Puntaje:**  
    * *Mapeo de Identidad:* Aplica la misma abstracción; el `Id` del `Participante` puede ser un `UsuarioId` o un `EquipoId` según la modalidad.  
    * *Regla de Acumulación:* Cuando el método de dominio `PartidaBDT.ValidarHito(participanteId, qrDecodificado)` confirma que el código es correcto, el estado de la `EtapaBDT` activa pasa a *Resuelta*, se le otorga el `PuntajeEtapa` sumándolo inmediatamente al `PuntajeAcumulado` de ese `Participante` específico, y se avanza el índice a la siguiente etapa.

#### 

#### 

#### **D. Contexto de Auditoría y Contexto de Identidad**

*(Se mantienen idénticos, ya que son contextos de soporte y genéricos que no manejan las mecánicas dinámicas de puntuación del juego).*

### **5\. Eventos de Dominio (Domain Events)**

Los eventos ahora notifican explícitamente los impactos en el tablero de puntuación:

* `RespuestaTriviaValidada` (Lleva el `ParticipanteId`, si fue correcta o incorrecta, y el tiempo empleado).  
* **`PuntajeTriviaIncrementado`** (Disparado cuando el participante muta su estado interno sumando puntos).  
* `HitoBDTEncontrado` (Lleva el `ParticipanteId` que escaneó exitosamente el QR).  
* **`PuntajeBDTIncrementado`** (Notifica que un competidor sumó los puntos de la etapa).  
* `PartidaTriviaFinalizada` / `PartidaBDTFinalizada` (Lleva el estado final de todos los participantes con sus puntajes consolidados).

### **6\. Servicios de Dominio (Domain Services)**

* **`ClasificadorRankingService`:** Este servicio vive de forma independiente en los contextos de Trivia y BDT. No almacena datos. Su única función es recibir la colección de objetos `Participante` del agregado de la partida al finalizar el juego, leer sus atributos `PuntajeAcumulado` (y `TiempoRespuestaAcumulado` en el caso de Trivia), aplicar el algoritmo de ordenamiento descendente y resolver empates matemáticos para entregar la estructura del Podio Final.

### **7\. Servicios de Aplicación (Application Services)**

Mapean los casos de uso coordinando los repositorios para guardar los puntajes modificados en la base de datos:

* **`ProcesarRespuestaTriviaCommandHandler`:**  
  1. Recibe el comando desde la API con el `ParticipanteId`, `PreguntaId` y la opción elegida.  
  2. Carga el agregado `PartidaTrivia` desde el repositorio.  
  3. Invoca la lógica del agregado para validar la respuesta y mutar el `PuntajeAcumulado` del `Participante`.  
  4. Guarda los cambios de la partida y sus participantes en la base de datos.  
  5. Publica el evento de dominio `PuntajeTriviaIncrementado` para que la capa de infraestructura (SignalR) actualice el ranking en tiempo real en los clientes.  
* **`ProcesarEscaneoQRCommandHandler`:**  
  1. Recibe el string del QR y el `ParticipanteId`.  
  2. Carga el agregado `PartidaBDT`.  
  3. Invoca la lógica de validación. Si es correcto, el dominio actualiza el `PuntajeAcumulado` del participante y cierra la etapa.  
  4. Persiste los cambios en el repositorio.  
  5. Dispara el evento `PuntajeBDTIncrementado` para refrescar los tableros globales.

* 

