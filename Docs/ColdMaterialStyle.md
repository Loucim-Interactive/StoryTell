# Materiales fríos de Level1

Rama: MainExperiencePolish. La escena usa variantes independientes de los materiales originales, con las mismas texturas de color, normales y metalicidad. No se sustituyen archivos de textura ni se cambian las asignaciones de los prefabs originales.

Se crean 11 variantes para 264 ranuras de material: lona de carpas (18), madera (14), piedra (87), sacos de arena (92), uniformes y cascos (45), bidones y otros suministros metálicos (8). La clasificación se limita a los objetos reconocidos de Level1; no se aplica un filtro general a todos los objetos.

La lona se desatura hacia un gris verdoso; la madera conserva su veta y reduce los tonos cálidos; la piedra mantiene su relieve con zonas húmedas ligeramente más oscuras. Un patrón suave de humedad cambia el brillo por zonas y el barro se concentra cerca de la base de los objetos estáticos. Los uniformes reciben un cambio suave de saturación y acabado, sin barro automático en la piel ni en las caras.

El shader ColdWeatherSurface utiliza iluminación PBR de URP, sombras, normales, profundidad y oclusión ambiental. Conserva los mapas originales y permite ajustar tinte, saturación, humedad, altura de suciedad y acabado seco. Los materiales de cartas, mapa y fogatas conservan su presentación. No se agregan nieve, luces, postprocesado ni cambios de terreno.

El inventario de materiales, su origen y su uso queda en Assets/Systems/MainIntegration/Data/ColdStyle/Manifest.json. La iluminación y niebla se comparan exactamente con el registro anterior a esta modificación.

Las pruebas de ColdStyleTests comprueban texturas conservadas, fuentes originales independientes, pases del shader, persistencia de asignaciones, iluminación intacta y renderizado desde la cámara del jugador. También se comprobaron los estilos y posiciones de UI previos y las colisiones de la escena. Las capturas comparativas se encuentran en Library/ColdStyle-*.png; las vistas del editor sirven para observar superficies y la captura player corresponde al juego en Play.

Resultados: 5/5 comprobaciones aprobadas en Library/ColdStyle-tests.xml. Tras afinar el tono final de la lona se repitieron las tres comprobaciones de materiales y cámara: 3/3 aprobadas en Library/ColdStyle-final-tests.xml, sin errores de shader ni excepciones de ejecución en esos registros.

Verificación realizada en Unity 6000.4.6f1, URP y el renderizador Forward Plus del proyecto. Quedan pendientes una evaluación de FPS y una compilación para el equipo objetivo. El respaldo de escena está en Library/LevelPolishBackups/BeforeColdStyle-*.unity. Los cambios que ya existían en otros archivos del proyecto quedan fuera de esta entrega.
