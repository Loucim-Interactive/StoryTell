# Presentación sencilla y carta original

- Se desactiva el texto flotante «PRUEBA DE COMBATE» y se quita su referencia de la presentación de Level1 para que no reaparezca al cambiar el estado del arma.
- Entrar directamente a Level1 con una partida guardada restaura el checkpoint sin abrir los ajustes. Nueva partida y Continuar desde los menús conservan sus funciones. La configuración se abre voluntariamente con Esc.
- El guardado usa un aviso separado de 280 × 34 unidades de referencia en la esquina inferior derecha, tipografía InterTight de 20 unidades y duración de dos segundos. Se agrupan avisos de guardado cercanos; se oculta durante vistas modales. Los errores de guardado siguen siendo visibles.
- El HUD conserva un objetivo actual con su progreso. Se oculta la lista duplicada y se eliminan los anuncios automáticos de nueva misión y los recordatorios repetidos al recoger equipo o completar los fragmentos. Se mantienen los avisos de recogida y la confirmación de entrega. El mapa y la reconstrucción tienen instrucciones más breves, sin anticipar eventos.
- Junto a los dos refugios hay marcas de suelo quemado con bordes irregulares y barriles volcados con colisión. Las marcas son visuales, siguen la superficie y no excavan ni modifican TerrainData. Las ubicaciones quedan fuera del paso central.

## Arte de la reconstrucción

Los FBX Carta3parte1–4 usan planos de la hoja completa, pero **los recortes reales están en los canales alfa de sus PNG**. Se utilizan esas cuatro texturas originales, sus siluetas, escritura y proporciones, con posiciones de encaje calculadas a partir del área opaca. No se agregan otros cortes ni texto artificial encima.

Las mallas auxiliares de UI solo delimitan la parte frontal de la textura. Cuatro máscaras pequeñas de 64 × 64 sirven para que las zonas transparentes no capturen el ratón. Las fuentes originales en `Assets/Art/Maximo/Interactables/Cartas/CartaLanus` no se modifican. Se mantienen arrastre, rotación, progreso parcial, checkpoint e inspección tridimensional.

## Verificación

33/33 pruebas aprobadas en `Library/QuietPresentation-tests.xml`. Después de revisar la transparencia original y corregir los recortes se repitieron cuatro pruebas: 4/4 aprobadas en `Library/QuietPresentation-final-tests.xml`. Incluyen entrada sin pausa, aviso discreto, uso de los PNG originales, cobertura de la silueta completa, máscaras de selección legibles, arrastre real y restauración del puzzle desde checkpoint.

Se inspeccionaron las capturas de la carta dispersa, reconstruida a 1280×720 y 1280×1024, del aviso de guardado y de las marcas de impacto. Archivos locales: `Library/QuietPresentation-*.png`.

Respaldo de la escena anterior: `Library/QuietPresentation-inputs/Level1.unity`.
