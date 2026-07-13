using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InmersionSystem;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.VFXSystem.New {
    public abstract class VFXTriggerBase : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private List<VFXObject<InmersiveElements.EInducedEffect>> vfxObjects = new();
        [SerializeField] private bool autoFindVFXObjects;

        [Header("General Event")]
        [Tooltip("Raises via GameEventBus with a VFXTriggerPayload when this trigger fires.")]
        [SerializeField] private string outputEventName;
        [Tooltip("Should the output event be fired/triggered?")]
        [SerializeField] private bool fireOutputEvent;
        private Coroutine[] _delayedVfx;
        protected bool HasFired;

        protected virtual void Awake() {
            _delayedVfx = new Coroutine[vfxObjects.Count];
            if (autoFindVFXObjects) {
                Transform[] children = GetComponentsInChildren<Transform>();
                foreach (var child in children) {
                    VFXObject<InmersiveElements.EInducedEffect> obj = child.GetComponent<VFXObject<InmersiveElements.EInducedEffect>>();
                    if (obj) vfxObjects.Add(obj);
                }
            }
        }

        protected void Fire() {
            if (HasFired) return;
            HasFired = true;
            PlayAll();
        }

        private void PlayAll() {
            foreach (var vfx in vfxObjects) {
                if (vfx == null) continue;
                if (vfx.UseDelay) {
                    Coroutine delayed = StartCoroutine(PlayDelayedEffect(vfx));
                    _delayedVfx.Append(delayed);
                    continue;
                }
                PlayEffect(vfx);
            }
        }
        
        private IEnumerator PlayDelayedEffect<T>(VFXObject<T> vfx) {
            yield return new WaitForSeconds(vfx.Delay);
            PlayEffect(vfx);
        }

        private void PlayEffect<T>(VFXObject<T> vfx) {
            VFXManager.PlayVFXAt(vfx.Definition, vfx.Position);
            if (vfx.UseEvent && vfx.Event != null) RaiseEffectEvent(vfx);
        }

        private void RaiseEffectEvent<T>(VFXObject<T> vfx) {
            GameEventBus.Raise(GameplayEvents.VFX, vfx.Event);
        }
        
    }
}
