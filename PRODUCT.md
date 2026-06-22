# Product

## Register

product

## Users

UMBRAL tiene **dos superficies de producto, una sola marca**: una consola web para quien administra y opera, y una app móvil para quien juega. El cliente se decide por rol (regla de routing de AGENTS.md), nunca se mezcla.

### A. Consola web — Administrador y Operador (administrar y operar)

- **Administrador**: gestiona usuarios y su rol inicial (crear, consultar, editar datos generales, desactivar). Trabaja sobre Identity.
- **Operador**: crea formularios y partidas de Trivia y de Búsqueda del Tesoro (BDT), las publica, las inicia, supervisa lobbies, sigue rankings, revisa tesoros/validación de QR y envía pistas. Sus flujos pasan por el gateway YARP y se materializan en Partidas (configuración), Operaciones de Sesion (runtime) y Puntuaciones (rankings/historial).

**Contexto (escena física):** laptop/escritorio, en dos momentos por igual:
1. **Antes del evento (setup):** configura con calma usuarios, formularios y partidas; manda la precisión y cero ambigüedad.
2. **En vivo durante la partida:** monitorea estado en tiempo real (quién entró al lobby, si inició, ranking actual, QR validado/no legible) y reacciona rápido; a veces se proyecta a la sala.

Ninguno de los dos modos puede sacrificar al otro. **Registro emocional:** "en control".

### B. App móvil — Participante y Líder de equipo (jugar)

- **Participante**: lista y filtra partidas publicadas, gestiona equipo (crear, responder una InvitacionEquipo, salir), se une a partidas individuales, responde Trivia, ve resultados y ranking, ve la etapa activa de BDT con su tiempo, sube la foto del tesoro QR, recibe pistas y comparte geolocalización cuando BDT lo exige.
- **Líder de equipo** (actuando como participante): además preinscribe al equipo y acepta/rechaza convocatorias.
- Trabaja siempre a través del gateway YARP, consumiendo capacidades de Identity, Partidas, Operaciones de Sesion y Puntuaciones según el contrato documentado. Cualquier carpeta de código con nombres antiguos de servicios es deuda de migración, no frontera activa.

**Contexto (escena física):** teléfono en una mano, dos escenas **por igual**:
1. **Trivia, sentado en interior:** responde preguntas bajo una cuenta regresiva, ambiente controlado.
2. **BDT, en movimiento y a veces a pleno sol:** camina por el campus buscando códigos QR contra el reloj, mirando el teléfono de reojo, con prisa.

**Registro emocional:** **competitivo, con urgencia** (tensión de reloj, ganas de subir en el ranking, adrenalina de la búsqueda) — distinto del "en control" del operador.

## Product Purpose

UMBRAL opera experiencias interactivas en exactamente dos modos de juego: **Trivia** y **Búsqueda del Tesoro (BDT)**. El producto vive en dos superficies que comparten marca y backend:

- **Consola web**: el administrador prepara el acceso (usuarios y roles) y el operador da vida a las partidas: las crea, publica, inicia y supervisa mientras ocurren.
- **App móvil**: el participante juega — descubre partidas, arma o se une a un equipo, responde Trivia, busca y sube tesoros QR en BDT, recibe pistas y sigue su posición.

El éxito en la web se ve cuando un operador lleva una partida en vivo sin fricción y sin equivocarse: lee el estado de un vistazo y actúa en el momento justo. El éxito en móvil se ve cuando un participante sabe en todo momento qué hacer ahora, cuánto tiempo le queda y dónde tocar, incluso caminando y a pleno sol. Ambas superficies **reflejan y comandan** el juego; la verdad del juego vive en el backend.

## Brand Personality

Tres palabras: **vivo, nítido, en control**.

"Umbral" es el punto desde el que se cruza hacia la experiencia: la consola se siente como el puesto de mando desde donde alguien enciende y conduce algo que está pasando ahora. Energética porque detrás hay un juego (el estado se mueve, el lobby se llena, el ranking cambia), pero esa energía es de grado operador: nace de mostrar el estado real con claridad y reacción, no de adornos.

