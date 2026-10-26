# La carta de Mateo

Level1 contains `====GAMEPLAY==== / Lost Letter Mission`: Mateo and four collectible pieces of the existing CartaLanus model. Mateo stands near the camp's crates, at approximately `(76, 27.78, 270)`.

Talk to Mateo using the existing NPC interaction. His dialogue explains that his mother's rain-soaked letter caught on barbed wire, tore into four pieces, and blew away. Accepting the request adds **La carta de Mateo** to the missions UI. Declining leaves the offer available for later.

The four pieces stay hidden until the mission is accepted. Use **Inspeccionar** to examine each piece with the existing inspection controls. The piece is collected and disappears only when inspection ends or is interrupted. Then talk to Mateo and choose **Acá tenés. Están los cuatro.** Each piece and the final return objective are crossed out. Other missions remain intact; repeated interactions cannot increase the count.

## Authored assets

- Dialogue lines: `DialogueSystem/ScriptableObjects/Dialogues/Mateo/LostLetter`.
- Conversation branches: `DialogueSystem/ScriptableObjects/Conversations/Mateo/LostLetter`.
- Dialogue choice event target: `Missions/ScriptableObjects/LostLetter/LetterMissionAction.asset`.
- NPC prefab: `Assets/Prefabs/Characters/MateoLostLetter.prefab`.
- Piece prefabs: `Assets/Prefabs/Interactables/LetterFragments`.
- Scene references and progress: the existing `LetterManager` on `MissionsManagers`.

`LetterMissionAction` resolves the scene's LetterManager when the player selects a choice. The dialogue assets therefore contain no scene-object references. `SpeakerScript.SetConversation` selects the next interaction without interrupting the current dialogue. Progress lasts for the current scene session; no save-game persistence is added.

## Verification

The editor smoke check covers visibility before/after acceptance, keeping pieces visible during inspection, normal exit, controller interruption, target deactivation, switching targets, transform restoration, duplicate protection, and mission completion. A full player-controlled Play Mode walkthrough is still recommended for dialogue pacing and piece visibility in grass.

`Tools / Missions / Validate Mateo Lost Letter` reruns the state check in Edit Mode. It offers to save pending scene edits, then reloads Level1 and discards the simulated mission progress. `Arrange Mateo Lost Letter` repositions the authored mission objects and saves the scene. The creation command is one-time and refuses to overwrite existing dialogue assets.
