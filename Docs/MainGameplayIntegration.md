# MainGameplayIntegration

Base: `origin/main`, commit `333a8f2`. La integración amplía sus sistemas y conserva el recorrido de esa rama: Diego y el equipo, Mateo y su carta, los personajes y zonas originales, y el combate temporal del main. El recorrido alternativo de cocina/caja/45 enemigos de GameplayPath corresponde a otra rama.

## UI y lectura

Se conservan los objetos, posiciones, fuentes, colores y plantillas de la UI de main. La checklist sigue usando MissionsUIManager; los encabezados nuevos reutilizan su bandera naranja y su fuente. Los paneles adicionales tienen un espacio de referencia adaptable para funcionar a distintas resoluciones sin modificar el Canvas ni los prefabs originales.

Al inspeccionar Carta2, los cuatro fragmentos de Mateo o Diario1, se muestra una transcripción a la derecha. T muestra/oculta el texto y E termina la inspección. El objeto conserva la rotación con clic izquierdo/ratón y el desplazamiento con WASD. La rueda desplaza el texto cuando es largo. Se conserva el objeto físico para observar su material y detalles. La recogida de fragmentos y equipo se resuelve después de restaurar el objeto al terminar la inspección, usando IInspectionTarget y los managers originales.

Los textos se editan en InspectableDocument de cada instancia. Se basan en los recursos gráficos existentes. Los fragmentos distribuyen el contenido de la carta por párrafos; esta distribución es orientativa respecto de los cortes de la malla. Un pasaje tachado y el final recortado de Carta2 se identifican como tales. Diario1 incluye los titulares visibles, no las columnas completas.

Detalle editorial previo del recurso: la imagen de la carta dice «Querido Marcos» y la misión de main pertenece a Mateo. Se conserva lo escrito en el recurso y el nombre del personaje de main; corresponde unificarlo con los autores de las cartas.

## Carta y mapa

L permite reconstruir la carta cuando se recogieron las cuatro partes. Se arrastran con clic izquierdo y giran con R o clic derecho. E/Esc cierran y conservan las piezas ya encajadas durante la sesión. El armado es opcional: main sigue permitiendo entregar los cuatro fragmentos directamente. Después de armarla, L permite consultar la carta completa. No se añade guardado entre sesiones.

El mapa opcional está junto al equipamiento, en `MainGameplay_AdditionalFeatures/Mapa_Consulta_Main`, alrededor de X:90.1, Y:29.18, Z:260.6. Se recoge con E y se abre/guarda con M. Mantiene tamaño físico compacto, posición del jugador y cuatro referencias: equipamiento/Diego, Mateo, puesto avanzado y frente. No aumenta los tres objetos obligatorios de la misión de main.

Se usa una capa nueva libre, MainPhysicalMap, para separar la vista del papel. La capa Equipment existente (9) no se reutiliza ni se renombra. El mapa y la inspección ocultan temporalmente la checklist y el rifle. Al cerrarlos se respeta el estado previo de los controles y la visibilidad solicitada por las misiones.

## Personajes y arma

Los personajes miran al jugador al hablar y recuperan su orientación después de finalizar o cancelar la conversación. Sus interacciones de conversación no entran en inspección de objetos. Se mantienen las conversaciones, decisiones y UnityEvents propios de main.

Se conserva PlayerWeaponController, su FAL y el evento OnBattleTriggered original. Se añaden cargador de 20 tiros, 60 de reserva inicial, recarga con R, apuntado con clic derecho y zoom coordinado con el ZoomScript nativo. El disparo usa hitscan: las coberturas bloquean la trayectoria y solo las siluetas con ShadowDamageTarget se disuelven al recibir un impacto. El retroceso de main conserva su canal; la pequeña sacudida del arma se suma por separado a los efectos de bombas.

La oscuridad periférica de ráfaga se recupera al dejar de disparar y no finaliza la escena. Se mantiene el combate de 12 siluetas temporales de main, su zona y sus VFX. Las instrucciones de prueba se ocultan al usar mapa/lectura o al equipar el rifle. Se utiliza el sonido provisional del FAL. El HUD de munición usa las fuentes y el encabezado de main.

También se corrige el acceso a ChestLid cuando un cofre decorativo tiene canOpen desactivado. El raycast de interacción respeta obstáculos y el cierre con E evita volver a abrir el objeto durante el mismo fotograma.

## Verificación y alcance

MainIntegrationTests comprueba estilos/anchors de la UI previa y posiciones de actores/objetos, referencias nativas, inspección y recogida diferida, cierre real con E mediante Input System, puzzle con arrastre real de EventSystem, reanudación, mapa/marcador y visibilidad de checklist/rifle, recarga y apuntado, giro/retorno de NPC, cofre sin tapa y disparos bloqueados por coberturas.

Las nueve pruebas pasaron en `Library/MainIntegration-final-tests.xml`. Las tres comprobaciones afectadas por el ajuste final de títulos/captura y marcador también pasaron en `Library/MainIntegration-polish-tests.xml`. Las capturas `Library/MainIntegration-*.png` muestran la pasada de UI por separado; no certifican la composición completa del mundo con la UI en Game View. Se revisan a 1280×720 y 1280×1024.

La versión del editor utilizada para verificar es Unity 6000.4.6f1; main declara 6000.0.29f1. Se conservan los archivos de versión, paquetes, materiales y configuración gráfica de main, evitando incluir conversiones automáticas del editor. No se importan los cambios de GraphicsAtmosphere. El único ajuste global necesario es la capa adicional del mapa.

Se conserva el punto de inicio actual de main, situado junto a la prueba de combate, y todas las ubicaciones originales. Para una versión de recorrido completo conviene decidir si ese punto de prueba debe volver al campamento. La duración, los FPS, la escucha y una partida humana completa siguen pendientes de evaluación.

El respaldo anterior a aplicar la integración está en Library/MainIntegrationBackups. MainUIBaseline.json conserva el inventario original de UI y posiciones usado por las pruebas.
