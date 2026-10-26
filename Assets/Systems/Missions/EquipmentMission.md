# Equiparse

Diego gives this mission in Level1 under `====GAMEPLAY==== / Equip Mission`. It uses the existing Casco, Radio and Rifle objects and their original positions. Dialogue lines live in `DialogueSystem/ScriptableObjects/Dialogues/Diego/Equipment`, conversation branches in `DialogueSystem/ScriptableObjects/Conversations/Diego/Equipment`, and the choice action in `Missions/ScriptableObjects/Equipment`.

The player can accept or postpone. Equipment stays hidden until acceptance. Each item can then be inspected with the existing controls, and is collected when inspection ends or is interrupted. The mission checklist marks each item once, preserving other missions. Getting all three completes the mission and changes Diego's next conversation to his final instructions.

`EquipmentManager.IsEquipped(item)` exposes acquired equipment for other scripts. This records the mission's equipment state for the current scene session; it does not introduce a new weapon, inventory, or save-game system.

`Tools / Missions / Validate Diego Equipment` checks visibility, repeated acceptance, preservation of another mission, normal and interrupted inspection, duplicate pickups, and the completion conversation in Edit Mode. It saves pending scene edits first, then discards simulated progress by reloading Level1. `Create Diego Equipment` is a one-time authoring command that refuses to overwrite existing assets.
