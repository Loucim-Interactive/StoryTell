using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSystem.Scripts
{
    [RequireComponent(typeof(SceneTransitionView))]
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Default Transition")]
        [SerializeField] private SceneTransitionProfile defaultProfile;

        [Header("Manual Inspector Trigger")]
        [SerializeField] private bool useBuildIndex;
        [SerializeField] private string sceneName;
        [SerializeField] private int sceneBuildIndex;
        [SerializeField] private SceneTransitionProfile manualProfileOverride;

        public static SceneTransitionManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        private SceneTransitionView _view;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _view = GetComponent<SceneTransitionView>();
            _view.Initialize();
        }

        public void TransitionToScene(string targetSceneName)
        {
            TransitionToScene(targetSceneName, null);
        }

        public void TransitionToScene(string targetSceneName, SceneTransitionProfile profile)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("Scene transition cancelled because the scene name is empty.");
                return;
            }

            SceneTransitionProfile resolvedProfile = ResolveProfile(profile);
            if (resolvedProfile == null)
                return;

            StartTransition(LoadSceneRoutine(targetSceneName, resolvedProfile));
        }

        public void TransitionToScene(int targetSceneBuildIndex)
        {
            TransitionToScene(targetSceneBuildIndex, null);
        }

        public void TransitionToScene(int targetSceneBuildIndex, SceneTransitionProfile profile)
        {
            if (targetSceneBuildIndex < 0 || targetSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning($"Scene transition cancelled because build index {targetSceneBuildIndex} is outside the build settings range.");
                return;
            }

            SceneTransitionProfile resolvedProfile = ResolveProfile(profile);
            if (resolvedProfile == null)
                return;

            StartTransition(LoadSceneRoutine(targetSceneBuildIndex, resolvedProfile));
        }

        [ContextMenu("Trigger Manual Transition")]
        public void TriggerManualTransition()
        {
            SceneTransitionProfile profile = ResolveProfile(manualProfileOverride);
            if (profile == null)
                return;

            if (useBuildIndex)
                TransitionToScene(sceneBuildIndex, profile);
            else
                TransitionToScene(sceneName, profile);
        }

        public void PreviewTransition()
        {
            if (IsTransitioning)
                return;

            SceneTransitionProfile profile = ResolveProfile(manualProfileOverride);
            if (profile == null)
                return;

            StartTransition(PreviewRoutine(profile));
        }

        private void StartTransition(IEnumerator routine)
        {
            if (IsTransitioning)
                return;

            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionRoutine = StartCoroutine(routine);
        }

        private IEnumerator LoadSceneRoutine(string targetSceneName, SceneTransitionProfile profile)
        {
            yield return PlayIntro(profile);

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            yield return WaitForSceneLoad(operation);

            yield return PlayOutro(profile);
        }

        private IEnumerator LoadSceneRoutine(int targetSceneBuildIndex, SceneTransitionProfile profile)
        {
            yield return PlayIntro(profile);

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneBuildIndex);
            yield return WaitForSceneLoad(operation);

            yield return PlayOutro(profile);
        }

        private IEnumerator PreviewRoutine(SceneTransitionProfile profile)
        {
            yield return PlayIntro(profile);
            yield return PlayOutro(profile);
        }

        private IEnumerator PlayIntro(SceneTransitionProfile profile)
        {
            IsTransitioning = true;
            _view.ApplyProfile(profile);
            _view.Show();

            yield return _view.FadeOverlay(0f, 1f, profile.OverlayFadeInDuration, profile.OverlayCurve);
            yield return _view.AnimateTextIn(profile);

            if (profile.HoldDuration > 0f)
                yield return new WaitForSecondsRealtime(profile.HoldDuration);
        }

        private IEnumerator PlayOutro(SceneTransitionProfile profile)
        {
            yield return _view.AnimateTextOut(profile);
            yield return _view.FadeOverlay(1f, 0f, profile.OverlayFadeOutDuration, profile.OverlayCurve);

            _view.HideImmediate();
            IsTransitioning = false;
            _transitionRoutine = null;
        }

        private SceneTransitionProfile ResolveProfile(SceneTransitionProfile profile)
        {
            SceneTransitionProfile resolvedProfile = profile != null ? profile : defaultProfile;

            if (resolvedProfile == null)
                Debug.LogError("Scene transition requires a SceneTransitionProfile.");

            return resolvedProfile;
        }

        private static IEnumerator WaitForSceneLoad(AsyncOperation operation)
        {
            if (operation == null)
                yield break;

            while (!operation.isDone)
                yield return null;
        }
    }
}
