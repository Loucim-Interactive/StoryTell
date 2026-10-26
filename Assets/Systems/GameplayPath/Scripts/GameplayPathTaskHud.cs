using System.Text;
using StoryTell.PhysicalMap;
using TMPro;
using UnityEngine;

namespace StoryTell.GameplayPath
{
    public sealed class GameplayPathTaskHud : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private PhysicalMapController map;
        [SerializeField] private GameObject checklistPanel, newMissionPanel;
        [SerializeField] private TMP_Text checklist, newMission;
        private PathStage previousStage;
        private PathEquipment previousEquipment;
        private int previousFragments = -1;
        private bool previousAssembly, initialized;
        private float announcementUntil, completedUntil;
        private string completedTask;
        public bool IsVisible => checklistPanel.activeInHierarchy;
        public string Checklist => checklist.text;
        public string Announcement => newMission.text;

        private void LateUpdate()
        {
            var p = controller.Progress;
            bool stageChanged = !initialized || p.Stage != previousStage;
            if (stageChanged)
            {
                if (initialized) { completedTask = StageName(previousStage); completedUntil = Time.unscaledTime + 7; }
                previousStage = p.Stage;
                newMission.text = "<size=20>NUEVA MISIÓN</size>\n" + controller.Objective;
                announcementUntil = Time.unscaledTime + 8;
            }
            if (stageChanged || p.Equipment != previousEquipment || p.Fragments != previousFragments || p.LetterAssembled != previousAssembly)
            {
                previousEquipment = p.Equipment; previousFragments = p.Fragments; previousAssembly = p.LetterAssembled;
                Rebuild(); initialized = true;
            }
            if (completedTask != null && Time.unscaledTime > completedUntil) { completedTask = null; Rebuild(); }
            bool visible = !map.IsOpen && !controller.IsBusy && p.Stage != PathStage.Complete;
            checklistPanel.SetActive(visible);
            newMissionPanel.SetActive(visible && Time.unscaledTime < announcementUntil);
        }

        private void Rebuild()
        {
            var p = controller.Progress;
            var text = new StringBuilder("<b>TAREAS</b>\n");
            if (completedTask != null) text.Append("<color=#A6CBAA>[X] ").Append(completedTask).Append("</color>\n");
            text.Append("[ ] ").Append(StageName(p.Stage));
            if (p.Stage == PathStage.Equipment || p.Stage == PathStage.ReturnEquipment)
            {
                AddEquipment(text, PathEquipment.Helmet, "Casco"); AddEquipment(text, PathEquipment.Rifle, "FAL");
                AddEquipment(text, PathEquipment.Ammunition, "Munición"); AddEquipment(text, PathEquipment.Map, "Mapa"); AddEquipment(text, PathEquipment.Radio, "Radio");
            }
            if (p.Stage == PathStage.FindLetter || p.Stage == PathStage.ReturnLetter)
            {
                text.Append(p.Fragments == 4 ? "\n  [X] " : "\n  [ ] ").Append("Fragmentos ").Append(p.Fragments).Append("/4");
                text.Append(p.LetterAssembled ? "\n  [X] " : "\n  [ ] ").Append("Reconstruir la carta");
                text.Append("\n  [ ] Entregar a Rubén");
            }
            checklist.text = text.ToString();
            ((RectTransform)checklistPanel.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Clamp(checklist.GetPreferredValues().y + 36, 100, 360));
        }
        private void AddEquipment(StringBuilder text, PathEquipment item, string name) => text.Append(controller.Progress.Has(item) ? "\n  [X] " : "\n  [ ] ").Append(name);
        public static string StageName(PathStage stage) => stage switch
        {
            PathStage.Arrival => "Hablar con Rubén", PathStage.Equipment => "Recoger el equipo",
            PathStage.ReturnEquipment => "Volver con Rubén", PathStage.Kitchen => "Comer en la cocina",
            PathStage.LetterRequest => "Escuchar a Rubén", PathStage.FindLetter => "Recuperar la carta",
            PathStage.ReturnLetter => "Reconstruir y devolver la carta", PathStage.Sergeant => "Hablar con el sargento",
            PathStage.TakeCrate => "Recoger la caja", PathStage.CarryCrate => "Entregar suministros en el puesto",
            PathStage.ApproachBattle => "Atravesar el paso hacia el frente", PathStage.Combat => "Defender la posición",
            _ => "Escena finalizada"
        };
    }
}
