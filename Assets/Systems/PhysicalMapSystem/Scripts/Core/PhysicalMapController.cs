using System;
using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
[RequireComponent(typeof(MapPhysicsController))]
[RequireComponent(typeof(PaperDeformationController))]
public class PhysicalMapController : MonoBehaviour
{
    [SerializeField] private MapSettings settings;
    [SerializeField] private Transform viewAnchor;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool autoFindMainCamera = true;
    [SerializeField] private bool keyboardInputEnabled = true;

    private MapPhysicsController physicsController;
    private PaperDeformationController deformationController;
    private MapWindReceiver windReceiver;
    private Renderer[] mapRenderers;
    private Collider[] mapColliders;
    private bool isOpen;
    private bool warnedAboutMissingAnchor;
    private Coroutine transitionRoutine;
    private Vector3 openScale;

    public bool IsOpen => isOpen;
    public MapSettings Settings => settings;
    public Transform ViewAnchor => viewAnchor;
    public bool IsReady => settings != null && viewAnchor != null;

    public event Action MapOpened;
    public event Action MapClosed;

    private void Awake()
    {
        CacheComponents();
        CacheVisibilityTargets();
        openScale = transform.localScale;

        TryResolveViewAnchor();

        ApplySettings();

        SetOpenInternal(!startHidden, true, false);
    }

    private void Update()
    {
        if (settings == null || !keyboardInputEnabled)
            return;

        if (WasMapKeyPressed())
            Toggle();
    }

    public void Toggle()
    {
        TryResolveViewAnchor();
        SetOpen(!isOpen, false);
    }

    public void SetOpen(bool open, bool instant)
    {
        SetOpenInternal(open, instant, true);
    }

    private void SetOpenInternal(bool open, bool instant, bool notify)
    {
        CacheComponents();
        CacheVisibilityTargets();

        bool changed = isOpen != open;

        if (!changed && !instant)
            return;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isOpen = open;

        if (!open)
        {
            physicsController.SetPhysicalActive(false);
            SetMapCollidersVisible(false);

            if (instant || settings == null || settings.openCloseDuration <= 0f)
            {
                transform.localScale = openScale * ClosedScale;
                SetMapRenderersVisible(false);
            }
            else
            {
                transitionRoutine = StartCoroutine(AnimateScale(
                    transform.localScale,
                    openScale * ClosedScale,
                    settings.openCloseDuration,
                    hideAtEnd: true));
            }

            if (notify && changed)
                MapClosed?.Invoke();

            return;
        }

        TryResolveViewAnchor();
        ApplySettings();
        SetMapRenderersVisible(true);
        SetMapCollidersVisible(true);
        physicsController.SetPhysicalActive(true);
        SnapToHoldPose();

        if (instant || settings == null || settings.openCloseDuration <= 0f)
        {
            transform.localScale = openScale;
        }
        else
        {
            transform.localScale = openScale * ClosedScale;
            transitionRoutine = StartCoroutine(AnimateScale(
                transform.localScale,
                openScale,
                settings.openCloseDuration,
                hideAtEnd: false));
        }

        if (notify && changed)
            MapOpened?.Invoke();
    }

    public void SetViewAnchor(Transform anchor)
    {
        viewAnchor = anchor;
        ApplySettings();
    }

    public void Configure(MapSettings newSettings, Transform anchor = null)
    {
        settings = newSettings;

        if (anchor != null)
            viewAnchor = anchor;

        TryResolveViewAnchor();
        ApplySettings();
    }

    public void SetKeyboardInputEnabled(bool enabled)
    {
        keyboardInputEnabled = enabled;
    }

    private void ApplySettings()
    {
        if (settings == null)
            return;

        CacheComponents();

        deformationController.ApplySettings(settings);
        physicsController.ApplySettings(settings, viewAnchor);

        if (windReceiver != null)
            windReceiver.ApplySettings(settings);
    }

    private void SnapToHoldPose()
    {
        if (settings == null || viewAnchor == null)
            return;

        transform.SetPositionAndRotation(
            viewAnchor.TransformPoint(settings.localHoldPosition),
            viewAnchor.rotation * Quaternion.Euler(settings.localHoldEulerAngles));

        physicsController.ResetMotion();
    }

    private void SetMapRenderersVisible(bool visible)
    {
        for (int i = 0; i < mapRenderers.Length; i++)
            mapRenderers[i].enabled = visible;
    }

    private void SetMapCollidersVisible(bool visible)
    {
        for (int i = 0; i < mapColliders.Length; i++)
            mapColliders[i].enabled = visible;
    }

    private void CacheComponents()
    {
        if (physicsController == null)
            physicsController = GetComponent<MapPhysicsController>();

        if (deformationController == null)
            deformationController = GetComponent<PaperDeformationController>();

        if (windReceiver == null)
            windReceiver = GetComponent<MapWindReceiver>();
    }

    private void CacheVisibilityTargets()
    {
        if (mapRenderers == null)
            mapRenderers = GetComponentsInChildren<Renderer>(true);

        if (mapColliders == null)
            mapColliders = GetComponentsInChildren<Collider>(true);
    }

    private float ClosedScale => settings != null ? settings.closedScale : 0.88f;

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, bool hideAtEnd)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        transform.localScale = to;

        if (hideAtEnd && !isOpen)
            SetMapRenderersVisible(false);

        transitionRoutine = null;
    }

    private void TryResolveViewAnchor()
    {
        if (viewAnchor != null || !autoFindMainCamera)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            viewAnchor = mainCamera.transform;
            warnedAboutMissingAnchor = false;
            return;
        }

        if (!warnedAboutMissingAnchor && Application.isPlaying)
        {
            Debug.LogWarning(
                "PhysicalMap could not find a MainCamera. Assign a view anchor or tag the player camera as MainCamera.",
                this);
            warnedAboutMissingAnchor = true;
        }
    }

    private bool WasMapKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && Enum.TryParse(settings.mapKey.ToString(), true, out Key key))
        {
            if (keyboard[key].wasPressedThisFrame)
                return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(settings.mapKey);
#else
        return false;
#endif
    }

    private void OnValidate()
    {
        if (settings == null)
            return;

        if (TryGetComponent(out PaperDeformationController deformation))
            deformation.ApplySettings(settings);
    }

    private void OnDrawGizmosSelected()
    {
        if (settings == null || viewAnchor == null)
            return;

        Vector3 targetPosition = viewAnchor.TransformPoint(settings.localHoldPosition);
        Quaternion targetRotation = viewAnchor.rotation * Quaternion.Euler(settings.localHoldEulerAngles);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(viewAnchor.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.035f);

        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(targetPosition, targetRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(settings.width, settings.colliderThickness, settings.height));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
}
