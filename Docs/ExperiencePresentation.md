# Presentación e interacción de Level1

Fecha: 2026-11-23. Proyecto: `C:/Users/Admin/Documents/GitHub/StoryTell`, rama `MainExperiencePolish`.

## Cambios

- El soldado de la carta se llama **Marcos**, según la decisión del usuario y el texto de los diseñadores. Se conserva su valor de enumeración 12 y su identidad de guardado. Los identificadores técnicos y carpetas existentes llamados Mateo se mantienen por compatibilidad.
- Hablar muestra un pictograma de ratón blanco, más grande, y la acción. Se elimina el texto redundante «Clic izq.»; los controles de teclado y el respaldo cuando no hay icono permanecen.
- El mapa usa una cámara ortográfica aislada en una textura de interfaz, con proporción 3:2. La cámara ya no se apila sobre la del jugador. Los indicadores se proyectan sobre el rectángulo real del papel y mantienen su posición al moverse o girar.
- Se regeneró la cartografía a partir del terreno real. La imagen anterior muestreaba fuera del terreno y repetía una franja de alturas; la nueva cubre X 0–300, Z 195–645. **El norte apunta hacia la derecha**, indicado en papel y leyenda. Se conservan ocho lugares, jugador y destino actual.
- Después de entregar la carta, L abre únicamente la transcripción centrada. E, Escape o L cierran; la rueda permite desplazar el texto si no cabe. Antes de entregarla sigue disponible la inspección física. El final del combate cierra también esta ventana.
- La barra blanca inicial era el tiempo restante de una respuesta de radio. Ahora solo se muestra con la radio equipada, con relleno ámbar y una etiqueta explicativa. No se altera el plazo de las respuestas.
- Marcos y Simón se miran; Néstor se integra al grupo y el cocinero mira la mesa. Se conserva la ubicación de la mesa elegida por el usuario. Sus posiciones se ajustan a los pies visibles y las frases ambientales activan gestos de conversación.
- Se amplió la continuidad visual del terreno exterior mediante una malla costera y se ajustó su mezcla con el horizonte. No agrega suelo explorable ni modifica límites o navegación. Se añadieron tres manchas discretas de suelo removido en el frente; no sustituyen cráteres modelados.

## Verificación

Unity 6000.4.6f1, Windows, ejecución automatizada de pruebas EditMode que entran en Play para las comprobaciones de comportamiento. Árbol de trabajo con cambios previos y actuales, sin commit en esta revisión.

- `Library/ExperiencePresentation-final-results.xml`: 11/11, sin fallos. Incluye interfaz, lectura, identidad de Marcos, radio, agrupación, continuidad del terreno, mapa al moverse y en dos proporciones, secuencia de misiones y apoyo de los 17 NPC en el suelo.
- `Library/ExperiencePresentation-ending-results.xml`: 1/1, cierre de la lectura de carta al finalizar el combate.
- `Library/ExperiencePresentation-coastal-results.xml`: 1/1, repetición dirigida de la agrupación y capturas de costa/cocina/frente después del ajuste del material costero.
- Capturas en `Library/ExperiencePresentation-*.png`. Las capturas de interfaz pueden usar el respaldo de renderizado de UI de las pruebas y no acreditan una partida manual completa.

No se midió rendimiento en las computadoras de los compañeros ni se realizó un recorrido manual completo. La continuidad geométrica del borde se comprueba. La captura costera todavía muestra una ladera regular y repetición del material; no se da por resuelto su acabado visual. Es necesario modelar mejor esa transición y comprobarla desde el recorrido dentro de una partida. La ambientación actual todavía usa modelos y materiales provisionales.

## Recursos de diseño más útiles

| Zona | Modelos prioritarios |
| --- | --- |
| Cocina/comedor | Cocina de campaña, ollas, cucharón, jarros y platos metálicos, bidones de agua y bancos. |
| Comunicaciones | Radio y teléfono de campaña con referencias de época, auriculares, baterías, bobinas de cable y libretas. |
| Campamento | Variantes de carpa y lona, sogas y estacas, mantas, bolsos y cajas de suministros. |
| Frente | Tramos de pozo/refugio, cráteres reales, bolsas de arena dañadas, lonas rotas y tablas quebradas. |
| Terreno | Pastos bajos y matas, rocas de cuarcita, materiales de turba húmeda/barro con normales y rugosidad coherentes. |

Animaciones útiles: comer, sentarse, calentarse las manos, operar la radio, escribir y variaciones de gestos de conversación. Evitar elegir modelos de radios o uniformes específicos sin comprobar la referencia de 1982.

Referencias para el equipo: [geología y paisaje de las islas, Falkland Islands Government](https://www.gov.fk/mineralresources/geology/regional-geology/onshore-geology/) y [fotografías y contexto de 1982, National Army Museum](https://www.nam.ac.uk/explore/british-army-and-falklands-war).

## Respaldo y entrega

Los respaldos de esta revisión están en `Library/ExperiencePresentation-backup/`. Tras finalizar las pruebas se restauran las fuentes dinámicas desde ese respaldo, respetando cambios anteriores, y los checkpoints originales de la cuenta local.

No se generó un ejecutable nuevo ni se hizo commit/push. El ZIP compartido `Builds/StoryTell-Prueba-2026-11-22-fd5f281.zip` corresponde a una versión anterior y no contiene esta revisión.
