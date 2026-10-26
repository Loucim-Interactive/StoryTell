# Battle trigger

1. Add `PlayerWeaponController` and `PlayerWeaponInput` to the Player object. `WeaponRecoil` is added automatically. Assign its camera controller if it is not on the same object or a parent. Assign the optional FAL visual as a child object, not the Player itself.
2. Add `SpawnArea` to an empty object. Its serialized bounds are local to that object and appear as a cyan gizmo. Bake a NavMesh inside the bounds, or turn off `Sample Nav Mesh` to sample directly in the box. Sampled positions are capsule foot positions.
3. Add `ShadowSpawner`, assign the area and `Materials/BattleShadow.mat`. Set capsule radius/height and the lifetime range (defaults to 3–7 seconds). All capsules share one black URP Unlit material, cast/receive no shadows, and use no probes, rigidbodies, agents or per-capsule Update scripts. If bounded NavMesh sampling cannot find enough positions, it logs the actual count instead of spawning outside the area.
4. Add `BattleManager`, assign the player weapon component and spawner, and set the count.
5. Add `TriggerZone` to a GameObject with a 3D Collider. The script sets `isTrigger`. The entering object or one of its parents must have the `Player` tag. Unity trigger callbacks also require a Rigidbody on one side or the player's CharacterController. For a Rigidbody-based trigger, use a kinematic Rigidbody with gravity disabled.

The zone and manager each default to triggering once. Disable both `Trigger Once` options to allow repeated batches. The manager subscribes while enabled and unsubscribes when disabled. No level objects or encounter positions are created automatically.

The project's API is `GameEventBus`, using `GameplayEvents.OnBattleTriggered` (the literal event name is `OnBattleTriggered`). Other scripts can raise the same no-payload event. It is also available as `GameplayEventType.BattleTriggered` for existing event selectors.

`player.EquipWeapon("FAL")` enables shooting. `TryShoot()` applies recoil, enforces the fire interval, and raises `ShotFired` only for accepted shots. `HolsterWeapon()` disables shooting and clears recoil. Attach projectile, hitscan, muzzle flash, or audio logic to `ShotFired`; this module does not invent a damage/inventory system. Input is separate: assign a fire InputActionReference or use the left mouse fallback. Input-driven shooting requires the cursor to be locked and the game unpaused.

`WeaponRecoil` exposes intensity (degrees per shot), maximum accumulated recoil and recovery speed (degrees per second). It contributes a temporary upward offset to the existing camera controller, separately from look input and immersion shake.
