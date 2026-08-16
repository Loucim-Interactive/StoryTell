using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneManagement
{
    /// <summary>Prototype main menu and entry point into the player's current chapter.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string DefaultChapterScene = "Level1";

        [Header("Progression")]
        [SerializeField] private string fallbackChapterScene = DefaultChapterScene;

        [Header("Chapter Introduction")]
        [SerializeField] private string chapterTitle = "CHAPTER I";
        [SerializeField, TextArea(2, 4)] private string chapterDescription = "The beginning of the journey.";
        [SerializeField] private string chapterDate = "1917";
        [SerializeField] private AudioClip chapterTitleSound;
        [SerializeField, Range(0f, 1f)] private float chapterTitleSoundVolume = 1f;

        [Header("Transition Timing")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.55f;
        [SerializeField, Min(0f)] private float titlePopDuration = 0.22f;
        [SerializeField, Min(0f)] private float introductionHoldDuration = 2.5f;
        [SerializeField, Min(0f)] private float postLoadHoldDuration = 0.25f;

        private Button _continueButton;
        private CanvasGroup _fadeOverlay;
        private CanvasGroup _chapterContent;
        private RectTransform _chapterTitleRect;
        private AudioSource _audioSource;
        private GameObject _menuContentRoot;
        private bool _isLoading;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.ignoreListenerPause = true;
            CreatePrototypeMenu();
        }

        public void ContinueGame()
        {
            if (!_isLoading)
                StartCoroutine(LoadCurrentChapter());
        }

        private IEnumerator LoadCurrentChapter()
        {
            _isLoading = true;
            _continueButton.interactable = false;
            _fadeOverlay.blocksRaycasts = true;

            string targetScene = GetCurrentChapterScene();
            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError($"Cannot continue: scene '{targetScene}' is not enabled in Build Settings.");
                _continueButton.interactable = true;
                _isLoading = false;
                yield break;
            }

            yield return FadeCanvasGroup(_fadeOverlay, 0f, 1f, fadeDuration);
            yield return ShowChapterIntroduction();

            _menuContentRoot.SetActive(false);
            yield return SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);

            if (postLoadHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(postLoadHoldDuration);

            yield return FadeCanvasGroup(_fadeOverlay, 1f, 0f, fadeDuration);
            Destroy(gameObject);
        }

        private IEnumerator ShowChapterIntroduction()
        {
            _chapterContent.alpha = 1f;
            _chapterTitleRect.localScale = Vector3.one * 0.72f;

            if (chapterTitleSound != null)
                _audioSource.PlayOneShot(chapterTitleSound, chapterTitleSoundVolume);

            float elapsed = 0f;
            while (elapsed < titlePopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = titlePopDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / titlePopDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _chapterTitleRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.72f, 1f, eased);
                yield return null;
            }

            _chapterTitleRect.localScale = Vector3.one;
            if (introductionHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(introductionHoldDuration);
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
                yield return null;
            }

            group.alpha = to;
        }

        private string GetCurrentChapterScene()
        {
            // Save-data lookup belongs here when chapter persistence is implemented.
            return string.IsNullOrWhiteSpace(fallbackChapterScene) ? DefaultChapterScene : fallbackChapterScene;
        }

        private void CreatePrototypeMenu()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _menuContentRoot = new GameObject("Menu Content", typeof(RectTransform));
            _menuContentRoot.transform.SetParent(canvasObject.transform, false);
            Stretch(_menuContentRoot.GetComponent<RectTransform>());

            Image background = CreateImage("Background", _menuContentRoot.transform, new Color(0.035f, 0.045f, 0.06f, 1f));
            Stretch(background.rectTransform);

            TextMeshProUGUI title = CreateText("Title", _menuContentRoot.transform, "STORYTELL", 76, FontStyles.Bold);
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(850f, 130f));

            GameObject buttonObject = new GameObject("Continue Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_menuContentRoot.transform, false);
            SetAnchoredRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(420f, 92f));

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.78f, 0.68f, 0.42f, 1f);
            _continueButton = buttonObject.GetComponent<Button>();
            _continueButton.targetGraphic = buttonImage;
            _continueButton.onClick.AddListener(ContinueGame);

            ColorBlock colors = _continueButton.colors;
            colors.highlightedColor = new Color(0.95f, 0.84f, 0.56f, 1f);
            colors.pressedColor = new Color(0.62f, 0.52f, 0.31f, 1f);
            _continueButton.colors = colors;

            TextMeshProUGUI label = CreateText("Label", buttonObject.transform, "CONTINUE", 34, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.color = new Color(0.06f, 0.07f, 0.08f, 1f);

            Image fadeImage = CreateImage("Scene Fade", canvasObject.transform, Color.black);
            Stretch(fadeImage.rectTransform);
            fadeImage.transform.SetAsLastSibling();
            fadeImage.raycastTarget = true;
            _fadeOverlay = fadeImage.gameObject.AddComponent<CanvasGroup>();
            _fadeOverlay.alpha = 0f;
            _fadeOverlay.blocksRaycasts = false;

            var contentObject = new GameObject("Chapter Introduction", typeof(RectTransform), typeof(CanvasGroup));
            contentObject.transform.SetParent(fadeImage.transform, false);
            Stretch(contentObject.GetComponent<RectTransform>());
            _chapterContent = contentObject.GetComponent<CanvasGroup>();
            _chapterContent.alpha = 0f;

            TextMeshProUGUI chapterTitleText = CreateText("Chapter Title", contentObject.transform, chapterTitle, 72, FontStyles.Bold);
            _chapterTitleRect = chapterTitleText.rectTransform;
            SetAnchoredRect(_chapterTitleRect, new Vector2(0.5f, 0.59f), new Vector2(1200f, 130f));

            TextMeshProUGUI descriptionText = CreateText("Chapter Description", contentObject.transform, chapterDescription, 30, FontStyles.Normal);
            descriptionText.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            SetAnchoredRect(descriptionText.rectTransform, new Vector2(0.5f, 0.47f), new Vector2(1000f, 100f));

            TextMeshProUGUI dateText = CreateText("Chapter Date", contentObject.transform, chapterDate, 25, FontStyles.Italic);
            dateText.color = new Color(0.58f, 0.58f, 0.58f, 1f);
            SetAnchoredRect(dateText.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(700f, 70f));
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, FontStyles style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
