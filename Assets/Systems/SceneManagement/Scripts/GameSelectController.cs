using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneManagement
{
    /// <summary>Campaign selector ported from Tell-Tale and adapted to StoryTell's single playable scene.</summary>
    public sealed class GameSelectController : MonoBehaviour
    {
        private const string LayoutResource = "StoryTellMainMenu/GameSelect";
        private const string ThemeResource = "StoryTellMainMenu/MainMenuTheme";
        private const string RuntimeThemeResource = "StoryTellMainMenu/RuntimeTheme";
        private const string NotebookResource = "StoryTellMainMenu/CampaignNotebook";
        private const string CampaignPhotoResource = "StoryTellMainMenu/MalvinasPhoto";

        [Header("Navigation")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string firstLevelScene = "Level1";

        [Header("Scene Introduction")]
        [SerializeField] private string chapterTitle = "ESCENA 1";
        [SerializeField] private string chapterDescription = "TRINCHERA ARGENTINA";
        [SerializeField] private string chapterDate = "17:38 · ATLÁNTICO SUR · 1982";
        [SerializeField, Min(0f)] private float fadeDuration = 0.55f;
        [SerializeField, Min(0f)] private float titlePopDuration = 0.22f;
        [SerializeField, Min(0f)] private float introductionHoldDuration = 2.5f;

        private Button _startButton;
        private Button _backButton;
        private VisualElement _fadeOverlay;
        private VisualElement _chapterContent;
        private VisualElement _chapterTitle;
        private bool _isLoading;

        private void Awake()
        {
            CreateDocument();
        }

        private IEnumerator Start()
        {
            yield return null;
            BindUI();
        }

        private void Update()
        {
            if (!_isLoading && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                BackToMainMenu();
        }

        private void OnDestroy()
        {
            if (_startButton != null) _startButton.clicked -= StartCampaign;
            if (_backButton != null) _backButton.clicked -= BackToMainMenu;
        }

        private void CreateDocument()
        {
            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
            if (layout == null)
            {
                Debug.LogError($"No se encontró Resources/{LayoutResource}.", this);
                enabled = false;
                return;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "StoryTell Game Select Panel";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;

            ThemeStyleSheet runtimeTheme = Resources.Load<ThemeStyleSheet>(RuntimeThemeResource);
            if (runtimeTheme == null)
            {
                Debug.LogError($"No se encontró Resources/{RuntimeThemeResource}.", this);
                enabled = false;
                return;
            }

            settings.themeStyleSheet = runtimeTheme;

            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = layout;
            document.sortingOrder = 100;
        }

        private void BindUI()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("GameSelect no pudo crear su árbol visual.", this);
                enabled = false;
                return;
            }

            StyleSheet theme = Resources.Load<StyleSheet>(ThemeResource);
            if (theme != null && !root.styleSheets.Contains(theme))
                root.styleSheets.Add(theme);

            Image notebook = Require<Image>(root, "notebook-background");
            Image campaignPhoto = Require<Image>(root, "campaign-photo");
            _startButton = Require<Button>(root, "start-button");
            _backButton = Require<Button>(root, "back-button");
            _fadeOverlay = Require<VisualElement>(root, "fade-overlay");
            _chapterContent = Require<VisualElement>(root, "chapter-content");
            _chapterTitle = Require<Label>(root, "chapter-title");

            if (!enabled) return;

            Texture2D notebookTexture = Resources.Load<Texture2D>(NotebookResource);
            Texture2D campaignTexture = Resources.Load<Texture2D>(CampaignPhotoResource);
            if (notebookTexture == null || campaignTexture == null)
            {
                Debug.LogError("Faltan las imágenes CampaignNotebook o MalvinasPhoto en Resources.", this);
                enabled = false;
                return;
            }

            notebook.image = notebookTexture;
            notebook.scaleMode = ScaleMode.StretchToFill;
            campaignPhoto.image = campaignTexture;
            campaignPhoto.scaleMode = ScaleMode.ScaleToFit;

            root.Q<Label>("chapter-title").text = chapterTitle;
            root.Q<Label>("chapter-description").text = chapterDescription;
            root.Q<Label>("chapter-date").text = chapterDate;

            _startButton.clicked += StartCampaign;
            _backButton.clicked += BackToMainMenu;
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.pickingMode = PickingMode.Ignore;
            _chapterContent.style.opacity = 0f;
            _startButton.Focus();
        }

        private T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                Debug.LogError($"Falta el elemento '{name}' en GameSelect.", this);
                enabled = false;
            }

            return element;
        }

        private void StartCampaign()
        {
            if (!_isLoading)
                StartCoroutine(LoadFirstLevel());
        }

        private void BackToMainMenu()
        {
            if (!_isLoading)
                StartCoroutine(LoadWithoutIntroduction(mainMenuScene));
        }

        private IEnumerator LoadFirstLevel()
        {
            _isLoading = true;
            SetButtonsEnabled(false);

            if (!CanLoad(firstLevelScene))
            {
                RestoreNavigation();
                yield break;
            }

            _fadeOverlay.pickingMode = PickingMode.Position;
            yield return FadeElement(_fadeOverlay, 0f, 1f, fadeDuration);
            yield return ShowChapterIntroduction();
            yield return SceneManager.LoadSceneAsync(firstLevelScene, LoadSceneMode.Single);
        }

        private IEnumerator LoadWithoutIntroduction(string sceneName)
        {
            _isLoading = true;
            SetButtonsEnabled(false);

            if (!CanLoad(sceneName))
            {
                RestoreNavigation();
                yield break;
            }

            _fadeOverlay.pickingMode = PickingMode.Position;
            yield return FadeElement(_fadeOverlay, 0f, 1f, fadeDuration);
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private bool CanLoad(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
                return true;

            Debug.LogError($"La escena '{sceneName}' no está habilitada en Build Settings.", this);
            return false;
        }

        private void RestoreNavigation()
        {
            _isLoading = false;
            SetButtonsEnabled(true);
            _fadeOverlay.pickingMode = PickingMode.Ignore;
        }

        private void SetButtonsEnabled(bool value)
        {
            _startButton?.SetEnabled(value);
            _backButton?.SetEnabled(value);
        }

        private IEnumerator ShowChapterIntroduction()
        {
            _chapterContent.style.opacity = 1f;
            _chapterTitle.style.scale = new Scale(Vector3.one * 0.72f);

            float elapsed = 0f;
            while (elapsed < titlePopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = titlePopDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / titlePopDuration);
                float scale = Mathf.LerpUnclamped(0.72f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                _chapterTitle.style.scale = new Scale(Vector3.one * scale);
                yield return null;
            }

            _chapterTitle.style.scale = new Scale(Vector3.one);
            if (introductionHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(introductionHoldDuration);
        }

        private static IEnumerator FadeElement(VisualElement element, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                element.style.opacity = to;
                yield break;
            }

            float elapsed = 0f;
            element.style.opacity = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                element.style.opacity = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
                yield return null;
            }

            element.style.opacity = to;
        }
    }
}
