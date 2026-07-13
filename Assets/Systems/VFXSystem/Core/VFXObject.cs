using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.VFXSystem.New {
    public abstract class VFXObject<TEffect> : MonoBehaviour {
        #region FIELDS
        [Header("VFX Settings")]
        [Tooltip("The asset to use for this object.")]
        [SerializeField] private VFXDefinition definition;
        [Tooltip("The delay time in which this VFX should fire.")]
        [SerializeField] private float delayTime = 0f;
        [Tooltip("Should the delay time be used?")]
        [SerializeField] private bool useDelay = false;
        
        [Header("VFX Event")]
        [Tooltip("The event to fire when used.")]
        [SerializeField] private TEffect outputEvent;
        [Tooltip("Should this VFX fire the event?")]
        [SerializeField] private bool useOutputEvent = false;
        #endregion
        
        #region API
        //data
        public VFXDefinition Definition => definition;
        
        //main
        public Vector3 Position => transform.position;
        public TEffect Event => outputEvent;
        public float Delay => delayTime;
        
        //flags
        public bool UseEvent => useOutputEvent;
        public bool UseDelay => useDelay;
        #endregion
    }
}
