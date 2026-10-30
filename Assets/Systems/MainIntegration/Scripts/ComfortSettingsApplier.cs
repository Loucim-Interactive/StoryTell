using System;
using Systems.FirstPersonControllerSystem.Scripts.ControllerSys.Camera;
using TMPro;
using UnityEngine;

namespace StoryTell.MainIntegration
{
    public sealed class ComfortSettingsApplier : MonoBehaviour
    {
        [Serializable] public struct TextSize { public TMP_Text text; public float size; }
        [SerializeField] private CameraControllerScript look;
        [SerializeField] private TextSize[] texts;
        private float originalVolume;
        private void OnEnable(){originalVolume=AudioListener.volume;ComfortSettings.Changed+=Apply;Apply();}
        private void OnDisable(){ComfortSettings.Changed-=Apply;AudioListener.volume=originalVolume;}
        private void Apply()
        {
            var settings=ComfortSettings.Current;
            look.SetComfort(settings.sensitivity,settings.motion);
            AudioListener.volume=settings.master;
            // Texts own their scaling so runtime clones inherit the same original size.
        }
    }
}
