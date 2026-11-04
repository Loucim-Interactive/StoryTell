# Apoyo de objetos y NPC; respiración en frío

Se conserva la escena guardada por el usuario antes de esta revisión. El respaldo de entrada está en `Library/GroundBreath-backup`. No se cambian las posiciones horizontales de los personajes ni el recorrido de misiones.

Los cuerpos físicos de los NPC quedan cinemáticos y sin gravedad para impedir caídas, giros o desplazamientos accidentales. El apoyo se calibra con la geometría visible de las botas después de inicializar la animación, en vez de usar únicamente los huesos o los límites aproximados del renderer. Se contempla la escala importada del FBX. Los pies se adaptan al terreno al conversar y girar; el pequeño descenso inicial se controla por código. La calibración se hace una vez, sin hornear las mallas en cada frame.

Se ajustaron alturas de suministros y objetos inspeccionables. Los diarios, botiquines, binoculares y radio se apoyan en la mesa; los objetos de cajas se apoyan en su soporte. `Data/GroundBreath/PlacementChanges.json` registra los cambios de interacción y permite verificar sus nuevas posiciones sin desactivar la comprobación del resto de la escena.

La respiración mantiene ParticleSystem, pero ahora usa varias partículas pequeñas, expansión, rotación, ruido suave, bordes difusos e intersecciones suavizadas con la profundidad. El material es procedural y no usa una imagen fija de vapor. La simulación es mundial: el vapor queda en el aire al mover o girar la cámara. Los emisores de los NPC compensan la escala del esqueleto y están junto a la boca.

Se eliminó el inicio de dos rutinas de respiración simultáneas. El jugador exhala aproximadamente cada 9 segundos, con variación de ±1,2 segundos; la emisión dura 0,45 segundos y las partículas se disipan en 0,7–1,2 segundos. Cada emisor está limitado a 32 partículas. El jugador espera antes de su primera exhalación y no emite vapor mientras consulta documentos, mapa u otras vistas modales. Se conserva el sonido provisional a menor volumen.

Las pruebas comprueban el apoyo visible de los once NPC al girar y volver, la ausencia de deriva física, las alturas de los diarios, la escala de los emisores, la cadencia única del vapor, su independencia del movimiento y su ocultación durante la lectura. Se añaden capturas en ejecución y regresión de diálogos, inspección, misiones, checkpoints y combate. No se considera una medición de rendimiento ni una partida completa de un jugador nuevo.
