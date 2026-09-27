# NPC decisions

Each `DialogueSO` now has an optional **Choices** list. Existing lines with no choices still advance normally. A choice contains a label, an outcome, and an optional `On Selected` event:

- **Continue**: play the next line in the current conversation.
- **Branch**: replace the rest of the conversation with **Next Conversation**, starting at its first line. Branch destinations can contain more choices.
- **End**: close this conversation.

Choices appear after the line finishes typing and its voice clip or Extra Read Time completes. The last NPC line stays on screen while the player chooses. NPC decisions have no deadline. Selection closes the panel with its existing animation; the selected branch then speaks before offering further choices.

## Try the example

1. Open Level1 (it already contains the subtitle UI, decision manager, and animated choices presenter).
2. Assign `ScriptableObjects/BranchingExample/Entry.asset` to the first conversation slot on a test NPC's `SpeakerScript`, or an instance of `DialogueSystem/Prefabs/NPC.prefab`. Existing authored NPC conversation assignments have been left intact.
3. Enter Play mode and interact with that NPC using your existing interaction binding.
4. Use **1 / 2** to cycle options and **Enter** to select. These defaults can be changed on `DecisionManagerScript`, including an optional submit Input Action.

The example offers route and supplies branches, a Continue choice, and an End choice. The route branch offers another decision. No voice clips are required.

For a new scene, provide `DialogueManagerScript` with its `UIDialogueScript`, plus a `DecisionManagerScript` and configured `WalkieUIScript` (the existing shared presenter). Set `Choices Panel Root` to the whole animated panel and `Choices Content` to its list container. The presenter does not require a walkie object for NPC decisions. `Choices Radio Icon` optionally identifies the decoration hidden during NPC decisions; the existing `WalkieIcon` child is found automatically.

## Ownership and behavior

`DialogueManagerScript` owns NPC playback. It reserves the decision session through `DecisionManagerScript.TryAcquire`, sends labels and a callback through `Present`, and releases it when finished or cancelled. The presenter binds that request to the same choice prefab and animations used for radio responses. Requests have a version so revisiting the same dialogue asset still resets and rebuilds its options.

NPC decisions ignore radio equip state and do not show the radio indicator or countdown. An active radio call must finish before starting an NPC conversation. Calls triggered during NPC dialogue are queued on the radio and start after the conversation finishes. This also prevents two speakers from overwriting the shared subtitles. Deactivating the speaking NPC cancels its conversation and releases its pending choices.

The manager retains `PlayConversation` for existing callers and adds `TryStartConversation(conversation, owner)` so callers can avoid consuming a conversation when the system is busy. `SpeakerScript` uses this entry point, and both the raycast dialogue interaction and the general `NPCInteractableScript` route to it.

## Verification

- Check the two distinct branches, nested route choices, Continue, and End.
- Confirm no options appear during typing, and that a single submit resolves once.
- Equip/store the walkie during NPC choices: the options should stay available.
- Trigger a radio zone during NPC dialogue: the radio should wait, then run normally afterward.
- Disable the speaking NPC or choices presenter while waiting: no stale decision should remain.
- Recheck an existing conversation with no choices and a timed radio call.
