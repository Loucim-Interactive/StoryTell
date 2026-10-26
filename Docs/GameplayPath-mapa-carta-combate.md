# Mapa, conversaciones, carta y combate de Level1

Cambios de la rama GameplayPath:

- El mapa físico mide 48 × 32 cm y el relevamiento abarca el recorrido jugable. Conserva el punto celeste del jugador.
- Las referencias 1–6 corresponden a equipo, cocina, carpa principal, puesto avanzado, paso de bombas y frente. Las posiciones proceden de objetos de Level1, no de coordenadas dibujadas a mano. Los carteles de orientación anteriores están desactivados y se ajustaron sus menciones en los diálogos.
- Rubén, Gómez, el sargento y el oficial giran suavemente hacia el jugador durante sus conversaciones. Al terminar o cancelar, recuperan su orientación anterior.
- Se añadieron ocho conjuntos de cobertura, alternando sacos y cajas apiladas. Tienen colliders que bloquean disparos y navegación; se recalculó el NavMesh y se verificaron las tres rutas.
- El contacto enemigo comprueba la proximidad de los cuerpos, excluye los colliders del propio enemigo y del jugador de la prueba de paredes y funciona aunque el agente haya salido del NavMesh. Una pared real sigue impidiendo el contacto. El cierre muestra negro y «Finalización escena 1».

## Reconstruir la carta

Se necesitan los cuatro fragmentos. Con el mapa abierto, L inicia el armado; también se inicia al hablar con Rubén antes de devolver la carta si aún falta reconstruirla.

Arrastrar con el botón izquierdo; seleccionar un fragmento y usar R o clic derecho para girarlo 90°. Las piezas encajan si la posición y orientación se aproximan al lugar correcto. No hay tiempo límite. E, Esc y el botón Volver cierran el tablero conservando las piezas ya colocadas. Terminar el tablero no entrega la carta automáticamente: hay que volver a interactuar con Rubén.

Después del armado, L desde el mapa permite inspeccionar y leer la carta completa. Leerla continúa siendo opcional. La reconstrucción y la entrega son obligatorias para avanzar hacia el sargento.

El texto se edita en `Assets/Systems/GameplayPath/Data/SO_GameplayPath_Level1.asset`, campo `motherLetter`; tanto las piezas como la lectura de la carta completa toman ese contenido. Las cartas opcionales mantienen sus transcripciones propias. Los textos actuales siguen siendo ficticios y provisionales.

## Objetos de edición

Los elementos nuevos permanecen bajo `====GAMEPLAY====/GameplayPath_Level1`. El tablero está en `GameplayPath_UI/SafeArea/LetterAssembly`; las referencias del mapa en `GameplayPath_UI/SafeArea/MapZones`; las coberturas nuevas en `Battlefield_ExistingExplosionZone/Additional_Combat_Cover`. El componente `NpcConversationFacing` configura la velocidad de giro de cada NPC.

La escena anterior se respaldó en `Library/GameplayPathBackups/Level1-before-next-*.unity`. La herramienta de instalación evita duplicar el contenido. Las referencias de escenas, prefabs y navegación se conservan.

## Verificación

Se aprobaron 24 pruebas de recorrido, referencias, navegación, mapa e interfaz. Después se repitieron las cinco comprobaciones afectadas por el ajuste visual, incluyendo arrastre real mediante EventSystem y GraphicRaycaster, encaje, reanudación, giro y retorno del NPC, pared que bloquea contacto y aparición del final por contacto. Las vistas de prueba de escena e interfaz usan pasadas de cámara independientes; la superposición final debe revisarse en Game View durante una partida completa. El equilibrio de dificultad y la duración total siguen requiriendo una prueba humana.
