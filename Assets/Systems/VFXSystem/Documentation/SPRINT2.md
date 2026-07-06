# VFX System - Sprint 2

El sistema existente de pooling se amplia sin modificar su funcionamiento.

## Nuevos componentes

- `VFXEmitter`: dispara un tipo y variante desde escena, trigger o UnityEvent.
- `BreathEffect`: reproduce aliento visible a una frecuencia configurable.

## Setup

1. Registrar `VFXConfig` para explosion, humo, chispas y fuego.
2. Agregar `VFXEmitter` al objeto donde debe aparecer el efecto.
3. Elegir tipo, variante, escala y punto de aparicion.
4. Llamar `Play()` directamente o desde `NarrativeEffectTrigger`.
5. Para aliento, colocar un ParticleSystem no-loop como hijo de la camara y
   agregar `BreathEffect`.

Ambos componentes funcionan sin Perception, Audio ni Music.
