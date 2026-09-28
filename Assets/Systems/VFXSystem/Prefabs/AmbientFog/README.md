# Ambient Fog Area

Drag AmbientFogArea.prefab into the scene and place its origin at ground level.
The default emission box is 18 x 3 x 18 metres, centred 1.5 metres above the origin.
Mist extends softly beyond the box because the individual billboards are 5–8 metres wide.

Tuning in the Particle System inspector:
- Area: Shape > Scale, or scale the whole prefab. Shape > Position controls the centre.
- Strength and tint: Main > Start Color. Default alpha is 0.07; try 0.03 for lighter mist.
- Density: Emission > Rate over Time (default 3). About 57 particles are alive at steady state, capped at 72.
- Movement: Velocity over Lifetime > X (default 0.04–0.12 metres per second).
- For a larger area with the same particle size, change Shape > Scale and adjust emission as needed.

Loops and prewarms automatically. Select it and use the Scene view particle playback controls to preview.
Uses URP Particles/Unlit, the existing SmokeLoop02 flipbook, gradual lifetime fades,
soft intersections (2 metres), and camera fading (0.5–3 metres).
Depth Texture must be enabled on the active URP asset/camera for soft intersections;
the project's PC URP asset already enables it.

This is local particle mist, not a global fog setting or a volumetric lighting effect.
Its unlit tint stays stable under sunset lighting; use a darker Start Color for night scenes.
No manager, scripts, colliders, or scene edits are required. Duplicate for separate areas.
