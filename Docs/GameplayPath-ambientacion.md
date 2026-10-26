# Ambientación visual y sonora de Level1

Esta pasada de GameplayPath conserva las misiones y el recorrido existentes. El objetivo es mejorar la lectura del campamento y las coberturas, diferenciar lugares de refugio y aumentar la presencia del frente mediante sonido y niebla.

## Iluminación y imagen

La luz solar se orientó a 38°/-35°, con intensidad 1.4, temperatura 6500 K y sombras suaves de fuerza 0.8. El exterior usa ambiente de tres colores y una luz direccional de relleno fría, sin sombras, para evitar superficies completamente negras. Dos lámparas provisionales marcan la cocina y la carpa del sargento; no tienen colliders y sus luces puntuales no generan mapas de sombras adicionales.

La niebla ExponentialSquared tiene densidad 0.0032 en el campamento y llega gradualmente a 0.0045 cerca del frente. Los valores son editables en Level1Atmosphere. La dirección de arte sigue siendo diurna, fría y poco saturada. Se creó una copia del cielo para esta escena.

La cámara principal ahora tiene postprocesado y FXAA. El nuevo perfil de Volume tiene prioridad 15, saturación -15, contraste +5, exposición +0.15, balance algo frío, bloom leve y viñeta suave. Se desactivan explícitamente el motion blur y el desenfoque de profundidad para mantener la lectura de objetos. Las cámaras separadas del mapa y del rifle conservan su configuración sin postprocesado. La UI de Canvas conserva sus fuentes y colores.

No se modificaron los perfiles compartidos, los materiales originales de modelos ni los assets globales de render. El PC_Renderer ya tenía SSAO; se conserva. La resolución de sombras, sus cuatro cascadas y su distancia de 50 m también se conservan. Esta pasada añade postprocesado y tres luces sin sombras; el coste real de GPU y los FPS requieren medición jugando en el equipo objetivo.

## Sonido

Level1Atmosphere gestiona cuatro AudioSources reutilizados: viento de fondo, roce de hierba localizado en el campamento, explosiones lejanas hacia el frente y gaviotas ocasionales cerca del campamento. Utiliza los clips existentes de SoundSystem/Assets/Ambient. No hay nuevas voces ni grabaciones definitivas.

El viento sube ligeramente al acercarse al frente y disminuye la presencia de sonidos del campamento. Las explosiones lejanas se espacian entre 17 y 29 segundos y se ubican por delante del jugador; no disparan efectos físicos, daño ni camera shake. Los VFX cercanos conservan sus sonidos y lógica actuales. No se modifica AudioListener.volume ni el volumen de diálogos o del FAL.

La mezcla ambiental baja al 40% durante diálogos/lecturas y al 65% con el mapa abierto. Al finalizar la escena se apagan las fuentes ambientales gradualmente y se detienen las explosiones y gaviotas. Desactivar el componente restaura la densidad de niebla que tenía la escena al activarlo. La calidad de los bucles y los niveles definitivos necesitan una escucha durante una partida.

## Edición y respaldo

Jerarquía: `====GAMEPLAY====/GameplayPath_Level1/Atmosphere_Level1`.

Assets propios: `Assets/Systems/GameplayPath/Data/Atmosphere/`. El perfil, el cielo y los dos materiales de lámparas son específicos de esta escena. El instalador evita duplicados. La escena anterior está en `Library/GameplayPathBackups/Level1-before-atmosphere-*.unity` y contiene también los cambios de checklist y límites hechos antes de esta pasada.

## Evidencia

El inventario original de luces, cámaras, audio y pipeline está en `Library/GameplayPath-atmosphere-audit.txt`. Las capturas comparables de cocina, equipo, campamento y frente están en `Library/Atmosphere-*-before.png` y `Library/Atmosphere-*-after.png`. Usan la misma resolución, posiciones, objetivos y FOV; son vistas del mundo renderizadas por URP, sin el Canvas. No sustituyen una partida completa con la UI, partículas en movimiento y audio.

Level1AtmosphereTests comprueba las referencias, las luces, la separación de cámaras, los tonos medios de las capturas y las transiciones de mezcla/niebla durante el recorrido, lectura y final. Se acompaña con las regresiones existentes de misiones, mapa, carta, límites, NPC y combate.

Los modelos, animaciones, texturas de cartas y varios materiales heredados continúan siendo provisionales. Esta pasada no incorpora guardado de partida, nuevas animaciones de soldados ni cambios de oleadas.

Resultado: las 27 regresiones existentes pasaron en `Library/GameplayPath-atmosphere-tests.xml`. La primera comprobación de imagen detectó la exportación lineal demasiado oscura; tras pasar a una salida SDR sRGB y regenerar antes/después, las tres pruebas de ambientación pasaron en `Library/GameplayPath-atmosphere-final-tests.xml`. No se midieron FPS ni se certificó una escucha subjetiva del audio.


Separación de entrega: esta ambientación se publica en `GraphicsAtmosphere`, creada desde `GameplayPath`. La rama GameplayPath mantiene la iluminación anterior y contiene la checklist, límites y recorrido. En la versión separada pasaron las 30 pruebas de `Library/GameplayPath-graphics-release-tests.xml`. La prueba de imágenes regenera sus capturas desde Level1 para funcionar también después de clonar el repositorio, sin depender de PNG previos de Library.
