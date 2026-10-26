using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.VFXSystem.New {
    public abstract class VFXObject<TEffect> : MonoBehaviour, ISerializationCallbackReceiver {
        #region FIELDS
        [Header("VFX Settings")]
        [Tooltip("The asset to use for this object.")]
        [SerializeField] private VFXDefinition definition;
        [Tooltip("The delay time in which this VFX should fire.")]
        [SerializeField] private float delayTime = 0f;
        [Tooltip("Should the delay time be used?")]
        [SerializeField] private bool useDelay = false;
        
        [Header("VFX Events")]
        [Tooltip("The effects to fire, in order, when this VFX plays.")]
        [SerializeField] private List<TEffect> outputEvents = new();
        [Tooltip("Should this VFX fire its events?")]
        [SerializeField] private bool useOutputEvent = false;
        [SerializeField, HideInInspector, FormerlySerializedAs("outputEvent")] private TEffect legacyOutputEvent;
        [SerializeField, HideInInspector] private bool eventsMigrated;
        #endregion
        
        #region API
        //data
        public VFXDefinition Definition => definition;
        
        //main
        public Vector3 Position => transform.position;
        public IReadOnlyList<TEffect> Events {
            get {
                MigrateEvents();
                return outputEvents;
            }
        }
        public float Delay => delayTime;
        
        //flags
        public bool UseEvent => useOutputEvent;
        public bool UseDelay => useDelay;
        #endregion

        protected virtual void Reset() {
            outputEvents = new List<TEffect>();
            eventsMigrated = true;
        }

        public void OnBeforeSerialize() => MigrateEvents();
        public void OnAfterDeserialize() => MigrateEvents();

        private void MigrateEvents() {
            outputEvents ??= new List<TEffect>();
            if (eventsMigrated) return;
            if (outputEvents.Count == 0) outputEvents.Add(legacyOutputEvent);
            eventsMigrated = true;
        }
    }
}
