using UnityEngine;

namespace StoryTell.MainIntegration
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RouteProgressZone : MonoBehaviour
    {
        [SerializeField] private LevelProgress progress;
        [SerializeField] private Transform player;
        [SerializeField] private RouteStage stage;
        private void OnTriggerEnter(Collider other)
        {
            if (player && (other.transform == player || other.transform.IsChildOf(player))) progress.Reach(stage);
        }
    }
}
