# Programación y comodidad de Level1

## Progreso y checkpoints

LevelProgress registra campamento, puesto avanzado, frente, aproximación y combate. Las zonas RouteStage de Gameplay_Reliability cambian el estado y se pueden ubicar desde el editor. ObjectiveHud usa esos estados y los eventos de las misiones y conversaciones; ya no decide el destino comparando la coordenada Z del jugador ni recompone sus textos en cada fotograma.

El autoguardado conserva equipo y fragmentos recogidos, misión de la carta, mapa, piezas encajadas, conversaciones completadas y zonas narradas. Se solicita al avanzar y se escribe cuando no hay una interacción abierta ni una llamada pendiente. El armado parcial solicita un guardado al cerrar el puzzle. El combate conserva el checkpoint anterior para reiniciar el tramo con sus enemigos y eventos iniciales.

Hay cuatro posiciones seguras y editables: campamento, puesto, acceso al frente y aproximación al combate. Retomar coloca al jugador en la correspondiente al avance guardado. Al abrir Level1 con una partida válida se ofrece Continuar o Nueva partida. Esc abre la pausa, donde se puede reiniciar el checkpoint. La escritura usa un archivo temporal, reemplazo del archivo principal y respaldo; los datos con versión o estados incompatibles se rechazan y se intenta recuperar el respaldo.

El archivo es `level1-checkpoint.json`, junto con su `.bak`, en Application.persistentDataPath. Las pruebas automáticas usan archivos aislados y no leen ni sobrescriben la partida del usuario.

## Interfaz y opciones

UiSessionCoordinator asigna un propietario al mapa, diálogo, inspección, puzzle, radio o pausa. Cada vista devuelve el estado de controles y cursor que recibió; otra pantalla no puede desbloquearlo. Esc cierra primero la vista activa. La pausa detiene el tiempo y el audio, y los pensamientos no avanzan mientras está abierta. El disparo espera a que se suelte el clic usado en una interfaz antes de volver a aceptar fuego.

La pausa incluye sensibilidad, tamaño de texto, movimiento de cámara, oscurecimiento al disparar y volumen general, voces/radio y efectos/ambiente. Las preferencias se conservan en PlayerPrefs. El tamaño de texto se aplica también a las listas creadas durante el juego; los diálogos conservan su paginación. Las fuentes de audio compartidas del FAL se separaron en hijos con sus referencias y propiedades preservadas. Los controles por categoría respetan las curvas de volumen de los efectos existentes.

## Rendimiento y colisiones

El HUD de objetivos y la munición actualizan su texto cuando cambian sus datos. Se elimina una búsqueda repetida de decisiones en el uso de la radio y un aviso general que se repetía al cargar zonas. VFXManager recupera correctamente el registro opcional y evita acceder a referencias vacías.

InputScript reutiliza sus acciones al habilitarse y las libera al destruirse, para que las recargas de escena no conserven instancias anteriores.

El menú permite guardar un informe en la carpeta Performance de Application.persistentDataPath. RuntimePerformanceMonitor registra hasta 1800 fotogramas de juego, excluye interfaces y pausa, e informa media y percentil 95 del tiempo de fotograma, FPS estimados y máximo de asignaciones observado desde el inicio del nivel. Las exportaciones identifican editor, versión, hardware y etapa. Una medición del editor incluye sus costes y no equivale al rendimiento de un ejecutable.

Las pruebas de colisiones comprueban el volumen del jugador en los cuatro puntos de reaparición. El recorrido ya tiene defensas y límites; no se estrecha la exploración en esta revisión. La evaluación visual y sonora completa y una partida humana siguen siendo complementarias a las pruebas automáticas.

## Verificación

Las 23 pruebas de ProgrammingPolishTests, GuidanceTests, DialoguePolishTests y MainIntegrationTests pasaron en Unity 6000.4.6f1. El resultado está en Library/Programming-release-tests.xml. Cubren recuperación del respaldo ante datos dañados, restauración de equipo/carta/puzzle/mapa/narraciones en una escena nueva, reinicio real del checkpoint, exclusión de interfaces, estados previos de controles, Escape, protección del disparo al cerrar el menú, escalado del texto, captura de métricas y cápsulas libres en los cuatro puntos seguros, además de las funciones anteriores.

Se revisaron las capturas del menú a 1280×720 y 1280×1024 con texto al 125 %. Las capturas de esta herramienta muestran la UI por separado. La medición de rendimiento generada en el editor valida el registro; no certifica FPS del ejecutable ni sustituye la escucha del audio en una partida.
