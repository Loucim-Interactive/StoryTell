using UnityEngine;
using UnityEngine.UI;

namespace StoryTell.MainIntegration
{
    /// <summary>Resolution-independent peripheral dimming with a readable central area.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeaponVignetteGraphic : MaskableGraphic
    {
        [SerializeField, Range(0, 1)] private float intensity;
        public float Intensity
        {
            get => intensity;
            set { value = Mathf.Clamp01(value); if (Mathf.Abs(value - intensity) < 0.001f) return; intensity = value; SetVerticesDirty(); }
        }
        protected override void OnEnable() { useLegacyMeshGeneration = false; base.OnEnable(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int columns = 32, rows = 18;
            Rect rect = GetPixelAdjustedRect();
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns, v = y / (float)rows;
                float radius = new Vector2(u * 2 - 1, v * 2 - 1).magnitude;
                float falloff = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.18f, 1.08f, radius));
                Color tint = Color.black;
                tint.a = intensity * Mathf.Lerp(0.08f, 1, falloff);
                mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u), Mathf.Lerp(rect.yMin, rect.yMax, v)), tint, new Vector2(u, v));
            }
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int i = y * (columns + 1) + x;
                mesh.AddTriangle(i, i + columns + 1, i + 1);
                mesh.AddTriangle(i + 1, i + columns + 1, i + columns + 2);
            }
        }
    }
}
