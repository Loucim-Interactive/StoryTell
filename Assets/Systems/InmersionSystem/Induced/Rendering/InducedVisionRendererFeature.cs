using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Systems.InmersionSystem.Induced.Rendering {
    public sealed class InducedVisionRendererFeature : ScriptableRendererFeature {
        [SerializeField] private Shader shader;
        private Material material;
        private VisionPass pass;
        public override void Create() {
            CoreUtils.Destroy(material);
            material = shader ? CoreUtils.CreateEngineMaterial(shader) : null;
            pass = new VisionPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing, Material = material };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            var camera = renderingData.cameraData.camera;
            if (!material || camera.cameraType != CameraType.Game) return;
            if (!camera.TryGetComponent<InducedVisionState>(out var state) || !state.isActiveAndEnabled || !state.IsActive) return;
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); }

        private sealed class PassData {
            public TextureHandle Source, History;
            public Material Material;
            public Vector4 Settings;
            public Vector4 Tunnel;
            public int Index;
        }
        private sealed class VisionPass : ScriptableRenderPass {
            public VisionPass() { requiresIntermediateTexture = true; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData) {
                var cameraData = frameData.Get<UniversalCameraData>();
                var state = cameraData.camera.GetComponent<InducedVisionState>();
                var resources = frameData.Get<UniversalResourceData>();
                if (!state || !state.IsActive || resources.isActiveTargetBackBuffer) return;
                // The feature owns the material; use per-pass properties to keep cameras isolated.
                var featureMaterial = Material;
                if (!featureMaterial) return;
                var source = resources.activeColorTexture;
                var desc = graph.GetTextureDesc(source);
                desc.clearBuffer = false;
                if (state.StepPrinting > 0) {
                    var historyDesc = cameraData.cameraTargetDescriptor;
                    historyDesc.depthBufferBits = 0;
                    historyDesc.depthStencilFormat = GraphicsFormat.None;
                    historyDesc.msaaSamples = 1;
                    if (RenderingUtils.ReAllocateHandleIfNeeded(ref state.History, historyDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Induced vision history"))
                        state.HistoryValid = false;
                    var history = graph.ImportTexture(state.History);
                    desc.name = "Step printing";
                    var printed = graph.CreateTexture(desc);
                    float interval = 1f / Mathf.Max(2, state.PrintRate);
                    float weight = state.HistoryValid ? Mathf.Min(.95f, Mathf.Exp(-interval / Mathf.Max(.02f, state.Persistence))) * state.StepPrinting : 0;
                    AddPass(graph, source, history, printed, featureMaterial, 0, new Vector4(weight, 0, 0, 0), Vector4.zero);
                    if (!state.HistoryValid || Time.time - state.LastPrintTime >= interval) {
                        AddPass(graph, printed, TextureHandle.nullHandle, history, featureMaterial, 2, Vector4.zero, Vector4.zero);
                        state.LastPrintTime = Time.time;
                        state.HistoryValid = true;
                    }
                    source = printed;
                } else state.ReleaseHistory();
                if (state.Blur > 0 || state.Tunnel > 0) {
                    desc.name = "Blur and tunnel vision";
                    var destination = graph.CreateTexture(desc);
                    AddPass(graph, source, TextureHandle.nullHandle, destination, featureMaterial, 1,
                        new Vector4(state.Blur, state.Tunnel, 0, 0),
                        new Vector4(state.ClearRadius, state.Softness, (float)cameraData.cameraTargetDescriptor.width / cameraData.cameraTargetDescriptor.height, 0));
                    source = destination;
                }
                resources.cameraColor = source;
            }
            public Material Material;
            private static void AddPass(RenderGraph graph, TextureHandle source, TextureHandle history, TextureHandle destination,
                Material material, int index, Vector4 settings, Vector4 tunnel) {
                using var builder = graph.AddRasterRenderPass<PassData>("Induced vision " + index, out var data);
                data.Source = source; data.History = history; data.Material = material;
                data.Index = index; data.Settings = settings; data.Tunnel = tunnel;
                builder.UseTexture(source, AccessFlags.Read);
                if (history.IsValid()) builder.UseTexture(history, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData d, RasterGraphContext context) => {
                    var properties = new MaterialPropertyBlock();
                    properties.SetTexture("_BlitTexture", d.Source);
                    if (d.History.IsValid()) properties.SetTexture("_History", d.History);
                    properties.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                    properties.SetVector("_Settings", d.Settings);
                    properties.SetVector("_Tunnel", d.Tunnel);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.Material, d.Index, MeshTopology.Triangles, 3, 1, properties);
                });
            }
        }
    }
}
