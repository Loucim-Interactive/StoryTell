# Claros de sol móviles — Level1

La luz direccional usa una máscara de nubes repetible de 110 × 110 metros. Las zonas cubiertas reciben el 14 % de la luz directa y los claros hasta el 100 %, con bordes suaves. La luz directa máxima pasa de 0,9 a 2,7; la iluminación ambiente, la niebla, el color del sol, su dirección y la exposición se conservan. Así aparecen diferencias entre áreas iluminadas y cubiertas sin oscurecer toda la escena al mismo tiempo.

`MovingCloudSunlight` desplaza la cobertura a 0,65 / 0,22 metros por segundo, siguiendo el tiempo del juego. La textura se generó con ruido procedural periódico en Unity y no requiere imágenes externas. Las carpas y el barro admiten la máscara de luz mediante variantes `_LIGHT_COOKIES`. El terreno y los materiales URP compatibles la reciben por el sistema nativo de luz. Los haces en el aire aumentan ligeramente de intensidad y se atenúan cuando quedan bajo la cobertura de las nubes.

Archivos principales:

- `Assets/Systems/MainIntegration/Scripts/MovingCloudSunlight.cs`
- `Assets/Systems/MainIntegration/Data/WeatherMenu/MovingCloudCookie.png`
- `Assets/Systems/MainIntegration/Editor/CloudSunlightAuthoring.cs`
- `Assets/Systems/MainIntegration/Editor/CloudSunlightTests.cs`

La velocidad se ajusta en el componente de Directional Light. El tamaño de la proyección se ajusta en Universal Additional Light Data. La intensidad del sol regula el brillo máximo de los claros; el contraste de la cobertura está en la textura. La niebla mantiene su configuración anterior.

La prueba captura el campamento desde la misma cámara en los segundos 0 y 50 de la cobertura y comprueba un cambio visible en la mitad inferior de la imagen, sin variar la intensidad global, la iluminación ambiente ni el estado de niebla. También se ejecutaron las cuatro pruebas de clima y menús. No se agregaron luces puntuales ni pases de pantalla completa para proyectar las nubes; se usa la máscara nativa de URP sobre el sol existente.

Verificación: 5/5 pruebas aprobadas en `Library/CloudSunlight-tests.xml`; después del ajuste final de contraste, 1/1 en `Library/CloudSunlight-final-tests.xml`. Las dos capturas finales fueron inspeccionadas. El cambio medio en la mitad inferior de la imagen fue de 0,012977 (escala normalizada 0–1). Esta evidencia verifica el cambio de cobertura; no es una medición de rendimiento de GPU.

Respaldo local de la escena anterior: `Library/CloudSunlight-backup/Level1.unity`. Capturas: `Library/CloudSunlight-camp-0.png` y `Library/CloudSunlight-camp-50.png`.
