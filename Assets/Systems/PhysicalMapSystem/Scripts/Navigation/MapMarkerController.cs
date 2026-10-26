using UnityEngine;

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
public sealed class MapMarkerController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private MapWorldReference worldReference;
    [SerializeField] private PhysicalMapController mapController;
    [SerializeField] private Transform markerVisual;

    [Header("World Target")]
    [SerializeField] private Transform trackedTarget;
    [SerializeField] private bool useMainCameraAsFallback;
    [SerializeField] private Vector3 fixedWorldPosition;

    [Header("Visibility")]
    [SerializeField] private bool visible = true;
    [SerializeField] private bool hideWhenOutsideMap;

    private Renderer[] markerRenderers;
    private bool mappingAvailable;
    private bool targetInsideMap;

    public bool IsVisible => visible;
    public bool IsInsideMap => targetInsideMap;
    public Transform TrackedTarget => trackedTarget;

    private void Awake()
    {
        ResolveReferences();
        CacheRenderers();
    }

    private void OnEnable()
    {
        SubscribeToMap();
    }

    private void Start()
    {
        ResolveFallbackTarget();

        UpdateMarker();
        RefreshVisibility();
    }

    private void LateUpdate()
    {
        if (!visible || (mapController != null && !mapController.IsOpen))
            return;

        ResolveFallbackTarget();
        UpdateMarker();
    }

    public void Track(Transform target)
    {
        trackedTarget = target;
        UpdateMarker();
        RefreshVisibility();
    }

    public void Configure(
        MapWorldReference reference,
        PhysicalMapController controller,
        Transform target,
        bool useCameraFallback = false)
    {
        worldReference = reference;
        mapController = controller;
        trackedTarget = target;
        useMainCameraAsFallback = useCameraFallback;
        ResolveFallbackTarget();
        UpdateMarker();
    }

    public void SetHideWhenOutsideMap(bool value)
    {
        hideWhenOutsideMap = value;
        RefreshVisibility();
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        trackedTarget = null;
        fixedWorldPosition = worldPosition;
        UpdateMarker();
        RefreshVisibility();
    }

    public void SetVisible(bool value)
    {
        visible = value;
        RefreshVisibility();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void UpdateMarker()
    {
        ResolveReferences();

        if (worldReference == null || markerVisual == null)
        {
            mappingAvailable = false;
            RefreshVisibility();
            return;
        }

        Vector3 worldPosition = trackedTarget != null
            ? trackedTarget.position
            : fixedWorldPosition;

        mappingAvailable = worldReference.TryWorldToMapPosition(
            worldPosition,
            out Vector3 mapPosition,
            out targetInsideMap);

        if (mappingAvailable)
            markerVisual.position = mapPosition;

        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool mapIsOpen = mapController == null || mapController.IsOpen;
        bool shouldShow =
            visible &&
            mapIsOpen &&
            mappingAvailable &&
            (!hideWhenOutsideMap || targetInsideMap);

        // Refresh the cache here as scene markers can be added to a prefab
        // instance after its map controller has already initialized.
        CacheRenderers();

        for (int i = 0; i < markerRenderers.Length; i++)
        {
            if (markerRenderers[i] != null)
                markerRenderers[i].enabled = shouldShow;
        }
    }

    private void ResolveReferences()
    {
        if (mapController == null)
            mapController = GetComponentInParent<PhysicalMapController>();

        if (worldReference == null)
            worldReference = GetComponentInParent<MapWorldReference>();

        if (markerVisual == null)
            markerVisual = transform;
    }

    private void CacheRenderers()
    {
        Transform source = markerVisual != null ? markerVisual : transform;
        markerRenderers = source.GetComponentsInChildren<Renderer>(true);

        // A marker normally keeps its renderer on the same GameObject. Keeping
        // this fallback makes visibility reliable even if Unity's component
        // initialization order temporarily returns an empty child query.
        if (markerRenderers.Length == 0)
        {
            Renderer ownRenderer = source.GetComponent<Renderer>();
            if (ownRenderer != null)
                markerRenderers = new[] { ownRenderer };
        }
    }

    private void ResolveFallbackTarget()
    {
        if (trackedTarget == null && useMainCameraAsFallback && Camera.main != null)
            trackedTarget = Camera.main.transform;
    }

    private void SubscribeToMap()
    {
        ResolveReferences();

        if (mapController == null)
            return;

        mapController.MapOpened -= HandleMapVisibilityChanged;
        mapController.MapClosed -= HandleMapVisibilityChanged;
        mapController.MapOpened += HandleMapVisibilityChanged;
        mapController.MapClosed += HandleMapVisibilityChanged;
    }

    private void HandleMapVisibilityChanged()
    {
        if (mapController != null && mapController.IsOpen)
            UpdateMarker();

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (mapController == null)
            return;

        mapController.MapOpened -= HandleMapVisibilityChanged;
        mapController.MapClosed -= HandleMapVisibilityChanged;
    }

    private void OnValidate()
    {
        ResolveReferences();
    }
}
}
