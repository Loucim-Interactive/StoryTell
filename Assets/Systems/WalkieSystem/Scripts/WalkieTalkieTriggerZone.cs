using System.Collections;
using System.Collections.Generic;
using Systems.DecisionSystem;
using Systems.EventSystem.Scripts;
using Systems.Global;
using UnityEngine;

namespace Systems.WalkieSystem.Scripts {
    public class WalkieTalkieTriggerZone : TriggerZoneScript
    {
        [Header("Walkie Talkie Zone settings")]
        [SerializeField] private WalkieDecisionAsset decisionAsset;
        
        protected override void Activate() {
            GameEventBus.Raise(DecisionEvents.Requested, decisionAsset);
            Debug.Log("Activated walkie talkie zone");
        }
    }
}
