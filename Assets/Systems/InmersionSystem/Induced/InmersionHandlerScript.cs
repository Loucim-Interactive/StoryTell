using System.Collections.Generic;
using InmersionSystem.Induced.Configs.Base;
using UnityEngine;

namespace InmersionSystem.Induced {
    public class InmersionHandlerScript : MonoBehaviour
    {
        private Dictionary<InmersiveElements.EInducedEffect, InducedEffect<InducedEffectConfig>> _activeEffects;

        public void Trigger(InmersiveElements.EInducedEffect effect, float intensity = 1f)
        {
            // If already active → restart it (your "re-trigger" behavior)
            // If not → add and start
        }

        public void ForceStop(InmersiveElements.EInducedEffect effect) {  }
    }
}
