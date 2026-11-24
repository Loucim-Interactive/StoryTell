# Cocina reubicada y puesto de comunicaciones

Se toma la mesa de cocina colocada por el usuario como referencia: posición mundial (83.7, 27.80, 253.5), orientación Y 73.3°. Su posición, rotación y escala se conservan. También se conservan las nuevas posiciones de las carpas y del sargento que el usuario guardó antes de este trabajo.

Se trasladaron la ración, la segunda mesa y la fogata junto a esa referencia. Mateo, Máximo, Simón y Néstor acompañan el nuevo comedor. Se despeja únicamente la vegetación de su huella, con bordes suaves. El punto de enfoque y las referencias de cocina/Mateo del mapa acompañan el cambio. Diego y los personajes que dan indicaciones señalan el sector sur; las pistas cercanas al comedor anterior ahora mencionan comunicaciones y suministros.

El espacio anterior tiene un puesto decorativo en la raíz `Comunicaciones del campamento`: mesa existente reutilizada, radio de campaña, visor, selectores, antena, cable y dos baterías. Radio, antena y baterías son modelos provisionales de primitivas, con materiales oscuros acordes al ambiente. Pueden reemplazarse por modelos del equipo de diseño. No agrega una misión ni sustituye la radio portátil del jugador o el puesto de Benítez. El mapa incorpora la zona 8, Comunicaciones, conservando los índices de las misiones existentes.

## Comprobaciones

Unity 6000.4.6f1 / Windows / URP. Cuatro casos distintos aprobados:
- Mesa colocada por el usuario conservada; comida accesible desde 22 posiciones, un solo objeto de comida, cuatro NPCs próximos y comunicaciones con referencia propia.
- Apoyo de las botas y estabilidad de los NPCs.
- Mapa en dos relaciones de aspecto con ocho indicadores, leyenda completa y punto del jugador libre.
- Recorrido equipo → comida → carta → sargento.

Resultados: Library/KitchenRelocation-results.xml, tomando para el recorrido la ejecución posterior Library/KitchenRelocation-route-verified.xml. La primera ejecución intentaba hablar mientras seguía activa una llamada de radio nativa; la prueba ahora espera su lectura y elige una respuesta antes de abrir la conversación de NPC. El comportamiento del juego y de la radio no se alteró por ese ajuste de prueba.

Capturas revisadas: Library/KitchenRelocation-kitchen.png, Library/KitchenRelocation-communications.png y Library/KitchenRelocation-layout.png. Respaldo de escena y terreno al inicio del trabajo: Library/KitchenRelocation-backup. La partida y los archivos temporales de fuentes se restauraron al terminar. No se regeneró el ZIP de prueba.
