# Revisión del campamento y cierre de Level1

Esta revisión reemplaza el orden anterior por el pedido del jugador: hablar con Diego, recoger casco/radio/FAL, volver con Diego, comer en la cocina, hablar con Mateo, recuperar cuatro fragmentos, reconstruir la carta, abrirla y leer su transcripción, cerrarla y entregarla. El objetivo actual y el mapa acompañan ese orden sin adelantar el combate. La reconstrucción y la lectura ahora son requisitos para entregar la carta.

## Interacción y orientación

- Equipo y munición junto a Diego. La munición se incorpora a la reserva con un aviso; las cajas decorativas se inspeccionan y vuelven a su posición.
- Los objetos inspeccionados se ajustan al encuadre. Los cuerpos permanecen estables al devolverlos. Ayudas contextuales indican la tecla real de interacción, giro, movimiento y cierre; los documentos mantienen texto lateral con T.
- La cocina ofrece una interacción sencilla para comer. Máximo y Facundo agregan conversaciones breves; los soldados del campamento y del puesto tienen intervenciones acordes a su ubicación.
- José muestra su nombre correcto. Mantener N muestra los nombres de soldados conocidos hasta 24 metros, si están delante y sin obstáculos. El registro no se pierde cuando cambia su conversación; se conserva en los checkpoints.
- Los marcadores del mapa se actualizan después del movimiento del papel y convierten las coordenadas de pantalla al espacio real de su interfaz. Se añadió la cocina.
- Los fragmentos conservan el arte original y están desplazados hacia bordes de los suministros, con pequeños paquetes como cobertura parcial. Se conservan las pistas de los NPC.
- GameSelect conserva el cuaderno y ajusta sus acciones a la página sin solaparlas.

## Frente

Se conserva el total de 12 sombras de main, con hasta 7 activas y reposición en grupos. Una navegación horneada exclusivamente para el frente utiliza los colliders existentes. Los puntos de entrada están en una parte conectada con la entrada al combate; no se generan enemigos tras obstáculos infranqueables. Las sombras ya no desaparecen por un temporizador. Avanzan con agentes que rodean los obstáculos y se eliminan al recibir un disparo.

Una vez activado el combate no se permite retroceder detrás de su entrada. La escena termina únicamente por contacto de una sombra con el jugador o por eliminar todas. La oscuridad progresa durante 2,2 segundos, seguida de «Finalización escena 1». El disparador antiguo de salida al menú queda desactivado. Abrir el mapa o inspeccionar un documento no impide que se ejecute el cierre. La oscuridad por ráfagas sigue siendo temporal y no completa la escena.

## Aspecto y colocación

- Las cinco fogatas conservan fuego, humo y luz animada. El centro tiene una barrera para impedir atravesarlo o pararse encima. Se retira el pasto que invade sus inmediaciones, cajas y mesas, sin limpiar todo el terreno.
- Dos refugios dañados adicionales reutilizan la lona existente, ajustada a la altura del terreno. Son modelos provisionales, no nuevos modelos finales del equipo de diseño.
- Los materiales conservan sus texturas originales y distinguen mejor tela mate, metal y superficies húmedas.
- El mar usa un shader URP con ondulación de normales animada, Fresnel, reflejo del entorno, brillo solar, profundidad, espuma cerca de intersecciones y niebla. No incluye simulación de olas, reflejos planares ni SSR. Las nubes móviles, manchas de sol y haces ya existentes se conservan y se verifican.

## Verificación

Pruebas de Unity comprueban el orden de misiones, el bloqueo de entregas prematuras, reconstrucción y lectura, persistencia, munición, encuadre, indicadores con movimiento, límites del frente, ambos finales y distribución de GameSelect. Capturas de ejecución permiten revisar equipo, cocina, mar e interfaz. Se amplía la comprobación con las suites anteriores de menús, diálogos, checkpoints y clima.

Las pruebas automatizadas no reemplazan una partida completa de una persona sin conocer el recorrido. Falta medir el ritmo real, la dificultad y el rendimiento en el equipo de destino, además de incorporar audio y animaciones definitivos del equipo de diseño.
