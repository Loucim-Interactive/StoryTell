# Correcciones de la prueba con compañeros

Se conserva el comedor común y el recorrido lineal de main. La instrucción de comer se registra al abrir la conversación de regreso con Diego, después de recoger el equipo. Antes dependía de terminar toda esa conversación: salir tras recibir la indicación dejaba la comida bloqueada. Se reprodujo el fallo antes del cambio en Library/PlaytestFeedback-baseline.xml y pasó después. Comer sigue requiriendo recoger el equipo y volver a hablar con Diego. Mateo distingue equipo pendiente, regreso pendiente y comida pendiente; Diego reconoce si ya comió.

Mateo pide reconstruir y devolver su carta, sin pedir que el jugador lea correspondencia personal. Se conserva el requisito de lectura del recorrido previamente acordado; este cambio no vuelve opcional esa mecánica. Se actualizó la frase activa MateoOffer_2 y el texto de aceptación nativo.

La ayuda de inspección indica explícitamente mantener el clic izquierdo mientras se mueve el ratón. El giro y el zoom existentes siguen usando Input System. La prueba del giro introduce un ratón simulado y usa una copia temporal de InputSettings para dirigir sus eventos al juego en batchmode; restaura esa copia al terminar. El juego no cambia sus preferencias de entrada.

Las tres imágenes de menú conservan sus dimensiones originales al importarse, en lugar de redimensionarse a potencias de dos. Se elimina el fondo duplicado del cuaderno, se utilizan sus páginas completas y se ajustan tamaños y contraste de los controles. La sombra del menú principal cubre el fondo sin el corte vertical que atravesaba el título. Se conserva el arte y la tipografía de los menús.

El mapa mantiene proporción 3:2 y se ajusta al lado izquierdo según la relación de aspecto, independientemente del FOV del jugador. La leyenda tiene márgenes, texto legible y una cabecera sin recortes. Los siete indicadores evitan superponerse, permanecen dentro del papel y dejan libre el punto del jugador; las líneas siguen apuntando a sus posiciones reales. La conversión usa coordenadas normalizadas de la cámara y el espacio del Canvas raíz para evitar diferencias de resolución entre cámaras.

## Evidencia

Unity 6000.4.6f1 / Windows / URP. Siete casos distintos aprobados, tomando el resultado más reciente por caso:
- Library/PlaytestFeedback-results.xml: recorrido completo, secuencia equipo/comida/Mateo y regresión de salida anticipada de Diego.
- Library/PlaytestFeedback-visual-results.xml: navegación MainMenu → GameSelect → Level1 → MainMenu y acciones de menú. La comprobación de ratón de esta ejecución fue reemplazada por la siguiente.
- Library/PlaytestFeedback-final-results.xml: giro real mediante eventos del Input System.
- Library/PlaytestFeedback-map-player-results.xml: mapa en 1280×720 y 1280×1024, siete indicadores, punto del jugador libre, leyenda completa y referencias tras caminar/mirar.

Se revisaron capturas de ambos menús y del mapa. Las capturas del mapa componen los pases de mundo/papel e interfaz, ya que las solicitudes de render separadas de URP no incluyen de forma fiable todo el Canvas en el mismo pase. Los archivos temporales de fuentes y la partida original se restauraron al terminar las pruebas.

Queda pendiente una nueva prueba manual con los compañeros. El ZIP del 22/11/fd5f281 no se regeneró y todavía no incluye el comedor ni estas correcciones.

Herramientas: PlaytestFeedbackAuthoring.Apply, PreserveMenuArtProportions y FixMateoOffer; Apply requiere una escena sin el cambio previo. Respaldo de la escena al empezar esta revisión en Library/PlaytestFeedback-backup, que ya incluye el comedor.
