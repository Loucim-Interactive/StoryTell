# Campamento ampliado y encargo de repuestos

Recorrido acordado: Diego → equipo → volver con Diego → cocina/comer → Mateo → fragmentos → reconstruir y leer → devolver la carta → sargento Ibarra → Rubén en el puesto avanzado → guardar los repuestos → Benítez en el puesto de radio → frente → combate.

## Campamento y personajes

Se agregan tres carpas alrededor del campamento principal, conservando los materiales fríos y las colisiones de la carpa existente. Se retira pasto solamente de las huellas de los elementos agregados o trasladados. La cocina pasa al espacio central entre las carpas; Mateo queda cerca del cocinero.

Los cuatro personajes nuevos son ficticios y provisionales:

- Sargento Ibarra: da el encargo después de devolver la carta.
- Néstor: compañero que revisa las carpas.
- Andrés: compañero junto a los suministros.
- Benítez: operador del puesto de radio cercano al frente, antes de las explosiones.

Hay quince soldados registrados para recordar sus nombres con N. Cada uno dispone de cinco comentarios ambientales: 75 frases sobre su actividad, el frío, el descanso, la comida y la familia. Son subtítulos ficticios; no se presentan como testimonios históricos ni tienen doblaje nuevo. Se conserva una sola frase a la vez, el intervalo general y la prioridad de los diálogos y paneles.

## Progresión

`SupplyDispatch` controla una secuencia sin saltos: sargento → puesto → recoger → llevar → entregar. Hablar antes de tiempo muestra una respuesta breve y no avanza la misión. El paquete solamente aparece para recogerlo después del encargo de Rubén.

Los repuestos se guardan en la mochila como estado del inventario de la misión. No se muestra un objeto en las manos, no se inicia inspección y no se suman balas. Se requiere terminar la conversación de entrega con Benítez para habilitar el combate. El mapa y el objetivo muestran únicamente el paso actual.

La conversación de devolución de la carta conserva la acción nativa de entrega, pero ahora habla de la carta ya reconstruida. Mateo indica ir al sargento en el agradecimiento de esa misma conversación.

Los guardados conservan el estado del encargo y los nuevos nombres conocidos. Una partida antigua conserva equipo y carta, y vuelve al campamento para continuar la secuencia nueva desde el sargento cuando corresponde. No se borra la partida del usuario.

## Cámara y combate

Hay cinco puntos de enfoque en total: puesto avanzado y refugio dañado, más cocina, carpa de mando y puesto de radio. Se mantiene la mirada automática breve al detenerse, cancelable con el ratón, y F para mirar voluntariamente.

Las sombras se introducen a un mínimo de 28 metros horizontales. El área inicial y las entradas adaptativas se sitúan aproximadamente a 30–40 metros del jugador y se amplía la navegación para las siguientes oleadas. Se conservan la cantidad de enemigos y los obstáculos físicos existentes. El cierre sigue requiriendo contacto o eliminar a todos los enemigos.

## Comprobación

Para recorrer la secuencia desde Diego, usar **Nueva partida**. Para comprobar una partida anterior, usar **Continuar** y seguir el objetivo del sargento o la tarea pendiente.

Las pruebas cubren orden de conversaciones, bloqueo de combate, recogida sin objeto en las manos, entrega, guardado y migración, carpas y fragmentos, registro de soldados, distancia de las sombras, apoyos sobre el suelo, mapa y accesos. Las capturas se guardan en `Library/CampExpansion-*`.

Se aprobaron 14 casos distintos en Unity 6000.4.6f1 / Windows / URP / Direct3D 11. Resultados: `Library/CampExpansion-results.xml` y `Library/CampExpansion-placement-verified.xml`, tomando el resultado más reciente de cada caso. Se revisaron las capturas del campamento, la cocina, el puesto de radio, la leyenda del mapa y la mesa de repuestos. La mesa se separó de la fogata después de la revisión visual; su acceso y separación del fuego se comprobaron de nuevo. El apoyo de los quince soldados y los destinos del mapa también se comprobaron en ejecución.

Quedan pendientes una recorrida manual completa, evaluación de ritmo con jugadores y medición de rendimiento en el equipo de destino. Los modelos, animaciones y audios existentes siguen siendo provisionales.
