using UnityEngine;

namespace SceneSystem.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class LevelEndTrigger : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("Name or full asset path of a scene enabled in the build scene list.")]
        [SerializeField] private string sceneName = "MainMenu";

        [Header("Activation")]
        [SerializeField] private bool triggerOnPlayerEnter = true;

        [Header("Transition")]
        [Tooltip("Uses the existing manager's default when empty. Required to create a manager automatically.")]
        [SerializeField] private SceneTransitionProfile profileOverride;
        [SerializeField] private bool createManagerIfMissing = true;

        private bool _hasEnded;

        private void Reset()
        {
            BoxCollider zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(4f, 3f, 2f);
            zone.center = new Vector3(0f, 1.5f, 0f);
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnPlayerEnter || !isActiveAndEnabled)
                return;

            // Child colliders also count when the Player tag lives on their parent.
            for (Transform candidate = other.transform; candidate != null; candidate = candidate.parent)
            {
                if (!candidate.CompareTag("Player"))
                    continue;

                EndLevel();
                return;
            }
        }

        /// <summary>Can also be connected to a UnityEvent, dialogue event, or UI button.</summary>
        [ContextMenu("End Level")]
        public void EndLevel()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || _hasEnded)
                return;

            SceneTransitionManager manager = SceneTransitionManager.Instance;
            if (manager != null && manager.IsTransitioning)
                return;

            // Validate before the overlay appears so a bad destination cannot strand the player.
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Level ending cancelled: scene '{sceneName}' must be enabled in the build scene list.", this);
                return;
            }

            if (manager == null)
            {
                if (!createManagerIfMissing || profileOverride == null)
                {
                    Debug.LogError("Level ending requires a SceneTransitionManager, or automatic creation with a transition profile assigned.", this);
                    return;
                }

                // Keep the persistent manager separate so the ending zone unloads with its level.
                manager = new GameObject("Scene Transition Manager").AddComponent<SceneTransitionManager>();
            }

            manager.TransitionToScene(sceneName, profileOverride);
            _hasEnded = manager.IsTransitioning;
        }
    }
}