Voz: directa y concreta, en español. Etiquetas verbo + objeto ("Iniciar partida", "Enviar pista", "Crear formulario"), nunca "OK"/"Aceptar" sueltos. Sin jerga de marketing. Mensajes de error que dicen qué pasó y qué hacer.

Meta emocional: el operador se siente al mando de algo que está vivo, sin sentirse abrumado.

**Una marca, dos inclinaciones por superficie.** La personalidad es una, pero cada superficie acentúa una palabra: el operador siente sobre todo "en control"; el participante siente sobre todo "vivo". En móvil la energía de juego puede subir **un punto** respecto a la consola (más color y feedback vivo en aciertos, tesoro encontrado, cambios de puesto en el ranking), siempre **sin cruzar a lo infantil**: la disciplina (no juguete, no casino, no confeti gratuito) sigue intacta. La urgencia competitiva se expresa con tiempo y estado claros, no con alarmas estridentes.

## Anti-references

Esto NO debe parecerse a (las cuatro confirmadas por el equipo):

- **Dashboard SaaS genérico**: el admin azul/teal con tarjetas idénticas, gradientes suaves y glassmorphism. Es justo lo que la versión actual aparenta; es el reflejo de categoría a evitar.
- **Infantil / gamificado**: confeti, mascotas, estética de casino o app de premios. Hay energía, pero no es un juguete.
- **Corporativo gris apagado**: ERP/panel administrativo sin vida, denso pero deprimente.
- **Marketing llamativo**: héroes enormes, eslóganes, CTA gigantes. Es una herramienta operativa, no una landing.

Identidad: UMBRAL es **marca propia**. La UCAB (realm `UMBRAL-UCAB`) es solo el contexto académico; no se imita ni se hereda su identidad institucional.

## Design Principles

1. **El estado es el protagonista.** La energía y el color nacen del estado real del juego (Lobby / Iniciada / Cancelada / Terminada, lobby llenándose, ranking moviéndose, QR Legible / No legible), no de decoración. Si un elemento no comunica estado o no permite actuar, sobra.
2. **Una sola consola, dos modos.** Configurar con precisión y supervisar en vivo son ciudadanos de primera clase. Ninguna pantalla obliga a elegir entre densidad de monitoreo y claridad de formulario.
3. **Umbral, no escaparate.** Es el puesto desde donde se opera la experiencia: densa, directa y reconocible. Nunca marketing, nunca juguete, nunca panel gris sin vida.
4. **El backend manda.** La UI refleja y comanda; jamás inventa puntajes, rankings ni reglas de negocio. Muestra exactamente lo que el dominio decide y respeta la propiedad de cada servicio. La validación de cliente es solo usabilidad.
5. **Distinguible a propósito.** Rechaza el reflejo "consola académica → azul genérico". Su identidad se reconoce de un vistazo sin caer en ninguna de las cuatro anti-referencias.
6. **Glanceable bajo presión (móvil).** El participante mira el teléfono de reojo, con prisa y a veces a pleno sol. Lo que importa ahora —la pregunta, el tiempo restante, la acción, la pista— domina la pantalla; toques grandes al alcance del pulgar y contraste que aguanta el exterior. La urgencia se siente sin gritar.

## Accessibility & Inclusion

Objetivo: **WCAG 2.1 AA**.

- Contraste de cuerpo ≥ 4.5:1 y texto grande ≥ 3:1, verificado (incluye placeholders y texto sobre superficies tintadas o proyectadas).
- Totalmente navegable por teclado con foco visible en cada control interactivo.
- `prefers-reduced-motion`: toda animación tiene alternativa (crossfade o transición instantánea); la animación nunca esconde contenido por defecto.
- **El estado nunca se codifica solo por color**: cada estado (partida, QR, ranking) lleva también texto y/o forma, porque la consola se opera en vivo y a veces se proyecta, y debe leerse con daltonismo o con poca luz ambiente.
- **Móvil**: objetivos de toque ≥ 44px, acciones primarias al alcance del pulgar, y contraste reforzado para lectura **a pleno sol** (BDT en exterior). El temporizador y la acción del momento deben ser legibles de un vistazo, en movimiento.
