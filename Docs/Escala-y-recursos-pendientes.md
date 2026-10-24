# Ajuste de escala y colocación — Level1

Aplicado sobre la rama GameplayPath. Se preservaron los cambios que ya tenía el proyecto en fuentes, material de humo y ajustes de tiempo.

## Cambios realizados

- Referencia: controlador del jugador de 1,82 m. Mesas de suministros y cocina reducidas desde 1,34–1,43 m hasta 0,78 m.
- Equipo reunido sobre la mesa, sin piezas flotantes ni salientes fuera del tablero. FAL acostado; casco, radio, mapa y munición separados. Colisiones adaptadas al modelo visible y recogida comprobada desde el frente de la mesa.
- Ocho carteles reducidos a postes de 1,32 m con tableros de 1,35 × 0,30 m, orientados hacia el tramo de llegada. Texto en ambas caras y flechas adaptadas a cada lado.
- Jarro, olla, tapa, cartas y caja de suministros ajustados a proporciones más cercanas al personaje. Añadido un apoyo sencillo bajo la olla.
- Fragmentos y cartas opcionales apoyados sobre cajas apiladas, por encima del pasto, repartidos alrededor de la cocina. Las lecturas opcionales permanecen separadas de la carta de misión.
- NavMesh recalculado después de modificar las colisiones. Respaldos de Level1 en Library/GameplayPathBackups.

## Qué conviene aportar después

1. Textos definitivos de las cartas de los compañeros: indicar título, autor ficticio o testimonio real y fuentes de los datos históricos. La entrega obligatoria de la carta no depende de leer el texto.
2. Recursos finales de primera persona: brazos/manos con esqueleto y animaciones de equipar, disparar, recargar y destrabar. El movimiento actual del arma es provisional.
3. Modelos de cocina y equipo acordes al estilo: jarro con asa, olla/calentador de campaña, radio portátil y casco final. Las formas simples actuales permiten probar el recorrido, pero no tienen todos los detalles de un modelo terminado.
4. Audio con permiso de uso: voces de diálogos, FAL, recarga y respiración. Dar los archivos separados y sus permisos o procedencia. Por ahora se usan textos y sonidos provisionales.
5. Una prueba humana del recorrido: medir cuánto dura sin leer todas las cartas, anotar dónde se pierde la orientación o cuesta interactuar y si las ráfagas, sacudidas y oscuridad resultan cómodas. El objetivo de 15–20 minutos aún debe validarse jugando.

No hace falta modificar código para usar estos ajustes. Se puede seguir mejorando iluminación, colocación, pistas y efectos con los recursos existentes. Los modelos detallados y las animaciones de manos sí requieren arte adicional.

También se apoyó junto a los suministros una caja anterior que estaba flotando sobre el campamento, conservando sus componentes e interacción.
