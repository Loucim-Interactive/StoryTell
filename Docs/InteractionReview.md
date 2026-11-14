# Inspección, carta, NPC y combate

Cambios de `MainExperiencePolish`, noviembre de 2026.

## Controles y presentación

- Los objetos inspeccionados admiten zoom real con la rueda, limitado a 0,7–1,6 de aumento y al espacio visible. Al dejarlos recuperan posición, escala y estado físico.
- En documentos, la rueda desplaza la transcripción; esa ayuda aparece solo si el texto excede el panel. No cambia el tamaño del documento.
- La guía central de la carta mide 450 × 586,8 unidades, igual que el papel completo. Las siluetas usan las texturas originales con transparencia y su recorte UV. La pieza seleccionada ilumina su guía; las guías no interceptan el ratón.
- El encaje admite 58 unidades de distancia y 20 grados de diferencia. Una rotación de 90 grados sigue requiriendo girar con R o clic derecho.
- Las narraciones breves se descartan o se interrumpen al abrir una interfaz modal. No se acumulan detrás de la carta.

## Soldados

El soldado antes sin nombre junto a Rubén y Franco es Jeremías, provisionalmente. Tiene conversación propia, una postura de pie y etiqueta propia. Sus acciones heredadas de inspección y pensamiento están desactivadas. Cada interacción y animador usa al Speaker de su propio actor; la interacción resuelve nuevamente su propietario al iniciar para soportar duplicados futuros.

Cinco soldados tienen comentarios ambientales ficticios sobre frío, descanso, comida y familia. Son subtítulos provisionales, sin doblaje nuevo ni atribución a testimonios históricos. Se muestran a menos de cinco metros, sin atravesar obstáculos; un solo comentario a la vez, cinco segundos de lectura, al menos 22 segundos entre comentarios y 85 por soldado. Ceden a conversaciones, pensamientos y paneles. Disminuyen con el avance y no se disparan durante el combate.

## Miradas del recorrido

Dos invitaciones: puesto avanzado y refugio dañado existente. El enfoque automático ocurre una vez por punto, cuando el jugador se detiene y ya está mirando aproximadamente hacia el tema. Dura como máximo 1,8 segundos. Mover el ratón, salir de la zona, pulsar F o abrir un panel lo cancela. No bloquea caminar ni cambia el campo de visión. F permite volver a mirar voluntariamente. Respeta otros propietarios de la cámara.

La zona antigua imponía una mirada repetible de cinco segundos y competía con la nueva. Se conserva su objeto y se desactiva esa activación; las miradas del recorrido quedan bajo el sistema breve y cancelable.

## Combate

- Se conserva la cantidad nativa de main y su límite de enemigos simultáneos.
- Las apariciones se mantienen por delante del avance hacia el frente, independientemente de que el jugador mire de costado. Si el jugador avanzó más allá del área original, se busca una entrada más adelante y se exige un recorrido NavMesh completo conectado a la entrada de combate.
- Las sombras no desaparecen por tiempo. Las oleadas pendientes no cuentan como bajas.
- Se rehace la navegación del frente con los colliders físicos reales. Los detectores de explosiones ya se guardan como triggers, en vez de convertirse recién en ejecución: antes dejaban la entrada aislada del resto del frente en el horneado.
- Solo el contacto físico real con la cápsula del jugador o la eliminación de todas las sombras habilitan el cierre. Se ignoran intentos de cierre por recorrido mientras falten enemigos.
- El disparador antiguo de salida queda desactivado, incluida su activación al entrar.

## Verificación

Unity 6000.4.6f1, URP, Windows / Direct3D 11. Pruebas específicas de interacción, zoom, encaje, comentario ambiental, cámara, oleadas, contacto y progreso de misiones. Capturas de UI y del soldado en `Library/InteractionReview-*`.

14 casos distintos aprobados, tomando el resultado más reciente de cada caso en `InteractionReview-verified.xml`, `InteractionReview-final.xml` y `InteractionReview-camera-verified.xml`. Los fallos iniciales de postura/etiqueta, contacto, navegación y enfoque antiguo se corrigieron y se volvieron a comprobar. La conectividad del frente se inspecciona además en `InteractionReview-navigation.txt`.

Esto no sustituye una recorrida manual completa ni una medición de rendimiento en el equipo de destino.
