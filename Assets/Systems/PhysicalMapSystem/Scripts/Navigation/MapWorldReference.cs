using UnityEngine;

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
public sealed class MapWorldReference : MonoBehaviour
{
    [Header("World Area")]
    [Tooltip("Transform placed at the center of the area represented by the map.")]
    [SerializeField] private Transform worldOrigin;
    [SerializeField] private Vector2 worldSize = new Vector2(100f, 100f);

    [Header("Map Orientation")]
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;
    [SerializeField] private bool clampToMap = true;
    [Tooltip("Signed offset from the paper surface. Negative values place markers on the side facing the default camera pose.")]
    [SerializeField] private float markerHeight = -0.055f;

    [Header("References")]
    [SerializeField] private PhysicalMapController mapController;

    public Transform WorldOrigin => worldOrigin;
    public Vector2 WorldSize => worldSize;
    public bool IsConfigured => worldOrigin != null && mapController != null;

    private void Awake()
    {
        ResolveMapController();
    }

    public bool TryWorldToMapPosition(
        Vector3 worldPosition,
        out Vector3 mapWorldPosition,
        out bool isInside)
    {
        mapWorldPosition = transform.position;
        isInside = false;

        ResolveMapController();

        if (worldOrigin == null ||
            mapController == null ||
            mapController.Settings == null)
        {
            return false;
        }

        Vector3 offset = worldPosition - worldOrigin.position;
        float horizontal = Vector3.Dot(offset, worldOrigin.right);
        float vertical = Vector3.Dot(offset, worldOrigin.forward);

        float u = horizontal / worldSize.x + 0.5f;
        float v = vertical / worldSize.y + 0.5f;

        if (invertHorizontal)
            u = 1f - u;

        if (invertVertical)
            v = 1f - v;

        isInside = u >= 0f && u <= 1f && v >= 0f && v <= 1f;

        if (clampToMap)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
        }

        MapSettings settings = mapController.Settings;
        Vector3 localPosition = new Vector3(
            Mathf.Lerp(-settings.width * 0.5f, settings.width * 0.5f, u),
            markerHeight,
            Mathf.Lerp(-settings.height * 0.5f, settings.height * 0.5f, v));

        // The visible side changes with the hold pose. Always place markers toward
        // the reader instead of assuming the negative normal faces the camera.
        if (mapController.ViewAnchor != null)
        {
            float side = Mathf.Sign(Vector3.Dot(mapController.ViewAnchor.position - transform.position, transform.up));
            localPosition.y = Mathf.Abs(markerHeight) * (side == 0 ? 1 : side);
        }
        mapWorldPosition = transform.TransformPoint(localPosition);
        return true;
    }

    public void SetWorldOrigin(Transform origin)
    {
        worldOrigin = origin;
    }

    public void SetWorldSize(Vector2 size)
    {
        worldSize = new Vector2(
            Mathf.Max(0.01f, size.x),
            Mathf.Max(0.01f, size.y));
    }

    public void Configure(Transform origin, Vector2 size, PhysicalMapController controller = null)
    {
        worldOrigin = origin;
        SetWorldSize(size);

        if (controller != null)
            mapController = controller;

        ResolveMapController();
    }

    private void ResolveMapController()
    {
        if (mapController == null)
            mapController = GetComponentInParent<PhysicalMapController>();
    }

    private void OnValidate()
    {
        worldSize.x = Mathf.Max(0.01f, worldSize.x);
        worldSize.y = Mathf.Max(0.01f, worldSize.y);
        ResolveMapController();
    }

    private void OnDrawGizmosSelected()
    {
        if (worldOrigin == null)
            return;

        Vector3 halfRight = worldOrigin.right * (worldSize.x * 0.5f);
        Vector3 halfForward = worldOrigin.forward * (worldSize.y * 0.5f);

        Vector3 bottomLeft = worldOrigin.position - halfRight - halfForward;
        Vector3 bottomRight = worldOrigin.position + halfRight - halfForward;
        Vector3 topRight = worldOrigin.position + halfRight + halfForward;
        Vector3 topLeft = worldOrigin.position - halfRight + halfForward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        Gizmos.DrawWireSphere(worldOrigin.position, 0.2f);
    }
}
}
