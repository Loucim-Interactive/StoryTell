using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DialogueSystem.Scripts;
using Systems.EventSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.InteractionSystem.Scripts.Interactables.Equipment;
using Systems.InteractionSystem.Scripts.Interactables.GlobalUtil;
using Systems.Missions.FindObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Systems.Missions.Editor {
    public static class EquipmentMissionSetup {
        private const string Lines = "Assets/Systems/DialogueSystem/ScriptableObjects/Dialogues/Diego/Equipment";
        private const string Conversations = "Assets/Systems/DialogueSystem/ScriptableObjects/Conversations/Diego/Equipment";
        private const string Actions = "Assets/Systems/Missions/ScriptableObjects/Equipment";

        [MenuItem("Tools/Missions/Create Diego Equipment")]
        public static void Create() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (AssetDatabase.LoadAssetAtPath<ConversationSO>(Conversations + "/Offer.asset"))
                throw new InvalidOperationException("Equipment mission already authored; edit its existing assets.");
            var group = GameObject.Find("Equip Mission");
            if (!group) throw new InvalidOperationException("Open Level1 with its Equip Mission group.");
            var allItems = group.GetComponentsInChildren<EquipmentInteractableScript>(true);
            var items = allItems.GroupBy(item => item.gameObject).Select(g => g.First()).ToArray();
            var manager = Object.FindFirstObjectByType<EquipmentManager>();
            if (!manager || items.Length != 3 || items.Select(i => i.interactableName).Distinct().Count() != 3)
                throw new InvalidOperationException($"Expected the existing manager and three distinct equipment items. Manager: {manager}; items: {string.Join(", ", allItems.Select(i => i.gameObject.name + ":" + i.interactableName))}");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { Lines, Conversations, Actions }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            var action = ScriptableObject.CreateInstance<EquipmentMissionAction>();
            AssetDatabase.CreateAsset(action, Actions + "/EquipmentMissionAction.asset");

            var greeting = Line("Greeting", "Che, antes de salir tenés que equiparte. Soy Diego, estoy a cargo de repartir el equipo. No te me vayas sin revisar lo que llevás.");
            var request = Line("Request", "Te dejé un casco, una radio y un rifle acá en el puesto. Andá a buscarlos y revisalos bien antes de guardarlos. El casco te protege, la radio nos mantiene en contacto y el rifle lo tenés que llevar listo. ¿Estamos?");
            var accepted = Conversation("Accepted", Line("Accepted", "Dale. Buscá el casco, la radio y el rifle sobre la mesa de este puesto. Revisá cada uno con calma; cuando termines de inspeccionarlo, lo guardás con tu equipo."));
            var declined = Conversation("Declined", Line("Declined", "Bueno, terminá lo que estés haciendo y después vení a verme. Pero antes de salir, pasá a buscar el equipo, ¿eh?"));
            var searching = Conversation("Searching", Line("Searching", "Todavía te falta equipo. Fijate en la lista: casco, radio y rifle. Los dejé sobre la mesa del puesto. Revisá lo que te falte y guardalo."));
            var completed = Conversation("Completed", Line("Completed", "Bien, ya tenés el casco, la radio y el rifle. Ahora sí estás equipado. Mantené la radio a mano y quedate atento a las indicaciones."));
            var offer = Conversation("Offer", greeting, request);
            var choiceSettings = new SerializedObject(request);
            var choices = choiceSettings.FindProperty("choices");
            choices.arraySize = 2;
            Choice(choices.GetArrayElementAtIndex(0), "Dale, voy a buscar el equipo.", accepted, action);
            Choice(choices.GetArrayElementAtIndex(1), "Bancame un momento, después vuelvo.", declined, null);
            choiceSettings.ApplyModifiedPropertiesWithoutUndo();

            foreach (var item in items) {
                var settings = new SerializedObject(item);
                settings.FindProperty("inspectInteraction").boolValue = true;
                settings.FindProperty("subInteractions").boolValue = true;
                settings.FindProperty("stateDescription").boolValue = false;
                settings.FindProperty("focusInteraction").boolValue = true;
                settings.FindProperty("equipAfterInspection").boolValue = true;
                settings.FindProperty("UIInteraction.label").stringValue = "Inspeccionar";
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
            // Some scene objects have an extra component in addition to their prefab component.
            // Keep one pickup emitter per object so Unity's GetComponent and inspection agree.
            foreach (var duplicate in allItems.Except(items)) {
                duplicate.fireEvents = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(duplicate);
                EditorUtility.SetDirty(duplicate);
            }

            Vector3 center = items.Aggregate(Vector3.zero, (position, item) => position + item.transform.position) / items.Length;
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/NPC.prefab"));
            npc.tag = "Untagged";
            npc.transform.position = FindClearGround(center, npc.transform);
            Vector3 facing = center - npc.transform.position;
            facing.y = 0;
            npc.transform.rotation = Quaternion.LookRotation(facing);
            var speaker = npc.GetComponent<SpeakerScript>();
            speaker.SetConversation(offer);
            var interaction = npc.GetComponentInChildren<NPCInteractableScript>();
            interaction.interactableName = "Diego";
            interaction.UIInteraction.label = "Hablar";
            var interactionSettings = new SerializedObject(interaction);
            foreach (string field in new[] { "inspectInteraction", "subInteractions", "stateDescription", "focusInteraction" })
                interactionSettings.FindProperty(field).boolValue = false;
            interactionSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(speaker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            var prefab = PrefabUtility.SaveAsPrefabAsset(npc, "Assets/Prefabs/Characters/DiegoEquipment.prefab");
            Object.DestroyImmediate(npc);
            npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
            npc.name = "NPC - Diego (equipamiento)";
            speaker = npc.GetComponent<SpeakerScript>();

            var mission = new SerializedObject(manager);
            mission.FindProperty("_missionName").stringValue = "Equiparse";
            mission.FindProperty("_missionsUI").objectReferenceValue = Object.FindFirstObjectByType<MissionsUIManager>();
            mission.FindProperty("missionGiver").objectReferenceValue = speaker;
            mission.FindProperty("searchingConversation").objectReferenceValue = searching;
            mission.FindProperty("completedConversation").objectReferenceValue = completed;
            var list = mission.FindProperty("equipment");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            mission.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(group.scene);
            Debug.Log($"EQUIPMENT_MISSION_CREATED: Diego at {npc.transform.position}; {string.Join(", ", items.Select(i => i.interactableName))}.");
            Validate();
        }

        private static DialogueSO Line(string name, string text) {
            var line = ScriptableObject.CreateInstance<DialogueSO>();
            var settings = new SerializedObject(line);
            settings.FindProperty("_speaker").enumValueIndex = (int)ESpeakers.Diego;
            settings.FindProperty("_dialogueText").stringValue = text;
            settings.FindProperty("_extraReadTime").floatValue = 1.5f;
            settings.FindProperty("_textSpeed").enumValueIndex = (int)ETextSpeed.Fast;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(line, $"{Lines}/{name}.asset");
            return line;
        }

        private static ConversationSO Conversation(string name, params DialogueSO[] lines) {
            var conversation = ScriptableObject.CreateInstance<ConversationSO>();
            conversation.Dialogues = lines;
            AssetDatabase.CreateAsset(conversation, $"{Conversations}/{name}.asset");
            return conversation;
        }

        private static void Choice(SerializedProperty choice, string label, ConversationSO next, EquipmentMissionAction action) {
            choice.FindPropertyRelative("label").stringValue = label;
            choice.FindPropertyRelative("outcome").enumValueIndex = (int)DialogueChoiceOutcome.Branch;
            choice.FindPropertyRelative("nextConversation").objectReferenceValue = next;
            var calls = choice.FindPropertyRelative("onSelected.m_PersistentCalls.m_Calls");
            calls.arraySize = action ? 1 : 0;
            if (!action) return;
            var call = calls.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = action;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(EquipmentMissionAction).AssemblyQualifiedName;
            call.FindPropertyRelative("m_MethodName").stringValue = "AcceptMission";
            call.FindPropertyRelative("m_Mode").enumValueIndex = 1;
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
        }

        private static Vector3 FindClearGround(Vector3 center, Transform npc) {
            Physics.SyncTransforms();
            for (int i = 0; i < 100; i++) {
                float angle = i * 2.399963f;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (3f + i / 12);
                foreach (Terrain terrain in Terrain.activeTerrains) {
                    Vector3 origin = terrain.transform.position;
                    Vector3 size = terrain.terrainData.size;
                    if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
                    point.y = terrain.SampleHeight(point) + origin.y;
                    if (Physics.OverlapCapsule(point + Vector3.up * 0.5f, point + Vector3.up * 1.6f,
                        0.5f, ~0, QueryTriggerInteraction.Ignore).All(c => c is TerrainCollider || c.transform.IsChildOf(npc))) return point;
                }
            }
            throw new InvalidOperationException("No clear position near the equipment.");
        }

        [MenuItem("Tools/Missions/Validate Diego Equipment")]
        public static void Validate() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var manager = Object.FindFirstObjectByType<EquipmentManager>();
            var data = new SerializedObject(manager);
            var array = data.FindProperty("equipment");
            var items = Enumerable.Range(0, array.arraySize).Select(i => (EquipmentInteractableScript)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var ui = (MissionsUIManager)data.FindProperty("_missionsUI").objectReferenceValue;
            var speaker = (SpeakerScript)data.FindProperty("missionGiver").objectReferenceValue;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Action<EquipmentInteractableScript> pickup = item => typeof(EquipmentManager).GetMethod("OnPickup", flags).Invoke(manager, new object[] { item });
            var probe = new GameObject("Equipment inspection test");
            var inspection = probe.AddComponent<InspectionScript>();
            inspection.inspectAnchor = new GameObject("Anchor").transform;
            inspection.inspectAnchor.SetParent(probe.transform);
            inspection.lockCursor = false;
            GameEventBus.Subscribe(GameplayEvents.PickupAction, pickup);
            try {
                if (items.Length != 3) throw new Exception("Expected three existing items.");
                typeof(EquipmentManager).GetMethod("Awake", flags).Invoke(manager, null);
                if (items.Any(i => i.gameObject.activeSelf)) throw new Exception("Equipment visible before acceptance.");
                pickup(items[0]);
                if (manager.EquippedCount != 0) throw new Exception("Early pickup counted.");
                ui.AddItemToMission("Other mission", "Existing objective");
                manager.StartMission();
                manager.StartMission();
                if (items.Any(i => !i.gameObject.activeSelf)) throw new Exception("Equipment did not appear.");
                if (((System.Collections.ICollection)typeof(MissionsUIManager).GetField("_missions", flags).GetValue(ui)).Count != 2)
                    throw new Exception("Another mission was replaced or equipment mission duplicated.");
                for (int i = 0; i < items.Length; i++) {
                    var item = items[i];
                    item.Interact();
                    if (!item.gameObject.activeSelf || manager.EquippedCount != i) throw new Exception("Equipment consumed before inspection.");
                    inspection.StartInspect(item.gameObject);
                    if (item.transform.parent != inspection.inspectAnchor) throw new Exception("Inspection failed to start.");
                    if (i == 1) typeof(InspectionScript).GetMethod("OnDisable", flags).Invoke(inspection, null);
                    else inspection.StopInspect();
                    pickup(item);
                    if (item.gameObject.activeSelf || manager.EquippedCount != i + 1 || !manager.IsEquipped(item)) throw new Exception("Equipment progress failed.");
                    if (i < items.Length - 1 && manager.MissionCompleted) throw new Exception("Mission completed early.");
                }
                var conversations = new SerializedObject(speaker).FindProperty("_conversationSos");
                if (!manager.MissionCompleted || conversations.GetArrayElementAtIndex(0).objectReferenceValue != data.FindProperty("completedConversation").objectReferenceValue)
                    throw new Exception("Completion dialogue not selected.");
                Debug.Log("EQUIPMENT_MISSION_CHECK_PASSED: hidden until acceptance, existing missions preserved, inspection, interruption, duplicate protection, completion dialogue.");
            } finally {
                inspection.StopInspect();
                GameEventBus.Unsubscribe(GameplayEvents.PickupAction, pickup);
                Object.DestroyImmediate(probe);
                EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            }
        }
    }
}
