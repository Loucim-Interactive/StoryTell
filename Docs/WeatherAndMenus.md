# Clima, superficies y menús de StoryTell

## Cambios

- MainMenu y GameSelect incorporan el diseño de Tell-Tale y usan el recorrido, las misiones y el checkpoint de StoryTell. No se importó el sistema de guardado del otro proyecto.
- Nueva partida abre la selección; Parte 1 inicia Level1 desde cero. Continuar restaura el checkpoint nativo. Partes 2 y 3 quedan deshabilitadas. Volver regresa al menú principal.
- El cuaderno conserva su proporción al adaptarse a la pantalla. Se ampliaron los botones y se separaron las opciones. Los controles usan las acciones UI del Input System existentes.
- Las nubes se desplazan con velocidad y dirección editables en MovingSky.mat. Tres haces suaves acompañan las aperturas del cielo. Son mallas con transparencia e intersección suavizada por profundidad, no iluminación volumétrica con sombras.
- Cinco fogatas tienen leña carbonizada, brasas, llamas perfiladas, humo desplazado por viento y luz de intensidad variable. No se agregó audio de fogata.
- Nueve carpas se elevaron entre 6 y 18 cm, manteniendo sus coordenadas horizontales. PlacementReport.json conserva las posiciones anteriores y posteriores.
- Los materiales ColdStyle conservan sus mapas originales y suman variación normal fina y humedad moderada.
- Se agregaron 17 parches visuales de barro que siguen la altura del terreno, con bordes atenuados y 2057 vértices. No tienen colisión ni ralentizan al jugador. El TerrainData y su vegetación se conservaron.

## Correcciones de transiciones

SceneTransitionView ahora crea explícitamente el AudioSource cuando falta; el operador de coalescencia no detectaba correctamente el objeto nulo de Unity. SceneTransitionManager valida las escenas y limpia su instancia al destruirse. Los menús tienen un perfil de transición persistente y PanelSettings asignados. La entrada del jugador se suspende durante el fundido y el checkpoint espera su finalización.

## Textura de barro: procedencia

Generada con la herramienta integrada imagegen, no descargada de un paquete externo. Archivo del proyecto: `Assets/Systems/MainIntegration/Data/WeatherMenu/ColdPeatMud.png`. El mapa normal usa una copia importada como altura/normal por Unity. El resultado es provisional y reemplazable.

Prompt exacto:

> Create a production game terrain albedo texture, square, seamless tileable edges, orthographic straight top-down flat material scan, no perspective. Cold subantarctic wet peat mud for a restrained stylized 3D historical Malvinas/Falklands camp game. Desaturated charcoal brown and grey olive soil, soft broad organic clumps and compacted fine grit, subtle shallow boot-worn smears without recognizable complete footprints, occasional sparse flattened straw fibers. Small dark damp depressions, NOT reflective white highlights and no baked directional lighting, no cast shadows, no objects, no text, no borders. Medium detail coherent with simple stylized props, not ultra photoreal or cartoon outlined. Even diffuse illumination, moderate low contrast, no snow, no ice, no orange clay, no large stones, no large puddles. Texture fills the entire image; opposite edges designed to repeat smoothly.

La fuente Black Ops One se importó con su licencia OFL.

## Verificación

Resultado: **27/27 pruebas aprobadas**, Unity 6000.4.6f1, EditMode con entradas temporales a Play. Incluye WeatherMenuTests, ProgrammingPolishTests, GuidanceTests, DialoguePolishTests y MainIntegrationTests. Informe local: `Library/WeatherMenu-final-tests.xml`.

Las pruebas WeatherMenuTests verifican compilación de shaders, posiciones de carpas, parches sin colisión, humo y fluctuación de luz, desplazamiento del cielo mediante comparación de renderizados, navegación de menús, ausencia de servicios duplicados y restauración del checkpoint. También capturan los menús a 1280×720 y 1280×1024 y comprueban límites de botones. Las capturas de menús fueron inspeccionadas y el contenido del cuaderno ajustado para permanecer sobre las páginas.

Tras la revisión visual se reemplazó el material de las llamas por WeatherFlame y se ajustaron sus partículas para que sean visibles. Se repitieron las cuatro pruebas WeatherMenuTests: **4/4 aprobadas**, informe `Library/WeatherMenu-final-visual-tests.xml`. Se inspeccionó la captura en Play `Library/WeatherMenu-fire-runtime.png`, además del detalle del barro. Una captura auxiliar de Editor mostró artefactos azules que no aparecen en la captura en Play; no se usó esa captura como evidencia del resultado final.

Estas pruebas no sustituyen una revisión de audio con auriculares ni una medición de rendimiento en la computadora de destino. Las capturas de cámara auxiliares no reproducen todos los ajustes de posprocesado de la cámara del jugador.
