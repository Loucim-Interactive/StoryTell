using UnityEngine;

namespace SceneSystem.Scripts
{
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Target Scene")]
        [SerializeField] private bool useBuildIndex;
        [SerializeField] private string sceneName;
        [SerializeField] private int sceneBuildIndex;

        [Header("Transition")]
        [SerializeField] private SceneTransitionProfile profileOverride;

        [ContextMenu("Trigger Scene Transition")]
        public void Trigger()
        {
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogWarning("No SceneTransitionManager exists in the scene.");
                return;
            }

            if (useBuildIndex)
                SceneTransitionManager.Instance.TransitionToScene(sceneBuildIndex, profileOverride);
            else
                SceneTransitionManager.Instance.TransitionToScene(sceneName, profileOverride);
        }
    }
}
