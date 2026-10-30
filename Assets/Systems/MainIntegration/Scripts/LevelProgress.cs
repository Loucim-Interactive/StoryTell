using System;
using Systems.EventSystem.Scripts;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public enum RouteStage { Camp, Outpost, Front, Approach, Battle }

    public sealed class LevelProgress : MonoBehaviour
    {
        [SerializeField] private RouteBoundary route;
        public RouteStage Stage { get; private set; }
        public event Action Changed;
        private void OnEnable() => GameEventBus.Subscribe(GameplayEvents.OnBattleTriggered, OnBattle);
        private void OnDisable() => GameEventBus.Unsubscribe(GameplayEvents.OnBattleTriggered, OnBattle);
        private void OnBattle() { Stage = RouteStage.Battle; Changed?.Invoke(); }
        public bool Reach(RouteStage stage)
        {
            if (stage <= Stage || stage >= RouteStage.Battle || (route && !route.CanProceed)) return false;
            Stage = stage;
            Changed?.Invoke();
            return true;
        }
        public void Restore(RouteStage stage)
        {
            Stage = stage >= RouteStage.Camp && stage < RouteStage.Battle ? stage : RouteStage.Camp;
            Changed?.Invoke();
        }
    }
}
