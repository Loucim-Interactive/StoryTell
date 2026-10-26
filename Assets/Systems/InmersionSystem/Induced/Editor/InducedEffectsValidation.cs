using System;
using InmersionSystem.Induced.Configs.Base;
using Systems.InmersionSystem.Induced.Configs;
using Systems.InmersionSystem.Induced.Effects;
using Systems.InmersionSystem.Induced.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Systems.InmersionSystem.Induced.Editor {
    public static class InducedEffectsValidation {
        [MenuItem("Tools/Inmersion/Validate Induced Effects")]
        public static void Validate() {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before validation.");
            foreach (var oldCamera in Resources.FindObjectsOfTypeAll<Camera>()) {
                if (oldCamera.name == "Induced effects validation camera" && EditorSceneManager.IsPreviewScene(oldCamera.gameObject.scene))
                    EditorSceneManager.ClosePreviewScene(oldCamera.gameObject.scene);
            }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Systems/InmersionSystem/Induced/Rendering/InducedVision.shader");
            Require(shader && !ShaderUtil.ShaderHasError(shader), "Shader must compile.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/FirstPersonControllerSystem/Prefabs/Player.prefab");
            var handler = new SerializedObject(prefab.GetComponentInChildren<InmersionHandlerScript>(true));
            foreach (var field in new[] { "blurryVisionConfig", "tunnelVisionConfig", "stepPrintingConfig", "muffledHearingConfig" })
                Require(handler.FindProperty(field).objectReferenceValue, "Missing player config: " + field);
            foreach (var path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" }) {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                Require(renderer.rendererFeatures.Exists(f => f is InducedVisionRendererFeature && f.isActive), "Missing renderer feature: " + path);
            }

            var scene = EditorSceneManager.NewPreviewScene();
            var go = new GameObject("Induced effects validation camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.cullingMask = 0;
            camera.allowHDR = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var state = go.AddComponent<InducedVisionState>();
            go.AddComponent<AudioListener>();
            var filter = go.AddComponent<AudioLowPassFilter>();
            filter.enabled = false;
            filter.cutoffFrequency = 17000;
            var blur = Config<BlurryVisionConfig>();
            var tunnel = Config<TunnelVisionConfig>();
            var step = Config<StepPrintingConfig>();
            var muffle = Config<MuffledHearingConfig>();
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var panelMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            try {
                var blurEffect = new BlurryVisionEffect(blur, state);
                var tunnelEffect = new TunnelVisionEffect(tunnel, state);
                var stepEffect = new StepPrintingEffect(step, state);
                var audioEffect = new MuffledHearingEffect(muffle, filter);
                Require(blurEffect.EffectRoutine(.5f).MoveNext() && Mathf.Approximately(state.Blur, blur.blurRadius * .5f), "Blur scales with intensity.");
                Require(tunnelEffect.EffectRoutine(1).MoveNext() && state.Blur > 0 && state.Tunnel == 1, "Visual effects coexist.");
                blurEffect.Reset();
                Require(state.Blur == 0 && state.Tunnel == 1, "Stopping blur preserves tunnel vision.");
                Render(camera, target, pixels);
                Require(pixels.GetPixel(64, 64).r > .8f && pixels.GetPixel(2, 2).r < .2f, "Tunnel must darken edges and preserve center.");
                tunnelEffect.Reset();
                Render(camera, target, pixels);
                Require(pixels.GetPixel(2, 2).r > .8f, "Tunnel restores normal rendering.");
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.cullingMask = ~0;
                var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(panel, scene);
                panel.transform.position = new Vector3(.5f, 0, 2);
                panel.transform.localScale = new Vector3(1, 2, 1);
                panelMaterial.SetColor("_BaseColor", Color.black);
                panel.GetComponent<Renderer>().sharedMaterial = panelMaterial;
                Render(camera, target, pixels);
                float sharpEdge = pixels.GetPixel(62, 64).r;
                blurEffect.EffectRoutine(1).MoveNext();
                Render(camera, target, pixels);
                Require(pixels.GetPixel(62, 64).r < sharpEdge - .02f, "Blur spreads a high contrast edge.");
                blurEffect.Reset();
                panel.SetActive(false);
                stepEffect.EffectRoutine(1).MoveNext();
                Render(camera, target, pixels);
                camera.backgroundColor = Color.black;
                Render(camera, target, pixels);
                Require(pixels.GetPixel(64, 64).r > .1f, "Step printing must retain the previous image.");
                stepEffect.Reset();
                Render(camera, target, pixels);
                Require(pixels.GetPixel(64, 64).r < .05f, "Stopping step printing clears its history.");
                audioEffect.EffectRoutine(1).MoveNext();
                Require(filter.enabled && Mathf.Abs(filter.cutoffFrequency - muffle.cutoffFrequency) < 1, "Muffling applies configured cutoff.");
                audioEffect.Reset();
                Require(!filter.enabled && Mathf.Abs(filter.cutoffFrequency - 17000) < 1, "Muffling restores disabled filter and original cutoff.");
                filter.enabled = true;
                filter.cutoffFrequency = 4500;
                audioEffect.EffectRoutine(.5f).MoveNext();
                audioEffect.Reset();
                Require(filter.enabled && Mathf.Abs(filter.cutoffFrequency - 4500) < 1, "Muffling preserves an existing filter.");
                Require(!new BlurryVisionEffect(blur, state).EffectRoutine(0).MoveNext() && state.Blur == 0, "Zero intensity does not start an effect.");
                Require(!ShaderUtil.ShaderHasError(shader), "Shader compiled after rendering.");
                Debug.Log("[InducedEffectsValidation] PASS: asset wiring, intensity, simultaneous effects, blur rendering, tunnel rendering/restoration, temporal history/restoration, audio filter/restoration, zero intensity.");
            } finally {
                state.ReleaseHistory();
                Object.DestroyImmediate(blur); Object.DestroyImmediate(tunnel); Object.DestroyImmediate(step); Object.DestroyImmediate(muffle);
                Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(panelMaterial);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        private static T Config<T>() where T : TimedInducedEffectConfig {
            var config = ScriptableObject.CreateInstance<T>();
            config.enterDuration = 0; config.holdDuration = 2;
            return config;
        }
        private static void Render(Camera camera, RenderTexture target, Texture2D pixels) {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active;
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply(); }
            finally { RenderTexture.active = previous; }
        }
        private static void Require(bool condition, string message) {
            if (!condition) throw new InvalidOperationException("[InducedEffectsValidation] " + message);
        }
    }
}
