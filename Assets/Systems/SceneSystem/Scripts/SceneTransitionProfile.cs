using TMPro;
using UnityEngine;

namespace SceneSystem.Scripts
{
    [CreateAssetMenu(fileName = "SceneTransitionProfile", menuName = "Scene System/Scene Transition Profile")]
    public class SceneTransitionProfile : ScriptableObject
    {
        [Header("Message")]
        [SerializeField] private string message = "Loading...";
        [SerializeField] private SceneTransitionAnimation textAnimation = SceneTransitionAnimation.Fade;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private int fontSize = 56;
        [SerializeField] private Color fontColor = Color.white;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float overlayFadeInDuration = 0.75f;
        [SerializeField, Min(0f)] private float textFadeInDuration = 0.3f;
        [SerializeField, Min(0f)] private float holdDuration = 0.5f;
        [SerializeField, Min(0f)] private float textFadeOutDuration = 0.25f;
        [SerializeField, Min(0f)] private float overlayFadeOutDuration = 0.75f;

        [Header("Curves")]
        [SerializeField] private AnimationCurve overlayCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve textFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float popStartScale = 0.75f;

        [Header("Audio")]
        [SerializeField] private AudioClip powerSound;
        [SerializeField, Range(0f, 1f)] private float powerSoundVolume = 1f;

        public string Message => message;
        public SceneTransitionAnimation TextAnimation => textAnimation;
        public TMP_FontAsset Font => font;
        public int FontSize => fontSize;
        public Color FontColor => fontColor;
        public float OverlayFadeInDuration => overlayFadeInDuration;
        public float TextFadeInDuration => textFadeInDuration;
        public float HoldDuration => holdDuration;
        public float TextFadeOutDuration => textFadeOutDuration;
        public float OverlayFadeOutDuration => overlayFadeOutDuration;
        public AnimationCurve OverlayCurve => overlayCurve;
        public AnimationCurve TextFadeCurve => textFadeCurve;
        public AnimationCurve PopCurve => popCurve;
        public float PopStartScale => popStartScale;
        public AudioClip PowerSound => powerSound;
        public float PowerSoundVolume => powerSoundVolume;
    }
}
