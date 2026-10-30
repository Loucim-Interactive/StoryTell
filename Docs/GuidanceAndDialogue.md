# Orientación y diálogos de Level1

La escena utiliza los sistemas y el recorrido de main. El objetivo actual aparece arriba a la izquierda y usa indicaciones breves. La lista muestra las tareas de la misión activa; las marcas y los contadores usan el estado real de equipamiento y carta. Los avisos de recogida y finalización esperan mientras la lista está oculta por una conversación, el mapa o una inspección.

El destino naranja del mapa corresponde a la zona de la tarea actual. Durante la búsqueda de la carta identifica el campamento, no la posición exacta de cada fragmento: Simón y José dan pistas para las piezas pendientes. El mapa sigue siendo un objeto que el jugador recoge junto al equipo. El mapa y la lista conservan la progresión opcional y las decisiones propias de main.

Los diálogos presenciales se muestran con páginas y permiten completar el texto o continuar con E, Espacio o Enter; Esc cancela. Tab o las flechas seleccionan respuestas y Espacio, Enter o clic confirman. Las acciones de conversación, sus bifurcaciones y las misiones siguen usando los datos de main. La interfaz escala con la resolución y utiliza las fuentes Inter Tight.

Los NPC usan el controlador existente con ajuste de pies sobre el terreno y vuelven a su orientación tras la conversación. Las coordenadas horizontales de los actores se conservan. La carta puede entregarse al recoger los cuatro fragmentos; reconstruirla con L continúa siendo opcional, como en main.

Las 16 pruebas de GuidanceTests, DialoguePolishTests y MainIntegrationTests pasaron en Unity 6000.4.6f1. El resultado está en Library/Guidance-final-tests.xml. Incluyen la aceptación de las misiones nativas, la recogida de equipo y fragmentos, la variación de pistas, la devolución por diálogo, el bloqueo y desbloqueo del frente, los destinos del mapa, la paginación y cancelación de diálogos, la selección de respuestas, los pies de los nueve NPC, la inspección, el puzzle y el arma.

Las capturas generadas desde el editor muestran la UI por separado y no certifican la composición completa del mundo con la interfaz. Son necesarias una partida humana completa, una escucha del audio y una medición de rendimiento en el equipo de destino para valorar la experiencia final.
