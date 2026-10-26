using UnityEngine;

namespace Systems.InspectionSystem.Scripts
{
    [DisallowMultipleComponent]
    public sealed class InspectableDocument : MonoBehaviour
    {
        public string title = "Documento";
        [TextArea(6, 24)] public string transcription;
        public Vector3 inspectionOffset = new(-0.18f, 0, 0);
        public Vector3 inspectionEulerAngles = new(-65, 0, 0);
        [Min(0.1f)] public float inspectionSize = 0.52f;
        public bool hideAfterInspection;
    }
}
