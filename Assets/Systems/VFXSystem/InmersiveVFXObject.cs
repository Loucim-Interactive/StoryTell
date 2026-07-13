using System;
using InmersionSystem;
using Systems.Utils;
using UnityEngine;

namespace Systems.VFXSystem.New {
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(ColliderVisualizer))]
    public class InmersiveVFXObject : VFXObject<InmersiveElements.EInducedEffect> {
        private Collider _collider;
        private ColliderVisualizer _visualizer;
        public void Awake() {
            _collider = GetComponent<Collider>();
            _visualizer = GetComponent<ColliderVisualizer>();
            _collider.isTrigger = true;
            _visualizer.triggerColor = Color.red;
        }
    }
}
