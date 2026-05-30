### **Los 5 Microservicios de Negocio**

#### **1\. Microservicio de Identidad y Accesos (`Identity Service`)**

* **Contexto DDD:** *Identity Context* (Genérico).  
* **Responsabilidad:** Registro, autenticación, generación de tokens JWT y control de roles (Administrador, Operador, Participante).  
* **Historias de Usuario que cubre:** `HU-01` y `HU-02` (Gestión de usuarios y asignación de roles iniciales).  
* **Base de Datos:** Tabla única de usuarios y credenciales.

#### **2\. Microservicio de Gestión de Equipos (`Team Service`)**

* **Contexto DDD:** *Team Context* (Soporte).  
* **Responsabilidad:** Controlar la creación de grupos, validación de códigos de invitación, límites de miembros y cambios de roles internos (Líder/Invitado).  
* **Historias de Usuario que cubre:** `HU-03`, `HU-04`, `HU-05`, `HU-06` y `HU-07`.  
* **Base de Datos:** Almacena la estructura social del juego (`Equipos` y `Miembros`). No sabe nada de juegos ni de puntajes.

#### **3\. Microservicio Motor de Trivia (`Trivia Game Service`)**

* **Contexto DDD:** *Trivia Context* (Core).  
* **Responsabilidad:** Es el cerebro síncrono de las preguntas. Gestiona el ciclo de vida de la trivia, los temporizadores de las rondas, procesa las respuestas (individuales o la primera del equipo) y acumula los puntajes en memoria o base de datos.  
* **Historias de Usuario que cubre:** `HU-11` (Filtros), `HU-13` (Advertencias), `HU-15`, `HU-17`, `HU-18`, `HU-19`, `HU-21`, `HU-22`, `HU-23`, `HU-24`, `HU-26`, `HU-27`, `HU-28`, `HU-29`, `HU-30` y `HU-35`.  
* **Base de Datos:** Almacena los formularios (plantillas) y el estado transitorio de las partidas activas y sus resultados.

#### **4\. Microservicio de Búsqueda del Tesoro (`BDT Game Service`)**

* **Contexto DDD:** *BDT Context* (Core).  
* **Responsabilidad:** Gestiona las etapas de la búsqueda. Recibe las strings decodificadas de los QR que envían los celulares, las valida textualmente contra el hito esperado y avanza de fase. También maneja el envío de pistas de texto del operador.  
* **Historias de Usuario que cubre:** `HU-12` (Filtros), `HU-14` (Advertencias), `HU-34`, `HU-37`, `HU-39`, `HU-40`, `HU-42`, `HU-43`, `HU-44`, `HU-45`, `HU-46`, `HU-47` y `HU-49`.  
* **Base de Datos:** Almacena la configuración de las rutas, hitos físicos esperados y el progreso del jugador/equipo activo.