using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.VFXSystem.New {
    [CreateAssetMenu(fileName = "VFXRegistry", menuName = "VFX/VFX Registry")]
    public class VFXRegistry : ScriptableObject
    {
        [Header("VFX Registry")]
        [Tooltip("All of the VFX definition assets should be placed here.")]
        [SerializeField] private VFXDefinition[] definitions;
        private Dictionary<VFXTypes, VFXDefinition> _lookup;

        private void OnEnable() {
            _lookup = new Dictionary<VFXTypes, VFXDefinition>();
            foreach (var vfx in definitions) {
                if (!IsValid(vfx)) continue;
                _lookup.TryAdd(vfx.type, vfx);
            }
        }
        
        public bool TryGet(VFXTypes type, out VFXDefinition definition) => _lookup.TryGetValue(type, out definition);

        private bool IsValid(VFXDefinition vfx) {
            if (vfx == null) return false;
            if (_lookup.ContainsKey(vfx.type)) {
                Debug.LogWarning("[VFXRegistry] VFX registry duplicate entry: " + vfx.type);
                return false;
            }
            
            return true;
        }
    }
}
