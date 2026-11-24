# Olla del comedor con interacción localizada

24 de noviembre de 2026 · StoryTell · MainExperiencePolish.

El archivo proporcionado `Olla_Terminada.fbx` contiene una malla única de olla y mango, sin mesa. Se importó completo y se colocó sobre la mesa actual de cocina. No se dividió ni se alteró su geometría. El diámetro del recipiente se ajustó a aproximadamente 65 cm, con el mango orientado sobre el eje largo de la mesa y el fondo apoyado en ella.

Se reemplazó el aspecto de la ración provisional por una superficie de comida dentro de la olla. Es un contenido visual provisional, independiente de la malla de los diseñadores.

## Interacción

- La olla completa tiene una colisión de malla sólida, en la capa normal del entorno.
- La acción «Comer» utiliza un pequeño collider de interacción dentro del recipiente. El mango, el borde y la mesa quedan fuera de la acción.
- El modelo completo no se recoge ni entra en inspección al comer.
- Se conserva el componente CampMeal original y su enlace con CampRoutine, mapa, objetivos y guardados. Su renderer y collider provisionales anteriores quedan desactivados.
- E ejecuta la acción existente. No permite saltarse la indicación de Diego, y comer una segunda vez no completa la tarea otra vez.
- La olla y la interacción están vinculadas a la mesa y la acompañan si se mueve.

## Verificación

`Library/KitchenPot-results.xml` contiene la prueba correcta del recorrido equipo → comida → carta → entrega. La primera comprobación específica de la olla se interrumpió por el contexto de variables capturadas al cambiar de EditMode a Play; se corrigió la preparación del test, sin cambios adicionales de gameplay.

`Library/KitchenPot-final-results.xml` verifica el modelo completo y su escala; colisión; interacción desde varios lados; ausencia de acción al apuntar a mango, borde y mesa; condición previa de Diego y una sola finalización de la comida.

Después del ajuste de tamaño del contenido, se repite la comprobación en `Library/KitchenPot-verified-results.xml`. Captura: `Library/KitchenPot-in-game.png`.

Recursos: `Assets/Art/Props/Kitchen/Olla_Terminada.fbx` y `Assets/Systems/MainIntegration/Data/KitchenPot/`.

Respaldo previo: `Library/KitchenPot-backup/`. Al terminar las pruebas se restauran fuentes y checkpoints anteriores.

No se hizo commit/push ni se regeneró el ZIP anterior: el build del 23 de noviembre no incluye esta olla.
