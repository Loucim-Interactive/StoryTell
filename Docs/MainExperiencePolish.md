# MainExperiencePolish

Base: MainGameplayIntegration (6d366c9). Revisión conjunta de recorrido y ambientación solicitada el 29/10/2026. Mantiene las misiones, decisiones, UI y combate nativos de main. No importa el recorrido de GameplayPath ni sus 45 enemigos.

## Cambios de juego

- Inicio en el campamento (aproximadamente X85, Z279), sobre el terreno.
- El modelo corporal conserva su sombra, pero no se dibuja por dentro en primera persona. No se desactiva el rifle, la radio ni el papel.
- Mapa más legible: papel frontal de 58 × 38,7 cm a 62 cm de la vista, sin viento, deformación ni retraso físico. Se conserva M y el seguimiento del jugador. La altura del marcador elige la cara orientada hacia el lector; punto de 13 mm.
- Colisiones estáticas para 95 mallas existentes de rocas y carpas. Las carpas usan una copia de su geometría con triángulos en ambas orientaciones; se conservan las aberturas del modelo. Los prefabs originales permanecen intactos.
- Área de búsqueda amplia en el campamento y un recorrido acotado que pasa por las zonas originales hacia el frente. Un perímetro estático invisible y una comprobación de posición impiden abandonar la franja. Se muestran pensamientos espaciados al chocar con el límite.
- Barrera antes del tramo del frente: requiere recoger el equipo. Si se aceptó la misión de Mateo, también requiere devolver su carta; se conserva la posibilidad nativa de no aceptar esa misión. La barrera se retira al cumplir esas condiciones.
- El disparador de combate original se alinea con el camino cerca de X56, Z529. Se conservan sus eventos y el combate de main.
- Las entradas de Narration zone 5 y del corredor de explosiones se validan en orden. Intentar adelantarse por un costado devuelve al último punto permitido. Sacos de arena marcan los accesos sin añadir carteles.
- Dos refugios adicionales en la zona de búsqueda. Los cuatro fragmentos están apoyados en cajones, dentro del campamento. Simón y José se reubican junto a esas zonas y dan una pista del primer fragmento pendiente al hablarles durante la misión. Sus conversaciones originales vuelven a estar disponibles al completar la carta.
- El mapa y los tres objetos de equipamiento se apoyan sobre la mesa existente. Se ajusta el apoyo de los dos soldados reubicados.

## Ambientación

Fogatas pequeñas distinguen el campamento, los refugios, el puesto avanzado y una parada de suministros. Se usan luces cálidas de corto alcance sin sombras adicionales y partículas suaves. El camino incluye una pequeña reserva de suministros; no se añade otra misión obligatoria.

Luz diurna fría con sombras suaves, ambiente de tres colores, niebla exponencial y cielo nublado procedural. Una lámina de agua distante, bajo la cota mínima del terreno, completa el horizonte; es decorativa y no tiene colisión ni sistema de natación. Una extensión visual de los bordes desciende hacia el mar para cubrir el corte rectangular. La niebla y los límites evitan acercarse a esos bordes. No se esculpe ni se sustituye el TerrainData original.

El volumen de color tiene prioridad baja para conservar los efectos narrativos de main. Se reutiliza el viento disponible a volumen bajo. El rifle conserva su disparo provisional y añade un destello breve, luz de boca, desplazamiento de retroceso, movimiento de recarga y un clic mecánico provisional al comenzar y terminar la recarga. No se añadieron brazos ni animaciones de manos: todavía requieren un recurso con esqueleto y clips.

## Verificación y límites

LevelPolishTests comprueba colisiones, objetos de misión dentro del recorrido, inicio en campamento, shaders, mapa estable, marcador delante del papel, cuerpo oculto y sombra conservada, pistas que cambian al recoger fragmentos y rechazo de escapes. La búsqueda de transitabilidad usa una cápsula y muestras de pendiente para comprobar una ruta desde el inicio hasta la entrada de combate, con la barrera de misión abierta.

MainIntegrationTests conserva las comprobaciones de UI, inspección, puzzle, misiones, mapa, NPC y arma. Su comprobación de ubicaciones excluye los objetos reubicados explícitamente en esta revisión.

Resultado: 13 comprobaciones aprobadas en Library/LevelPolish-release-tests.xml; la comprobación restante del destello detectó una partícula que sobrevivía al ocultar el rifle. Se corrigió su limpieza desde el componente del jugador y se volvió a verificar el caso con la partícula desactivada: 1/1 aprobada en Library/LevelPolish-flash-final-tests.xml. No quedan fallos conocidos de estas 14 comprobaciones. Los registros finales no contienen errores de compilación ni excepciones de ejecución.

Las capturas de cámara en Library/LevelPolish-*.png permiten revisar mundo, cielo y papel; la UI superpuesta debe revisarse también en Game View. La búsqueda de ruta es una comprobación aproximada de espacio y pendientes, no sustituye caminar todo el trayecto. Quedan pendientes una partida humana completa, evaluación de sonido y medición de FPS en el equipo objetivo.

Editor utilizado: Unity 6000.4.6f1. El proyecto declara 6000.0.29f1; las conversiones automáticas de paquetes, fuentes y configuración se excluyen de la entrega. Respaldo de escena: Library/LevelPolishBackups.
