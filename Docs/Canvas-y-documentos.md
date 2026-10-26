# Canvas y documentos de Level1

La interfaz usa Inter Tight Medium para el cuerpo del texto e Inter Tight Bold para nombres y títulos. Los Canvas se adaptan a la pantalla con una referencia de 1920 × 1080 y modo Expand. Los subtítulos tienen espacio propio para nombre y diálogo; los avisos de interacción quedan por encima. El fondo de los subtítulos se oculta cuando no hay conversación.

Los diarios `Diario1` y las cartas opcionales conservan la inspección del objeto en 3D. Al interactuar con E, el documento se acerca a la izquierda y su transcripción se muestra a la derecha. Clic izquierdo y ratón permiten girarlo; WASD lo desplaza dentro de un margen; la rueda desplaza la transcripción si no entra completa. E deja el documento en su lugar. Durante la inspección se suspende el movimiento del personaje y se guarda el mapa; al terminar se recuperan los controles y la disponibilidad previa del mapa.

## Reemplazar textos

Seleccionar el diario o la carta y editar el componente `InspectableDocument`:

- `Title`: encabezado de la transcripción.
- `Transcription`: texto legible, con saltos de línea. Se muestra literalmente, sin interpretar etiquetas de formato.
- `Inspection Offset`, `Inspection Euler Angles` y `Inspection Size`: posición, orientación inicial y tamaño durante la inspección.

Las cartas opcionales siguen siendo ficticias y provisionales. La transcripción inicial de Diario1 conserva el texto que ya tenía el objeto; debe reemplazarse por el texto del periódico si el equipo entrega una reproducción concreta. Cambiar estos campos no altera el progreso de las misiones. Los cuatro fragmentos de la carta de la madre mantienen su recogida y entrega obligatorias: al interactuar se registra la recogida, se inspecciona el fragmento y se guarda al cerrar. La carta reunida debe reconstruirse con el minijuego antes de entregarla. L desde el mapa abre el armado; después permite inspeccionar y leer la carta. El texto de la madre proviene del campo motherLetter del asset SO_GameplayPath_Level1.

El prefab `Assets/Prefabs/Interactables/Diario1.prefab` incluye los campos de transcripción para nuevas instancias. Las instancias ya colocadas en Level1 están configuradas. El componente `DocumentInspectionPanel` de la escena presenta la lectura lateral.

Antes de aplicar el ajuste, la herramienta del editor guarda una copia de Level1 en `Library/GameplayPathBackups`. El menú `StoryTell > Gameplay Path > Polish Canvas and document inspection` instala esta configuración una sola vez y evita duplicarla.

Verificación: 12 pruebas aprobadas, 0 fallidas y 0 omitidas en Unity 6000.4.6f1. Incluye cierre con E mediante Input System, restauración del diario, lectura de cartas y fragmentos, carta reunida y recorrido de Level1. Se revisaron capturas a 1280 × 720, 1920 × 1080 y 1280 × 1024.
