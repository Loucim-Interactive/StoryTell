# Induced effects

Effects keep the existing `EInducedEffect -> InmersionHandlerScript -> InducedEffect<TConfig>.EffectRoutine` flow. `PanicBreathing` remains unimplemented.

## Activate

On an `InmersiveVFXObject`, enable **Use Output Event** and select **BlurryVision**, **TunnelVision**, **StepPrinting**, or **MuffledHearing** in **Output Event**. The existing VFX trigger sends `GameplayEvents.VFX`, just as it does for the explosion's ShakeVision. The explosion's current selection is unchanged.

From another script:

```csharp
using InmersionSystem;
using Systems.EventSystem.Scripts;
using Systems.InmersionSystem.Induced;

GameEventBus.Raise(GameplayEvents.VFX, InmersiveElements.EInducedEffect.TunnelVision);
// Or specify strength (0–1):
InmersionHandlerScript.Instance.Trigger(InmersiveElements.EInducedEffect.StepPrinting, 0.8f);
InmersionHandlerScript.Instance.ForceStop(InmersiveElements.EInducedEffect.StepPrinting);
```

Different effects run together. Triggering the same effect restarts it; zero intensity stops it. ForceStop and disabling/destroying the handler restore the affected state. Durations use game time and pause with `Time.timeScale`.

## Tune

The Player prefab references the four assets in `Configs/SOs`. Each has `enterDuration`, `holdDuration`, `exitDuration`, and `maxIntensity`.

- **BlurryVision:** `blurRadius` controls the sampling radius in pixels.
- **TunnelVision:** `clearRadius` controls the clear central area, and `softness` its transition into darkened edges.
- **StepPrinting:** `printRate` controls how often the accumulated image is updated, while `trailPersistence` controls how long previous images bleed through. History belongs to the affected camera and is discarded on stop/restart or a render-target size change.
- **MuffledHearing:** `cutoffFrequency` controls the low-pass filter on the player camera's AudioListener. Existing filter settings are restored after the effect.

The visual effects use the **Induced Vision** renderer feature installed on the PC and Mobile renderers. It targets Unity 6 URP's Render Graph, which is enabled in this project; keep Render Graph enabled. It runs before post-processing on affected game cameras, with no render passes when idle. Screen-space overlay UI is unaffected. The serialized shader reference keeps the shader available in builds.

New player prefabs need the handler config references and a camera with an AudioListener. The handler adds the camera's transient visual state and, if needed, the listener filter at runtime.

## Validation

Run **Tools > Inmersion > Validate Induced Effects** outside Play Mode. It renders an isolated preview scene and checks asset references, intensity, combined effects, blur at a contrasting edge, tunnel center/edges, temporal history, reset behavior, and restoration of existing audio filter settings. It does not alter the level. Actual audio listening and gameplay pacing can be tuned in Play Mode.
