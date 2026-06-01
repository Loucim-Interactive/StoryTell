using System;
using System.Collections.Generic;
using UnityEngine;

namespace InmersionSystem.Utils {
    public class CharacterEffectsScript : MonoBehaviour
    {
        public List<InmersiveElements.EAmbientEffect> effects = new List<InmersiveElements.EAmbientEffect>();
        
        private GameObject _dumpObject;
        public void Awake() {
            _dumpObject.transform.SetParent(transform);
            _dumpObject.transform.name = "CharacterEffectsDump";
            AddEffects(effects);
        }

        private void AddEffects(List<InmersiveElements.EAmbientEffect> all) {
            foreach (var vfx in all) {
                //_dumpObject.AddComponent<>() // add the effect
            }
        }
    }
}
