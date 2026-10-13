# StoryTell Weapon System

## Estado de integración

El sistema fue trasladado desde Tell-Tale y adaptado al namespace `StoryTell.Weapons`.
Actualmente está aislado: no modifica `Level1`, el prefab `Player`, el FAL existente ni los
assets de Input System. La conexión con escenas se realizará en una etapa posterior.

## Propósito

Proporciona secuencias narrativas de arma en primera persona. El arma permanece enfundada
y sin entrada durante la exploración normal; una `WeaponSequenceController` decide cuándo
se habilita y cuándo termina la secuencia. No utiliza un contador numérico de munición.

## Reglas del jugador

- La capacidad del cargador se comunica antes de la secuencia y el jugador cuenta los disparos.
- Cada cargador de reserva conserva su propia cantidad de balas.
- Recargar descarta el cargador instalado y las balas que todavía contenga.
- Los cargadores se recogen hasta alcanzar un límite configurable.
- `R` recarga o comienza a resolver un encasquillamiento.
- Solo los objetos con `WeaponDamageReceiver` reciben daño.

## Componentes principales

- `WeaponDefinition`: daño, alcance, dispersión, capacidad y tiempos.
- `WeaponController`: equipar, apuntar, disparar, recargar, encasquillar y hitscan.
- `MagazineInventory`: colección ordenada de cargadores individuales.
- `WeaponCondition`: estado limpio, agua, arena o barro y sus intervalos de fallo.
- `WeaponInput`: ratón y teclado compatibles con ambos backends de entrada de Unity.
- `MagazinePickup`: recogida de cargadores completos o parciales.
- `WeaponDamageReceiver`: vida opcional para objetivos de una secuencia armada.
- `WeaponSequenceController`: entrada y salida controlable mediante eventos o Timeline.
- `WeaponContaminationZone`: aplica agua, arena o barro desde volúmenes de escena.

## Modelo de encasquillamiento

El próximo intervalo se calcula cuando cambia la condición o se produce un atasco:

- Limpia: 25-40 intentos de disparo.
- Agua: 15-25.
- Arena: 8-15.
- Barro: 4-10.

`WeaponSequenceController.ForceNextShotToJam()` permite controlar el fallo desde la
narrativa. El atasco no consume una bala. `R` inicia el desatasco y, si no hay un evento
de animación, el temporizador configurado lo completa.

## Integración futura mínima

1. Asignar una `WeaponDefinition` al `WeaponController`.
2. Añadir `MagazineInventory`, `WeaponCondition`, `WeaponController` y `WeaponInput` al rig.
3. Asignar la cámara, el modelo y las poses de cadera/apuntado.
4. Conectar una `WeaponSequenceController` al evento narrativo correspondiente.
5. Añadir `WeaponDamageReceiver` únicamente a objetivos válidos.
6. Añadir recogidas y zonas de contaminación donde lo requiera la escena.

Los modelos, animaciones, sonidos y VFX son opcionales. La lógica funciona sin contenido
visual definitivo y mantiene temporizadores de respaldo para recarga y desatasco.

## Pruebas incluidas

Las pruebas de Editor cubren:

- Orden y balas individuales de los cargadores.
- Pérdida de las balas restantes al recargar.
- Intervalos de atasco y contaminación por barro.
- Atasco forzado sin consumo de munición y posterior desatasco.
- Daño permitido únicamente en receptores habilitados.
