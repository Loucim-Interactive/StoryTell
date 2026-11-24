# Mapa limpio y cráteres del frente

2026-11-23 · StoryTell · rama `MainExperiencePolish`.

## Cambios aplicados

- Se retiraron de la cartografía los rectángulos sólidos que el generador dibujaba para representar carpas. Los ocho lugares de interés conservan sus números y líneas de ubicación, con fondos circulares de color tierra; el jugador y el destino actual conservan sus colores.
- La imagen del mapa permanece oculta hasta que su cámara termina de renderizar. Las texturas nuevas se limpian de forma explícita antes de usarse y se actualiza el tamaño de la interfaz al abrir. Esto evita exponer una imagen inicial sin contenido o mostrar la imagen de la apertura anterior.
- Se regeneraron las curvas del mapa usando el terreno actualizado.
- Se crearon cinco cráteres de impacto, con radios aproximados de 4,5–6,2 metros, depresiones de 0,9–1,2 metros, borde irregular y tierra húmeda removida. Los centros están en X/Z: `(27,535)`, `(94,546)`, `(35,560)`, `(104,578)` y `(70,609)`.
- Son depresiones del relieve real, con la colisión del terreno correspondiente. La cobertura de pasto disminuye dentro del impacto y vuelve gradualmente hacia afuera.
- Se usa una copia local de TerrainData en `Assets/Systems/MainIntegration/Data/MapFrontCraters/FrontTerrain.asset`. El asset de terreno anterior permanece sin modificar; las alturas de cocina, equipo, puesto, inicio del combate y punto de aparición de enemigos se verifican iguales a las anteriores.
- Se reconstruyó la navegación del frente sobre el relieve actualizado, en un asset nuevo. No se cambió el inicio, la cantidad de enemigos ni las condiciones del final.

## Verificación ejecutada

Unity 6000.4.6f1, Windows. Árbol de trabajo con cambios previos y actuales, sin commit.

`Library/MapFrontCraters-results.xml`: **6/6 pruebas correctas**.

1. Los cinco impactos tienen profundidad real y colisión coincidente; los lugares del recorrido externos a los impactos conservan sus alturas.
2. Las rutas desde el centro y los dos flancos del campo llegan al jugador con caminos completos.
3. Tres aperturas y cierres del mapa muestran el papel únicamente después del renderizado, con ocho destinos circulares y sin los antiguos rectángulos en la textura.
4. Capturas del terreno del frente y de un impacto a altura del jugador.
5. La prueba de combate conserva entradas de enemigos por delante del jugador y finalización tras completar las oleadas; cruzar la antigua salida no termina la escena.
6. Los indicadores del mapa permanecen unidos a sus ubicaciones al caminar y girar.

Capturas revisadas: `Library/MapFrontCraters-map.png`, `Library/MapFrontCraters-crater-near.png` y `Library/MapFrontCraters-front.png`. La captura del mapa usa el pase de UI de las pruebas; no acredita una partida manual completa. En el frente, el humo existente puede ocultar parte de los cráteres desde algunas posiciones. El acabado usa los materiales disponibles, sin nuevos modelos de producción ni audio.

## Respaldo y entrega

`Library/MapFrontCraters-backup/` conserva escena, código del mapa, fuentes y checkpoints anteriores. Los checkpoints y fuentes se restauran tras las pruebas.

No se generó otro build ni se hizo commit o push. No se midió rendimiento en las computadoras de los compañeros.
