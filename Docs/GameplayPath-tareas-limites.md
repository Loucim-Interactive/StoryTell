# Checklist y límites de Level1

Cambios de la rama GameplayPath.

## Tareas

`GameplayPath_UI/SafeArea/TaskHud` muestra arriba a la izquierda la tarea activa y sus pasos. `[ ]` indica pendiente; `[X]`, completado. Al cambiar de etapa conserva la anterior marcada durante siete segundos y muestra durante ocho segundos el objetivo completo en un aviso superior. Equipo y fragmentos se actualizan al recogerlos; reconstruir la carta también actualiza su casilla.

La checklist y el aviso se ocultan al abrir el mapa, durante conversaciones, inspecciones y el minijuego, y al finalizar la escena. El mapa conserva la leyenda de zonas, la ubicación del jugador y los controles de mapa/carta. No hay dos listas de tareas simultáneas.

Se usa la tipografía Inter Tight y el CanvasScaler existente. El panel deja libre la zona central para los avisos de interacción.

## Límites

`GameplayPath_Level1/RouteBoundaries` contiene un polígono de once tramos que rodea el campamento, la subida, el paso de bombas y la arena final. No restringe el recorrido en función de la misión, pero evita explorar todo el terreno exterior. Los puntos X/Z se editan en `GameplayPathBoundary.outline`; los colliders deben conservar la misma correspondencia por índice.

La primera vez que el jugador se aproxima a cinco metros de un tramo, el protagonista muestra un pensamiento acorde a su tarea. La cámara gira suavemente hacia la última posición segura y el personaje retrocede unos dos metros usando su CharacterController para respetar obstáculos. Dura un segundo; después recupera sus controles y se activa la pared invisible de ese tramo. Otro intento en ese tramo encuentra una pared física, sin repetir continuamente el giro. Cada tramo se recuerda durante la sesión actual.

Los límites se suspenden durante diálogos, inspecciones y otros controles temporales del personaje. Al finalizar la escena tampoco intervienen. Si por un teletransporte o un salto de fotograma se detecta al jugador fuera del polígono, se lo devuelve a su última posición segura.

Las barreras se guardan desactivadas y no modifican el NavMesh existente. Sus colliders cubren la altura del terreno y se activan durante la partida. La escena previa se respalda en `Library/GameplayPathBackups/Level1-before-hud-boundaries-*.unity` al ejecutar el instalador.

## Verificación

`GameplayPathHudTests` comprueba el contenido del recorrido dentro del margen, las casillas y cambios de misión, el ocultamiento al abrir el mapa o leer, el giro y la restitución de controles, y la colisión física del CharacterController en un segundo intento. Se ejecuta junto a las pruebas existentes del recorrido, carta, NPC, mapa y combate. Pasaron las 27 pruebas del conjunto de regresión. Tras el ajuste final del aviso durante el giro y de la protección del bloqueo al terminar la escena, pasaron de nuevo las cuatro pruebas afectadas. Resultados: `Library/GameplayPath-hud-final-tests.xml` y `Library/GameplayPath-hud-controls-tests.xml`. La vista de interfaz se revisó en `Library/GameplayPath-hud-checklist.png`; esa captura usa una pasada de UI separada y no certifica la composición final de Game View. La dificultad y la sensación del giro requieren una partida humana completa.

