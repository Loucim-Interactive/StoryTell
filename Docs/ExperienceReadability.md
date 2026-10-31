# Transición al frente, diálogos e interacción

## Recorrido y ambiente

Se conserva el recorrido nativo de main y no se agregan misiones ni condiciones de progreso. `FrontApproachAmbience` aumenta suavemente entre Z=335 y Z=525. Fuera del campamento, las explosiones lejanas provisionales aumentan de volumen y pasan de intervalos de unos 30 segundos a unos 11 segundos. Son recursos existentes, con posición hacia el frente y filtro de paso bajo. No disparan efectos de daño, sacudidas de cámara ni eventos de misión. La reproducción espera durante diálogos, menús y pausa, y deja de programar explosiones adicionales al comenzar el combate nativo.

Tres columnas de humo aumentan su emisión con la aproximación, con un máximo de 36 partículas por columna. Dos refugios colapsados reutilizan el modelo de carpa, con mallas propias adaptadas al terreno, restos de madera y colisión. Están al costado del recorrido, cerca de (60,416) y (48,475). Son elementos provisionales que diseño puede reemplazar por modelos dañados definitivos. Las carpas originales se conservan.

El componente permite atenuar fuentes ambientales de conversaciones en `campVoices`. Actualmente esta lista está vacía porque no hay una pista de murmullo ambiental configurada. Se conectará cuando diseño entregue ese recurso. No se silencian conversaciones de misión, narración ni radio.

## Diálogo

Ya existía la doble pulsación: E/Espacio completa el texto y otra pulsación continúa. También estaban la navegación de respuestas con Tab/arriba/abajo, confirmación con Espacio/Enter y selección con el ratón. Se conserva ese sistema.

Se corrigió una espera automática antes de mostrar respuestas: una línea manual con opciones ahora pasa directamente a ellas al terminar o completar el texto, sin consumir el tiempo de lectura automático. Se mantiene la protección contra confirmar una respuesta con la misma pulsación usada para completar el texto. Los pensamientos y mensajes automáticos conservan sus tiempos.

## Objetos y avisos

`InteractionFocusHighlight` resalta discretamente el objeto enfocado hasta 3,5 metros, usando mallas superpuestas y un material propio. No modifica los materiales originales, no agrega colisiones ni permite detectar objetos a través de paredes. No resalta personajes y se oculta durante mapa, inspección, diálogo, radio y pausa. Las mallas temporales se eliminan al perder el foco.

Los avisos de recogida ahora tienen prioridad sobre la cola de anuncios, con el texto `Recogiste un fragmento · 2/4` y equivalentes para equipo y mapa. Recoger varios fragmentos actualiza el último progreso. La restauración de checkpoint conserva la supresión de avisos antiguos.

## Verificación

31/31 pruebas aprobadas: `ExperienceReadabilityTests`, `DialoguePolishTests`, `GuidanceTests`, `ProgrammingPolishTests`, `MainIntegrationTests`, `WeatherMenuTests` y `CloudSunlightTests`. Informe local: `Library/ExperienceReadability-tests.xml`.

Después de ajustar las mallas al terreno y distinguir el humo de la niebla se repitieron las tres pruebas nuevas: 3/3 aprobadas (`Library/ExperienceReadability-final-tests.xml`). Se inspeccionaron las capturas finales del frente y del objeto enfocado. La captura `ExperienceReadability-focus.png` es solo de UI; `ExperienceReadability-focus-world.png` comprueba la vista del mundo.

Las pruebas nuevas verifican respuestas sin espera automática, oclusión y bloqueo del resaltado por interfaz, prioridad real del aviso al recoger dos fragmentos, aumento de humo e intensidad de ambiente y conservación del estado de misiones. Las capturas auxiliares del frente y del objeto enfocado se guardan en `Library/ExperienceReadability-front.png` y `Library/ExperienceReadability-focus-world.png`.

Respaldo anterior a esta tarea: `Library/ExperienceReadability-inputs/Level1.unity`. No se hizo una medición de rendimiento en hardware de destino ni una evaluación final de audio con los recursos de diseño.
