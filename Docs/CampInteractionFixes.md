# Correcciones de comedor, inspección y borde del escenario

Proyecto StoryTell, rama `MainExperiencePolish`. Fecha: 2026-11-23.

## Cambios

- La invitación F del comedor apuntaba a un ancla creada antes de mover la cocina. Ahora apunta al centro visible de la mesa actual, con el destino como hijo de esa mesa y la entrada situada en su aproximación desde el campamento. Si se vuelve a mover la mesa, el destino sigue unido a ella; la zona de activación debe acompañarse si se desplaza el comedor a otro sector.
- Los 33 objetos inspeccionables del escenario usan E. Esto incluye el FAL decorativo y los objetos de las mesas. Los objetos de equipamiento, mapa y munición que ya usaban E conservan ese control. Los NPC mantienen el clic para hablar y las perillas de radio mantienen su interacción específica.
- La inspección centra los modelos por sus límites visibles, compensando el origen desplazado de modelos como el FAL. La compensación se actualiza al girar y hacer zoom. Los objetos corrientes tienen un tamaño inicial mayor; el zoom sigue siendo real. Al cerrar se restablecen padre, posición, rotación y escala originales, y se mantiene el comportamiento de recoger equipo al finalizar su inspección.
- El punto central corresponde al Image `Dot` del canvas nativo de interacción. La revisión del build del 23 de noviembre lo ocultó y restauró las decoraciones `Cross` de las respuestas, que se habían confundido con la mirilla. Se comprobó la ausencia del punto dentro del ejecutable.
- El borde ampliado usa 1024 segmentos por lado, correspondientes a la resolución de alturas del terreno original. Se conserva la altura del terreno en cada vértice de unión.
- La escala horizontal de la textura original era negativa, `(-1500, 1500)`. El generador anterior la limitaba a 1, haciendo que una imagen de 1500 metros se repitiera cada metro y creando rayas. Ahora se conserva el signo y la escala originales en ambos ejes.
- Se agregaron 1528 matas de los dos prefabs optimizados de pasto ya disponibles, en la ampliación occidental próxima al recorrido. Se apoyan mediante consulta de la malla costera y se combinan en dos mallas, con materiales locales de tono frío y viento del shader existente. La vegetación se dispersa y disminuye hacia afuera; no se cubre toda la ampliación distante.
- Se conservan las alturas, vegetación, límites y navegación originales del terreno jugable. El exterior sigue siendo una continuación visual, sin nuevos accesos al frente.

## Verificación

Los resultados se guardan en `Library/CampInteractionFixes-*-results.xml`, y las capturas en `Library/CampInteractionFixes-*.png`. Las pruebas de esta revisión verifican ubicación del enfoque, controles consistentes, retiro del punto central, inspección de cuatro objetos en 16:9 y 4:3, restitución de objetos, resolución/UV de la costa y apoyo de la vegetación. Se comprueba además el recorrido lineal y las funciones de inspección/documentos relacionadas.

La primera ejecución tuvo 2/3 pruebas correctas: la prueba del equipo buscaba objetos antes de activar su misión, cuando el juego los oculta. Se corrigió la preparación de esa prueba para iniciar la misión antes de inspeccionar. La ejecución final pasó 4/4 pruebas, incluido el recorrido lineal completo de misiones.

La comprobación de lectura de carta entregada también pasó. Una prueba antigua de zoom tenía otra preparación incompleta: intentaba hablar con un NPC durante la llamada inicial de radio. Se ajustó para esperar y resolver esa llamada antes de comprobar conversación e inspección; la repetición pasó 1/1 y su resultado se guarda en `Library/CampInteractionFixes-zoom-results.xml`. En total, seis comprobaciones distintas de esta revisión terminaron correctamente.

No equivale a una partida manual completa ni a una medición de rendimiento en las computadoras de los compañeros. No se generó un build ni se hizo commit/push.

## Respaldo

`Library/CampInteractionFixes-backup/` contiene la escena, el sistema de inspección, las fuentes y los checkpoints anteriores a esta revisión. Los checkpoints y fuentes se restauran al terminar las pruebas, conservando el estado anterior de la cuenta y los cambios que ya existían.
