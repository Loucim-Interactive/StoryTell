using System;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace Systems.VFXSystem.New.Triggers {
    public class VFXEventTrigger : VFXTriggerBase
    {
        public GameplayEventType eventType;
        
        public void OnEnable() {
            GameEventBus.Subscribe(GameplayEvents.GetName(eventType), Fire);
        }
        
        public void OnDisable() {
            GameEventBus.Unsubscribe(GameplayEvents.GetName(eventType), Fire);
        }
    }
}
