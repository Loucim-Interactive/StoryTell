using UnityEngine;
using UnityEngine.Rendering;

namespace Systems.InmersionSystem.Induced.Rendering {
    [DisallowMultipleComponent]
    public sealed class InducedVisionState : MonoBehaviour {
        [System.NonSerialized] public float Blur, Tunnel, StepPrinting;
        [System.NonSerialized] public float ClearRadius = .28f, Softness = .35f, Persistence = .22f, PrintRate = 12;
        internal RTHandle History;
        internal bool HistoryValid;
        internal float LastPrintTime;
        public bool IsActive => Blur > 0 || Tunnel > 0 || StepPrinting > 0;
        public void ReleaseHistory() {
            History?.Release();
            History = null;
            HistoryValid = false;
        }
        private void OnDisable() { Blur = Tunnel = StepPrinting = 0; ReleaseHistory(); }
        private void OnDestroy() { ReleaseHistory(); }
    }
}
