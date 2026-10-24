using UnityEngine;
namespace StoryTell.GameplayPath
{
    public sealed class GameplayPathVfxGate : MonoBehaviour
    {
        [SerializeField] private GameplayPathController controller;
        [SerializeField] private Collider[] explosionTriggers;
        private bool allowed;
        private void Start() { Apply(false); }
        private void Update()
        {
            bool next = controller.Progress.Stage >= PathStage.ApproachBattle && controller.Progress.Stage < PathStage.Complete;
            if (next != allowed) Apply(next);
        }
        private void Apply(bool next)
        {
            allowed = next;
            foreach (var trigger in explosionTriggers) if (trigger != null) trigger.enabled = next;
        }
    }
}
