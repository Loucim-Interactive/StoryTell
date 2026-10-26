using System;
using System.IO;
using System.Linq;
using DialogueSystem.Scripts;
using InteractionSystem.Scripts.Utils;
using Systems.InteractionSystem.Scripts.Interactables.Pickables;
using Systems.InteractionSystem.Scripts.Utils;
using Systems.Missions.FindObjects;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Systems.Missions.Editor {
    // One-time authoring utility. Refuses to overwrite the authored mission on subsequent runs.
    public static class LostLetterMissionSetup {
        private const string Lines = "Assets/Systems/DialogueSystem/ScriptableObjects/Dialogues/Mateo/LostLetter";
        private const string Conversations = "Assets/Systems/DialogueSystem/ScriptableObjects/Conversations/Mateo/LostLetter";
        private const string MissionAssets = "Assets/Systems/Missions/ScriptableObjects/LostLetter";
        private const string Pieces = "Assets/Prefabs/Interactables/LetterFragments";

        [MenuItem("Tools/Missions/Create Mateo Lost Letter")]
        public static void Create() {
            if (AssetDatabase.LoadAssetAtPath<ConversationSO>(Conversations + "/Offer.asset"))
                throw new InvalidOperationException("The lost-letter mission already exists. Edit its authored assets directly.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            foreach (string path in new[] { Lines, Conversations, MissionAssets, Pieces }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();

            var action = ScriptableObject.CreateInstance<LetterMissionAction>();
            AssetDatabase.CreateAsset(action, MissionAssets + "/LetterMissionAction.asset");
            var greeting = Line("Greeting", "Che, ¿tenés un minuto? Soy Mateo. Perdí una carta de mi vieja, de Lanús. Es la única que me llegó desde que estoy acá.");
            var accident = Line("Accident", "La llevaba en el bolsillo de la campera. Con la lluvia se empapó y, cuando pasé por el alambrado, se enganchó en una púa. Quise sacarla y se abrió en cuatro pedazos.");
            var request = Line("Request", "Una ráfaga me los arrancó de la mano. Los vi caer por acá cerca, alrededor del puesto, pero me llamaron a la guardia y no pude buscarlos. ¿Me das una mano para juntarlos?");
            var accepted = Conversation("Accepted", Line("Accepted", "Gracias, che. Son cuatro pedazos de papel, por el suelo de este sector. Si encontrás todos, traémelos. Aunque tenga que pegarla, quiero poder leerla entera."));
            var declined = Conversation("Declined", Line("Declined", "Está bien, no te preocupes. Si después te hacés un rato, vení a verme. Yo sigo acá."));
            var searching = Conversation("Searching", Line("Searching", "¿Pudiste encontrar algo? Son cuatro pedazos, acá cerca del puesto. Fijate en el suelo, a unos pasos de donde estoy. Cuando tengas los cuatro, traémelos."));
            var thanks = Conversation("Thanks", Line("Thanks", "¡Están los cuatro! Mirá, acá quedó la firma de mi vieja. Gracias, de verdad. Ahora consigo un poco de cinta y la armo. Me devolviste un pedacito de casa."));
            var epilogue = Conversation("Completed", Line("Completed", "Ya guardé los pedazos en un sobre, bien adentro de la campera. Esta vez no se me escapan. Gracias por darme una mano, che."));
            var handover = Line("Return", "¡Los encontraste! ¿Me pasás los cuatro pedazos? Tengo unas ganas de terminar de leer lo que me escribió mi vieja...");
            Choices(request, ("Dale, yo te busco los pedazos.", accepted, "AcceptMission"),
                ("Ahora no puedo. Después vuelvo.", declined, null));
            Choices(handover, ("Acá tenés. Están los cuatro.", thanks, "ReturnLetter"));
            var offer = Conversation("Offer", greeting, accident, request);
            var returnConversation = Conversation("Return", handover);

            var npcSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/NPC.prefab");
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(npcSource);
            npc.name = "NPC - Mateo (carta perdida)";
            npc.tag = "Untagged";
            npc.transform.SetPositionAndRotation(Ground(72f, 266f), Quaternion.Euler(0f, 210f, 0f));
            var speaker = npc.GetComponent<SpeakerScript>();
            speaker.SetConversation(offer);
            var interaction = npc.GetComponentInChildren<Systems.InteractionSystem.Scripts.Interactables.GlobalUtil.NPCInteractableScript>();
            interaction.interactableName = "Mateo";
            interaction.UIInteraction.label = "Hablar";
            ConfigureInteraction(interaction);
            PrefabUtility.RecordPrefabInstancePropertyModifications(speaker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            var npcPrefab = PrefabUtility.SaveAsPrefabAsset(npc, "Assets/Prefabs/Characters/MateoLostLetter.prefab");
            Object.DestroyImmediate(npc);
            npc = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
            speaker = npc.GetComponent<SpeakerScript>();

            Vector2[] locations = { new(68f, 263f), new(77f, 264f), new(74f, 273f), new(64f, 270f) };
            var fragments = new LetterInteractableScript[4];
            for (int i = 0; i < fragments.Length; i++) {
                var root = new GameObject($"Carta de Mateo - pedazo {i + 1}");
                root.layer = 6;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Art/Maximo/Interactables/Cartas/CartaLanus/Carta3parte{i + 1}.fbx"));
                model.transform.SetParent(root.transform, false);
                Bounds bounds = BoundsOf(model);
                Vector3 size = bounds.size;
                Vector3 normal = size.x < size.y && size.x < size.z ? Vector3.right : size.y < size.z ? Vector3.up : Vector3.forward;
                model.transform.rotation = Quaternion.FromToRotation(normal, Vector3.up);
                bounds = BoundsOf(model);
                model.transform.localScale *= 0.32f / Mathf.Max(bounds.size.x, bounds.size.z);
                bounds = BoundsOf(model);
                model.transform.position -= bounds.center;
                var collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.4f, 0.12f, 0.4f);
                var pickup = root.AddComponent<LetterInteractableScript>();
                pickup.interactableName = $"Pedazo de carta ({i + 1}/4)";
                pickup.UIInteraction = new UIInteraction { label = "Inspeccionar", interactionType = EInteractions.LeftClick };
                ConfigureInteraction(pickup);
                pickup.collectAfterInspection = true;
                var pickupSettings = new SerializedObject(pickup);
                pickupSettings.FindProperty("inspectInteraction").boolValue = true;
                pickupSettings.FindProperty("subInteractions").boolValue = true;
                pickupSettings.ApplyModifiedPropertiesWithoutUndo();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Pieces}/MateoLetterFragment{i + 1}.prefab");
                Object.DestroyImmediate(root);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = Ground(locations[i].x, locations[i].y) + Vector3.up * 0.06f;
                instance.transform.rotation = Quaternion.Euler(0f, i * 73f, 0f);
                fragments[i] = instance.GetComponent<LetterInteractableScript>();
                Debug.Log($"LETTER_FRAGMENT_{i + 1}: {instance.transform.position}");
            }

            var manager = Object.FindFirstObjectByType<LetterManager>();
            if (!manager) throw new InvalidOperationException("Level1 needs its existing LetterManager.");
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_missionsUI").objectReferenceValue = Object.FindFirstObjectByType<MissionsUIManager>();
            serialized.FindProperty("missionGiver").objectReferenceValue = speaker;
            serialized.FindProperty("searchingConversation").objectReferenceValue = searching;
            serialized.FindProperty("returnConversation").objectReferenceValue = returnConversation;
            serialized.FindProperty("completedConversation").objectReferenceValue = epilogue;
            var array = serialized.FindProperty("fragments");
            array.arraySize = fragments.Length;
            for (int i = 0; i < fragments.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = fragments[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(npc.scene);
            Debug.Log("LOST_LETTER_CREATED");
            SmokeCheck();
        }

        private static DialogueSO Line(string name, string text) {
            var line = ScriptableObject.CreateInstance<DialogueSO>();
            var serialized = new SerializedObject(line);
            serialized.FindProperty("_speaker").enumValueIndex = (int)ESpeakers.Mateo;
            serialized.FindProperty("_dialogueText").stringValue = text;
            serialized.FindProperty("_extraReadTime").floatValue = 1.5f;
            serialized.FindProperty("_textSpeed").enumValueIndex = (int)ETextSpeed.Fast;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(line, $"{Lines}/{name}.asset");
            return line;
        }

        private static ConversationSO Conversation(string name, params DialogueSO[] lines) {
            var conversation = ScriptableObject.CreateInstance<ConversationSO>();
            conversation.Dialogues = lines;
            AssetDatabase.CreateAsset(conversation, $"{Conversations}/{name}.asset");
            return conversation;
        }

        private static void Choices(DialogueSO line, params (string label, ConversationSO next, string method)[] choices) {
            var serialized = new SerializedObject(line);
            var array = serialized.FindProperty("choices");
            array.arraySize = choices.Length;
            for (int i = 0; i < choices.Length; i++) {
                var choice = array.GetArrayElementAtIndex(i);
                choice.FindPropertyRelative("label").stringValue = choices[i].label;
                choice.FindPropertyRelative("outcome").enumValueIndex = (int)DialogueChoiceOutcome.Branch;
                choice.FindPropertyRelative("nextConversation").objectReferenceValue = choices[i].next;
                var calls = choice.FindPropertyRelative("onSelected.m_PersistentCalls.m_Calls");
                calls.arraySize = choices[i].method == null ? 0 : 1;
                if (calls.arraySize == 0) continue;
                var call = calls.GetArrayElementAtIndex(0);
                call.FindPropertyRelative("m_Target").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LetterMissionAction>(MissionAssets + "/LetterMissionAction.asset");
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(LetterMissionAction).AssemblyQualifiedName;
                call.FindPropertyRelative("m_MethodName").stringValue = choices[i].method;
                call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // Void
                call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // RuntimeOnly
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInteraction(Systems.InteractionSystem.Scripts.InteractableScript interaction) {
            var serialized = new SerializedObject(interaction);
            foreach (string property in new[] { "stateDescription", "focusInteraction", "inspectInteraction", "subInteractions" })
                serialized.FindProperty(property).boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds BoundsOf(GameObject model) {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Letter model has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 Ground(float x, float z) {
            foreach (Terrain terrain in Terrain.activeTerrains) {
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (x >= origin.x && x <= origin.x + size.x && z >= origin.z && z <= origin.z + size.z)
                    return new Vector3(x, terrain.SampleHeight(new Vector3(x, 0f, z)) + origin.y, z);
            }
            throw new InvalidOperationException("No terrain under mission location.");
        }

        [MenuItem("Tools/Missions/Arrange Mateo Lost Letter")]
        public static void Arrange() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var manager = Object.FindFirstObjectByType<LetterManager>();
            var serialized = new SerializedObject(manager);
            var speaker = (SpeakerScript)serialized.FindProperty("missionGiver").objectReferenceValue;
            var group = GameObject.Find("Lost Letter Mission") ?? new GameObject("Lost Letter Mission");
            var gameplay = GameObject.Find("====GAMEPLAY====");
            if (gameplay) group.transform.SetParent(gameplay.transform, true);
            speaker.transform.SetParent(group.transform, true);
            speaker.name = "NPC - Mateo (carta perdida)";
            speaker.transform.position = ClearGround(speaker.transform, 76f, 270f, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(speaker.transform);
            var pieces = serialized.FindProperty("fragments");
            for (int i = 0; i < pieces.arraySize; i++) {
                var piece = (LetterInteractableScript)pieces.GetArrayElementAtIndex(i).objectReferenceValue;
                piece.transform.SetParent(group.transform, true);
                Vector3 p = piece.transform.position;
                piece.transform.position = ClearGround(piece.transform, p.x, p.z, false) + Vector3.up * 0.06f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(piece.transform);
            }
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            Selection.activeGameObject = speaker.gameObject;
            SceneView.lastActiveSceneView.LookAt(speaker.transform.position + Vector3.up, Quaternion.Euler(20f, 35f, 0f), 5f);
            Debug.Log("LOST_LETTER_PLACEMENT_CHECK_PASSED: NPC and pieces clear of solid colliders.");
        }

        private static Vector3 ClearGround(Transform target, float x, float z, bool person) {
            Physics.SyncTransforms();
            for (int i = 0; i < 100; i++) {
                float radius = i == 0 ? 0f : 0.5f + i / 12;
                float angle = i * 2.399963f;
                Vector3 p = Ground(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius);
                Collider[] nearby = Physics.OverlapCapsule(p + Vector3.up * 0.4f,
                    p + Vector3.up * (person ? 1.6f : 0.4f), person ? 0.4f : 0.3f, ~0, QueryTriggerInteraction.Ignore);
                if (nearby.All(c => c is TerrainCollider || c.transform.IsChildOf(target))) return p;
            }
            throw new InvalidOperationException("Could not find an unobstructed mission placement.");
        }

        // Run on a freshly loaded scene. It exercises gameplay state, then discards all test changes.
        [MenuItem("Tools/Missions/Validate Mateo Lost Letter")]
        public static void SmokeCheck() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var manager = Object.FindFirstObjectByType<LetterManager>();
            var serialized = new SerializedObject(manager);
            var array = serialized.FindProperty("fragments");
            if (array.arraySize != 4) throw new Exception("Expected four letter pieces.");
            var pieces = Enumerable.Range(0, 4).Select(i => (LetterInteractableScript)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var pickup = typeof(LetterManager).GetMethod("OnPickup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Action<LetterInteractableScript> collect = piece => pickup.Invoke(manager, new object[] { piece });
            var probe = new GameObject("Inspection smoke test");
            var inspection = probe.AddComponent<InspectionScript>();
            inspection.inspectAnchor = new GameObject("Anchor").transform;
            inspection.inspectAnchor.SetParent(probe.transform);
            inspection.lockCursor = false;
            var ordinary = new GameObject("Ordinary inspected object");
            GameEventBus.Subscribe(GameplayEvents.PickupAction, collect);
            try {
                InvokeLifecycle(manager, "Awake");
                if (pieces.Any(p => p.gameObject.activeSelf)) throw new Exception("Pieces visible before accepting.");
                collect(pieces[0]);
                if (manager.CollectedCount != 0) throw new Exception("Early pickup accepted.");
                manager.StartMission();
                manager.StartMission();
                if (pieces.Any(p => !p.gameObject.activeSelf)) throw new Exception("Pieces did not appear.");
                manager.CompleteMission();
                if (manager.MissionCompleted) throw new Exception("Incomplete return accepted.");
                InvokeLifecycle(inspection, "Awake");
                for (int i = 0; i < pieces.Length; i++) {
                    var piece = pieces[i];
                    Transform parent = piece.transform.parent;
                    Vector3 position = piece.transform.localPosition;
                    if (!piece.InspectInteractable || !piece.collectAfterInspection) throw new Exception("Piece not configured for inspection.");
                    piece.Interact();
                    if (!piece.gameObject.activeSelf || manager.CollectedCount != i) throw new Exception("Piece collected before inspection.");
                    inspection.StartInspect(piece.gameObject);
                    if (piece.transform.parent != inspection.inspectAnchor) throw new Exception("Inspection did not start.");
                    if (i == 0) inspection.StopInspect();
                    else if (i == 1) InvokeLifecycle(inspection, "OnDisable");
                    else if (i == 2) {
                        piece.gameObject.SetActive(false);
                        InvokeLifecycle(inspection, "Update");
                    } else inspection.StartInspect(ordinary);
                    if (piece.gameObject.activeSelf || manager.CollectedCount != i + 1 ||
                        piece.transform.parent != parent || piece.transform.localPosition != position)
                        throw new Exception("Inspection cleanup/collection failed.");
                    inspection.StopInspect();
                    collect(piece);
                    if (manager.CollectedCount != i + 1) throw new Exception("Duplicate counted.");
                }
                if (!ordinary.activeSelf) throw new Exception("Ordinary object was consumed.");
                manager.CompleteMission();
                manager.CompleteMission();
                if (!manager.MissionCompleted) throw new Exception("Return failed.");
                Debug.Log("LOST_LETTER_INSPECTION_CHECK_PASSED: hidden until acceptance, normal exit, controller interruption, inactive target, target switch, restoration, no duplicates, return.");
            } finally {
                inspection.StopInspect();
                GameEventBus.Unsubscribe(GameplayEvents.PickupAction, collect);
                Object.DestroyImmediate(ordinary);
                Object.DestroyImmediate(probe);
                EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            }
        }

        private static void InvokeLifecycle(object target, string method) {
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
