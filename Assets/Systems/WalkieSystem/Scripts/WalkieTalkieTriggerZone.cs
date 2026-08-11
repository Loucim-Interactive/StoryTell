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
            GameEventBus.Raise(GameplayEvents.WalkieTalkieTrigger, decisionAsset); // we send the decision
            GameEventBus.Raise(GameplayEvents.WalkieTalkieTrigger, WalkieInteractionMachine.WalkieInteractionStates.Awaiting); // and send the asset
            Debug.Log("Triggered walkie talkie interaction");
        }
    }
}
