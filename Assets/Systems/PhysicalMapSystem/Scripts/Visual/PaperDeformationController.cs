using UnityEngine;

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PaperDeformationController : MonoBehaviour
{
    private static readonly int BendStrengthId = Shader.PropertyToID("_BendStrength");
    private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
    private static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int FoldAmountId = Shader.PropertyToID("_FoldAmount");
    private static readonly int PaperMotionSpeedId = Shader.PropertyToID("_PaperMotionSpeed");
    private static readonly int BendPointCountId = Shader.PropertyToID("_BendPointCount");
    private static readonly int BendPointsId = Shader.PropertyToID("_BendPoints");
    private static readonly int BendDirectionsId = Shader.PropertyToID("_BendDirections");

    [SerializeField] private MapSettings settings;
    [SerializeField] private bool rebuildMeshOnStart = true;

    private readonly Vector4[] bendPoints = new Vector4[4];
    private readonly Vector4[] bendDirections = new Vector4[4];
    private MaterialPropertyBlock propertyBlock;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (rebuildMeshOnStart && settings != null)
            RebuildMesh(settings);

        ApplySettings(settings);
    }

    public void ApplySettings(MapSettings newSettings)
    {
        settings = newSettings;

        if (settings == null)
            return;

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        UpdateShaderProperties(settings.windDirection, settings.windStrength);
    }

    public void RebuildMesh(MapSettings sourceSettings)
    {
        if (sourceSettings == null)
            return;

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        meshFilter.sharedMesh = BuildSubdividedMapMesh(
            sourceSettings.width,
            sourceSettings.height,
            sourceSettings.subdivisionsX,
            sourceSettings.subdivisionsY);

        if (TryGetComponent(out BoxCollider boxCollider))
            boxCollider.size = new Vector3(sourceSettings.width, sourceSettings.colliderThickness, sourceSettings.height);
    }

    public void SetWind(Vector3 direction, float strength)
    {
        if (settings == null)
            return;

        UpdateShaderProperties(direction, strength);
    }

    private void UpdateShaderProperties(Vector3 windDirection, float windStrength)
    {
        if (meshRenderer == null || propertyBlock == null)
            return;

        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(BendStrengthId, settings.bendStrength);
        propertyBlock.SetFloat(WindStrengthId, windStrength);
        propertyBlock.SetVector(WindDirectionId, windDirection);
        propertyBlock.SetFloat(NoiseScaleId, settings.noiseScale);
        propertyBlock.SetFloat(FoldAmountId, settings.foldAmount);
        propertyBlock.SetFloat(PaperMotionSpeedId, settings.paperMotionSpeed);

        int count = Mathf.Min(settings.bendPoints != null ? settings.bendPoints.Length : 0, bendPoints.Length);
        propertyBlock.SetInt(BendPointCountId, count);

        for (int i = 0; i < count; i++)
        {
            MapBendPoint point = settings.bendPoints[i];
            Vector2 direction = point.direction.sqrMagnitude > 0.0001f
                ? point.direction.normalized
                : Vector2.up;

            bendPoints[i] = new Vector4(point.position.x, point.position.y, point.intensity, point.radius);
            bendDirections[i] = new Vector4(direction.x, direction.y, 0f, 0f);
        }

        propertyBlock.SetVectorArray(BendPointsId, bendPoints);
        propertyBlock.SetVectorArray(BendDirectionsId, bendDirections);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private static Mesh BuildSubdividedMapMesh(float width, float height, int subdivisionsX, int subdivisionsY)
    {
        int xSegments = Mathf.Max(2, subdivisionsX);
        int ySegments = Mathf.Max(2, subdivisionsY);
        int vertexCountX = xSegments + 1;
        int vertexCountY = ySegments + 1;

        Vector3[] vertices = new Vector3[vertexCountX * vertexCountY];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * ySegments * 6];

        int vertexIndex = 0;
        for (int y = 0; y < vertexCountY; y++)
        {
            float v = y / (float)ySegments;
            float z = Mathf.Lerp(-height * 0.5f, height * 0.5f, v);

            for (int x = 0; x < vertexCountX; x++)
            {
                float u = x / (float)xSegments;
                float localX = Mathf.Lerp(-width * 0.5f, width * 0.5f, u);

                vertices[vertexIndex] = new Vector3(localX, 0f, z);
                uvs[vertexIndex] = new Vector2(u, v);
                vertexIndex++;
            }
        }

        int triangleIndex = 0;
        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int bottomLeft = y * vertexCountX + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + vertexCountX;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Generated Physical Map Mesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
}
