using System;
using System.Collections;
using DialogueSystem.Scripts;
using FirstPersonControllerSystem.Scripts.ControllerSys;
using StoryTell.PhysicalMap;
using StoryTell.Weapons;
using Systems.DialogueSystem.Scripts;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using Systems.InteractionSystem.Scripts;
using Systems.InspectionSystem.Scripts;
using Systems.EventSystem.Scripts;
using Systems.WalkieSystem.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StoryTell.GameplayPath
{
    [DefaultExecutionOrder(100)]
    public sealed class GameplayPathController : MonoBehaviour
    {
        [SerializeField] private GameplayPathDefinition definition;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerControllerScript movement;
        [SerializeField] private CameraControllerScript look;
        [SerializeField] private InteractionScript interaction;
        [SerializeField] private DialogueManagerScript dialogue;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private Level1WeaponTrial trial;
        [SerializeField] private GameplayPathBattle battle;
        [SerializeField] private GameObject heldCrate, deliveredCrate, eatingVisual;
        [SerializeField] private GameObject rubenAtStart, rubenAtKitchen, rubenAtFront;
        [SerializeField] private TMP_Text noticeText, journalText, readingText;
        [SerializeField] private GameObject journalPanel, readingPanel;
        [SerializeField] private InspectableDocument motherDocument;
        [SerializeField] private InputActionReference closeReading, readMother;
        private bool ownsClose, ownsRead, busy, reading, inputLocked, wasMoving, wasInteracting, mapAvailable;
        private float noticeUntil, readingSince;
        private Coroutine sequence;
        private Camera playerView;
        private WalkieTalkieScript radio;
        private InspectionScript inspection;
        private int visibleRuben;
        public GameplayPathProgress Progress { get; } = new();
        public Transform Player => player;
        public GameplayPathDefinition Definition => definition;
        public bool IsBusy => busy || reading || (inspection != null && inspection.IsInspecting) || (dialogue != null && dialogue.IsInConversation);
        public bool WeaponAllowed => Progress.Stage == PathStage.Combat && !IsBusy && (radio == null || !radio.IsEquipped);
        public bool ShowRifle => WeaponAllowed && trial != null && trial.IsInsideZone;
        public string Objective => Progress.Stage switch
        {
            PathStage.Arrival => "Hablá con Rubén junto al punto de llegada.",
            PathStage.Equipment => "Recogé el equipo de la mesa de suministros y sus alrededores.",
            PathStage.ReturnEquipment => "Volvé con Rubén: ya tenés todo el equipo.",
            PathStage.Kitchen => "Andá a la cocina de campaña y acercá el jarro para comer.",
            PathStage.LetterRequest => "Hablá con Rubén junto a la cocina.",
            PathStage.FindLetter => "Buscá los cuatro fragmentos cerca de la cocina, las cajas y la lona.",
            PathStage.ReturnLetter => "Devolvé la carta a Rubén. Leerla es opcional.",
            PathStage.Sergeant => "Buscá al sargento Acosta en la carpa principal.",
            PathStage.TakeCrate => "Recogé la caja de herramientas y mantas junto al sargento.",
            PathStage.CarryCrate => "Llevá la caja al puesto avanzado por la subida y los carteles del camino.",
            PathStage.ApproachBattle => "Seguí por el paso junto a las explosiones. Rubén te espera más adelante.",
            PathStage.Combat => "Mantené la distancia: las siluetas avanzan entre las coberturas. R para recargar.",
            _ => "Finalización escena 1"
        };
        private void OnEnable()
        {
            ownsClose = Enable(closeReading); ownsRead = Enable(readMother);
        }
        private void OnDisable()
        {
            if (ownsClose) closeReading.action.Disable();
            if (ownsRead) readMother.action.Disable();
            if (dialogue != null) dialogue.CancelConversation(this);
            if (sequence != null) StopCoroutine(sequence);
            if (inputLocked && (trial == null || !trial.IsFinishing)) UnlockInput();
        }
        private static bool Enable(InputActionReference reference)
        {
            if (reference == null || reference.action == null || reference.action.enabled) return false;
            reference.action.Enable(); return true;
        }
        private void Start()
        {
            playerView = player.GetComponentInChildren<Camera>();
            radio = player.GetComponent<WalkieTalkieScript>();
            inspection = player.GetComponent<InspectionScript>();
            map.SetOpen(false, true); map.enabled = false;
            Refresh(); Notice(Objective, 8);
        }
        private void Update()
        {
            UpdateRuben();
            if (reading && Time.unscaledTime > readingSince + 0.25f && closeReading.action.WasPressedThisFrame()) CloseReading();
            if (!reading && !IsBusy && map.enabled && map.IsOpen && Progress.Fragments == 4 && readMother.action.WasPressedThisFrame())
                InspectMotherLetter();
            bool showJournal = map.enabled && map.IsOpen && !reading && !trial.IsFinishing;
            if (journalPanel.activeSelf != showJournal) journalPanel.SetActive(showJournal);
            if (noticeText != null)
            {
                var target = interaction != null ? interaction.CurrentTarget as GameplayPathInteractable : null;
                bool otherPrompt = interaction != null && interaction.CurrentTarget != null && target == null;
                noticeText.text = IsBusy || trial.IsFinishing || otherPrompt || map.IsOpen ? "" : target != null ? "[E] " + target.UIInteraction.label :
                    Progress.Stage == PathStage.Combat && !trial.IsInsideZone ? "Volvé a la posición de combate para usar el FAL." : Time.unscaledTime < noticeUntil ? lastNotice : "";
            }
            if (heldCrate != null) heldCrate.SetActive(Progress.Stage == PathStage.CarryCrate && !map.IsOpen && !reading);
        }
        private string lastNotice;
        public void Notice(string text, float seconds = 5) { lastNotice = text; noticeUntil = Time.unscaledTime + seconds; }
        public bool Interact(PathAction action, int fragmentIndex = 0, string text = null)
        {
            if (IsBusy || trial.IsFinishing) return false;
            if (action == PathAction.Read) { OpenReading(text); return false; }
            if (action == PathAction.Fragment)
            {
                if (!Progress.CollectFragment(fragmentIndex)) return false;
                Refresh(); Notice($"Fragmentos recuperados: {Progress.Fragments}/4. " + (Progress.Fragments == 4 ? "Volvé con Rubén; podés leer la carta desde el mapa con L." : "")); return true;
            }
            PathEquipment item = action switch {
                PathAction.Helmet => PathEquipment.Helmet, PathAction.Rifle => PathEquipment.Rifle,
                PathAction.Ammunition => PathEquipment.Ammunition, PathAction.Map => PathEquipment.Map,
                PathAction.Radio => PathEquipment.Radio, _ => PathEquipment.None };
            if (item != PathEquipment.None)
            {
                if (Progress.Stage != PathStage.Equipment) { Notice("Hablá primero con Rubén para revisar lo que necesitás."); return false; }
                if (Progress.Has(item)) return false;
                if (item == PathEquipment.Rifle && !trial.CollectRifle()) return false;
                if (item == PathEquipment.Ammunition) trial.CollectMagazineCount(3);
                Progress.Collect(item); Refresh(); Notice(item == PathEquipment.Map ? "Mapa recogido · [M] Mapa y tareas" : "Equipo recogido. " + (Progress.Stage == PathStage.ReturnEquipment ? Objective : "Revisá la lista con M cuando tengas el mapa.")); return true;
            }
            if (action == PathAction.Crate)
            {
                if (!Progress.Advance(action)) { Notice("El sargento tiene que darte la orden antes de llevarte la caja."); return false; }
                Refresh(); Notice("Caja recogida. Podés consultar el mapa con M; la caja se apoya temporalmente."); return true;
            }
            ConversationSO conversation = GetConversation(action);
            if (conversation != null) { sequence = StartCoroutine(Talk(conversation, action)); return false; }
            if (action == PathAction.Ruben && Progress.Stage == PathStage.FindLetter)
                Notice($"Recuperaste {Progress.Fragments}/4. Mirá sobre las cajas a los costados de la cocina y cerca del cartel.", 8);
            else Notice(Objective);
            return false;
        }
        private ConversationSO GetConversation(PathAction action)
        {
            if (action == PathAction.Ruben)
            {
                return Progress.Stage switch { PathStage.Arrival => definition.introduction, PathStage.ReturnEquipment => definition.equipmentReady,
                    PathStage.LetterRequest => definition.letterRequest, PathStage.ReturnLetter => definition.letterReturn, _ => null };
            }
            if (action == PathAction.Eat && Progress.Stage == PathStage.Kitchen) return definition.kitchen;
            if (action == PathAction.Sergeant && Progress.Stage == PathStage.Sergeant) return definition.sergeant;
            if (action == PathAction.Officer && Progress.Stage == PathStage.CarryCrate) return definition.delivery;
            return null;
        }
        private IEnumerator Talk(ConversationSO conversation, PathAction action)
        {
            int completedBefore = dialogue.CompletedConversationCount;
            if (!dialogue.TryStartConversation(conversation, this)) { Notice("Esperá a que termine la conversación o la llamada de radio."); yield break; }
            busy = true; LockInput(); map.SetOpen(false, true);
            if (action == PathAction.Eat && eatingVisual != null) eatingVisual.SetActive(true);
            while (dialogue.IsConversationOwnedBy(this)) yield return null;
            if (eatingVisual != null) eatingVisual.SetActive(false);
            if (dialogue.CompletedConversationCount > completedBefore) Progress.Advance(action);
            busy = false; UnlockInput(); Refresh(); Notice(Objective, 7); sequence = null;
        }
        public bool TryEnterBattle()
        {
            if (IsBusy || !Progress.Advance(PathAction.BattleEntrance)) return false;
            map.SetOpen(false, true); Refresh(); battle.Begin(); Notice("¡A cubierto! Mantené la distancia. Hay cargadores en el puesto.", 9);
            dialogue.StateThought("¡Martín! ¡Se mueven detrás de las cajas! ¡No dejes que se acerquen!", "Rubén · radio");
            return true;
        }
        public bool FinishBattle(bool contact)
        {
            if (!Progress.Finish()) return false;
            if (reading) CloseReading();
            battle.StopBattle(); map.SetOpen(false, true); journalPanel.SetActive(false);
            dialogue.StateThought("¡Martín! ¿Me escuchás?", "Rubén · radio");
            trial.FinishFromGameplay(contact ? 0.4f : 4f); return true;
        }
        public void OpenReading(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || IsBusy || trial.IsFinishing) return;
            reading = true; readingSince = Time.unscaledTime; readingText.text = text + "\n\n<size=22>[E] Cerrar</size>";
            readingPanel.SetActive(true); map.SetOpen(false, true); LockInput();
        }
        public void InspectMotherLetter()
        {
            if (IsBusy || Progress.Fragments != 4 || trial.IsFinishing) return;
            if (motherDocument != null)
            {
                motherDocument.gameObject.SetActive(true);
                GameEventBus.Raise(GameplayEvents.StartInspection, motherDocument.gameObject);
            }
            else OpenReading("Carta de la madre de Rubén — ficción provisional\n\n" + definition.motherLetter);
        }
        private void CloseReading() { reading = false; readingPanel.SetActive(false); UnlockInput(); }
        private void LockInput()
        {
            if (inputLocked) return;
            inputLocked = true; wasMoving = movement.enabled; wasInteracting = interaction.enabled;
            movement.enabled = false; interaction.enabled = false; look.SetInputLocked(true);
            mapAvailable = map.enabled; map.enabled = false;
        }
        private void UnlockInput()
        {
            if (!inputLocked) return;
            if (movement != null) movement.enabled = wasMoving;
            if (interaction != null) interaction.enabled = wasInteracting;
            if (look != null) look.SetInputLocked(false);
            if (map != null) map.enabled = mapAvailable;
            inputLocked = false;
        }
        private void Refresh()
        {
            map.enabled = Progress.Has(PathEquipment.Map) && !IsBusy;
            if (deliveredCrate != null) deliveredCrate.SetActive(Progress.Stage >= PathStage.ApproachBattle);
            UpdateRuben();
            string Check(PathEquipment flag, string label) => (Progress.Has(flag) ? "[x] " : "[ ] ") + label;
            journalText.text = "<size=32>Tareas — Escena 1</size>\n\n" + Objective + "\n\n" +
                Check(PathEquipment.Helmet, "Casco") + "\n" + Check(PathEquipment.Rifle, "FAL") + "\n" +
                Check(PathEquipment.Ammunition, "Munición") + "\n" + Check(PathEquipment.Map, "Mapa") + "\n" + Check(PathEquipment.Radio, "Radio") +
                $"\n\nCarta: {Progress.Fragments}/4 fragmentos" + (Progress.Fragments == 4 ? "\n[L] Leer la carta (opcional)" : "") +
                "\n\n<size=21>[M] Guardar mapa\nEquipo → Cocina → Carpa principal\n→ Puesto avanzado → Paso de combate</size>";
        }
        private void UpdateRuben()
        {
            int desired = Progress.Stage <= PathStage.ReturnEquipment ? 0 : Progress.Stage <= PathStage.ReturnLetter ? 1 : 2;
            if (desired == visibleRuben) return;
            GameObject current = visibleRuben == 0 ? rubenAtStart : visibleRuben == 1 ? rubenAtKitchen : rubenAtFront;
            if (playerView != null && current != null)
            {
                Vector3 offset = current.transform.position + Vector3.up - playerView.transform.position;
                if (offset.sqrMagnitude < 625 && Vector3.Dot(playerView.transform.forward, offset.normalized) > 0.15f) return;
            }
            rubenAtStart.SetActive(desired == 0); rubenAtKitchen.SetActive(desired == 1); rubenAtFront.SetActive(desired == 2);
            visibleRuben = desired;
        }
    }
}
