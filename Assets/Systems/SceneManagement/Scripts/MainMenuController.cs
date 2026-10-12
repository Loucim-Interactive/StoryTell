using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SceneManagement
{
    /// <summary>Entry point for the StoryTell flow: MainMenu -> GameSelect.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string DefaultGameSelectScene = "GameSelect";
        private const string LayoutResource = "StoryTellMainMenu/MainMenu";
        private const string ThemeResource = "StoryTellMainMenu/MainMenuTheme";
        private const string RuntimeThemeResource = "StoryTellMainMenu/RuntimeTheme";
        private const string BackgroundResource = "StoryTellMainMenu/MainMenuBackground";
        private const string VolumePreference = "storytell.master-volume";
        private const string FullscreenPreference = "storytell.fullscreen";

        [Header("Navigation")]
        [SerializeField] private string gameSelectScene = DefaultGameSelectScene;
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

        private Button _continueButton;
        private Button _newGameButton;
        private Button _optionsButton;
        private Button _exitButton;
        private Button _closeOptionsButton;
        private VisualElement _optionsOverlay;
        private VisualElement _fadeOverlay;
        private Slider _volumeSlider;
        private Toggle _fullscreenToggle;
        private bool _isLoading;

        private void Awake()
        {
            CreateMenuDocument();
        }

        private IEnumerator Start()
        {
            yield return null;
            BindMenu();
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame &&
                _optionsOverlay != null &&
                _optionsOverlay.style.display == DisplayStyle.Flex)
            {
                HideOptions();
            }
        }

        private void OnDestroy()
        {
            if (_continueButton != null) _continueButton.clicked -= OpenGameSelect;
            if (_newGameButton != null) _newGameButton.clicked -= OpenGameSelect;
            if (_optionsButton != null) _optionsButton.clicked -= ShowOptions;
            if (_exitButton != null) _exitButton.clicked -= ExitGame;
            if (_closeOptionsButton != null) _closeOptionsButton.clicked -= HideOptions;
        }

        public void ContinueGame() => OpenGameSelect();

        public void StartNewGame() => OpenGameSelect();

        private void OpenGameSelect()
        {
            if (!_isLoading)
                StartCoroutine(LoadGameSelect());
        }

        private IEnumerator LoadGameSelect()
        {
            _isLoading = true;
            SetNavigationEnabled(false);

            string sceneName = string.IsNullOrWhiteSpace(gameSelectScene)
                ? DefaultGameSelectScene
                : gameSelectScene;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"No se puede abrir el selector: la escena '{sceneName}' no está habilitada en Build Settings.",
                    this);
                SetNavigationEnabled(true);
                _isLoading = false;
                yield break;
            }

            _fadeOverlay.pickingMode = PickingMode.Position;
            yield return FadeElement(_fadeOverlay, 0f, 1f, fadeDuration);
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
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

        private void CreateMenuDocument()
        {
            VisualTreeAsset layout = Resources.Load<VisualTreeAsset>(LayoutResource);
            if (layout == null)
            {
                Debug.LogError($"No se encontró Resources/{LayoutResource}.", this);
                enabled = false;
                return;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "StoryTell Main Menu Panel";
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

        private void BindMenu()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("El menú principal no pudo crear su árbol visual.", this);
                enabled = false;
                return;
            }

            StyleSheet theme = Resources.Load<StyleSheet>(ThemeResource);
            if (theme != null && !root.styleSheets.Contains(theme))
                root.styleSheets.Add(theme);

            Texture2D backgroundTexture = Resources.Load<Texture2D>(BackgroundResource);
            VisualElement background = root.Q<VisualElement>(className: "main-menu__background");
            if (background != null && backgroundTexture != null)
                background.style.backgroundImage = new StyleBackground(backgroundTexture);

            _continueButton = Require<Button>(root, "continue-button");
            _newGameButton = Require<Button>(root, "new-game-button");
            _optionsButton = Require<Button>(root, "options-button");
            _exitButton = Require<Button>(root, "exit-button");
            _closeOptionsButton = Require<Button>(root, "close-options-button");
            _optionsOverlay = Require<VisualElement>(root, "options-overlay");
            _fadeOverlay = Require<VisualElement>(root, "fade-overlay");
            _volumeSlider = Require<Slider>(root, "volume-slider");
            _fullscreenToggle = Require<Toggle>(root, "fullscreen-toggle");

            if (!enabled) return;

            _continueButton.clicked += OpenGameSelect;
            _newGameButton.clicked += OpenGameSelect;
            _optionsButton.clicked += ShowOptions;
            _exitButton.clicked += ExitGame;
            _closeOptionsButton.clicked += HideOptions;
            _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
            _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);

            LoadPreferences();
            _optionsOverlay.style.display = DisplayStyle.None;
            _optionsOverlay.pickingMode = PickingMode.Ignore;
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.pickingMode = PickingMode.Ignore;
            _continueButton.Focus();
        }

        private T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                Debug.LogError($"Falta el elemento '{name}' en el menú principal.", this);
                enabled = false;
            }

            return element;
        }

        private void LoadPreferences()
        {
            float volume = PlayerPrefs.GetFloat(VolumePreference, AudioListener.volume);
            bool fullscreen = PlayerPrefs.GetInt(FullscreenPreference, Screen.fullScreen ? 1 : 0) == 1;
            _volumeSlider.SetValueWithoutNotify(volume);
            _fullscreenToggle.SetValueWithoutNotify(fullscreen);
            AudioListener.volume = volume;
        }

        private void ShowOptions()
        {
            _optionsOverlay.style.display = DisplayStyle.Flex;
            _optionsOverlay.pickingMode = PickingMode.Position;
            _volumeSlider.Focus();
        }

        private void HideOptions()
        {
            _optionsOverlay.style.display = DisplayStyle.None;
            _optionsOverlay.pickingMode = PickingMode.Ignore;
            _optionsButton.Focus();
        }

        private static void OnVolumeChanged(ChangeEvent<float> evt)
        {
            AudioListener.volume = evt.newValue;
            PlayerPrefs.SetFloat(VolumePreference, evt.newValue);
            PlayerPrefs.Save();
        }

        private static void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            Screen.fullScreen = evt.newValue;
            PlayerPrefs.SetInt(FullscreenPreference, evt.newValue ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SetNavigationEnabled(bool value)
        {
            _continueButton?.SetEnabled(value);
            _newGameButton?.SetEnabled(value);
            _optionsButton?.SetEnabled(value);
            _exitButton?.SetEnabled(value);
        }

        private static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
