using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SceneSystem.Scripts
{
    public class SceneTransitionView : MonoBehaviour
    {
        [Header("Optional UI References")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Image blackScreen;
        [SerializeField] private TextMeshProUGUI transitionText;
        [SerializeField] private AudioSource audioSource;

        public void Initialize()
        {
            EnsureOverlay();
            HideImmediate();
        }

        public void ApplyProfile(SceneTransitionProfile profile)
        {
            EnsureOverlay();

            transitionText.text = profile.Message;
            transitionText.fontSize = profile.FontSize;
            transitionText.color = profile.FontColor;
            transitionText.rectTransform.localScale = Vector3.one;

            if (profile.Font != null)
                transitionText.font = profile.Font;
        }

        public void Show()
        {
            EnsureOverlay();
            overlayCanvas.gameObject.SetActive(true);
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = false;
        }

        public void HideImmediate()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            if (transitionText != null)
            {
                SetTextAlpha(0f);
                transitionText.rectTransform.localScale = Vector3.one;
            }

            if (overlayCanvas != null)
                overlayCanvas.gameObject.SetActive(false);
        }

        public IEnumerator FadeOverlay(float from, float to, float duration, AnimationCurve curve)
        {
            if (duration <= 0f)
            {
                overlayGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                overlayGroup.alpha = Mathf.Lerp(from, to, Evaluate(curve, t));
                yield return null;
            }

            overlayGroup.alpha = to;
        }

        public IEnumerator AnimateTextIn(SceneTransitionProfile profile)
        {
            switch (profile.TextAnimation)
            {
                case SceneTransitionAnimation.Pop:
                    yield return PopTextIn(profile);
                    break;
                default:
                    yield return FadeText(0f, 1f, profile.TextFadeInDuration, profile.TextFadeCurve);
                    break;
            }
        }

        public IEnumerator AnimateTextOut(SceneTransitionProfile profile)
        {
            transitionText.rectTransform.localScale = Vector3.one;
            yield return FadeText(1f, 0f, profile.TextFadeOutDuration, profile.TextFadeCurve);
        }

        private IEnumerator FadeText(float from, float to, float duration, AnimationCurve curve)
        {
            if (duration <= 0f)
            {
                SetTextAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetTextAlpha(Mathf.Lerp(from, to, Evaluate(curve, t)));
                yield return null;
            }

            SetTextAlpha(to);
        }

        private IEnumerator PopTextIn(SceneTransitionProfile profile)
        {
            PlayPowerSound(profile);
            SetTextAlpha(0f);
            transitionText.rectTransform.localScale = Vector3.one * profile.PopStartScale;

            if (profile.TextFadeInDuration <= 0f)
            {
                SetTextAlpha(1f);
                transitionText.rectTransform.localScale = Vector3.one;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < profile.TextFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / profile.TextFadeInDuration);
                SetTextAlpha(Mathf.Clamp01(t * 2f));
                transitionText.rectTransform.localScale = Vector3.one * Mathf.Lerp(profile.PopStartScale, 1f, Evaluate(profile.PopCurve, t));
                yield return null;
            }

            SetTextAlpha(1f);
            transitionText.rectTransform.localScale = Vector3.one;
        }

        private void PlayPowerSound(SceneTransitionProfile profile)
        {
            if (profile.PowerSound == null)
                return;

            audioSource.PlayOneShot(profile.PowerSound, profile.PowerSoundVolume);
        }

        private void SetTextAlpha(float alpha)
        {
            Color color = transitionText.color;
            color.a = alpha;
            transitionText.color = color;
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas == null)
                CreateOverlay();

            if (overlayGroup == null)
                overlayGroup = overlayCanvas.GetComponent<CanvasGroup>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        }

        private void CreateOverlay()
        {
            GameObject canvasObject = new GameObject("Scene Transition Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            overlayCanvas = canvasObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayGroup = canvasObject.GetComponent<CanvasGroup>();

            GameObject blackScreenObject = new GameObject("Black Screen", typeof(RectTransform), typeof(Image));
            blackScreenObject.transform.SetParent(canvasObject.transform, false);
            blackScreen = blackScreenObject.GetComponent<Image>();
            blackScreen.color = Color.black;
            StretchToParent(blackScreen.rectTransform);

            GameObject textObject = new GameObject("Transition Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            transitionText = textObject.GetComponent<TextMeshProUGUI>();
            transitionText.alignment = TextAlignmentOptions.Center;
            transitionText.raycastTarget = false;

            RectTransform textRect = transitionText.rectTransform;
            textRect.anchorMin = new Vector2(0.1f, 0.4f);
            textRect.anchorMax = new Vector2(0.9f, 0.6f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static float Evaluate(AnimationCurve curve, float t)
        {
            return curve != null ? curve.Evaluate(t) : t;
        }
    }
}
