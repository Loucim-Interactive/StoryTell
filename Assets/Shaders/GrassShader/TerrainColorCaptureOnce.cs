using System.Collections;
using UnityEngine;

/// <summary>
/// Renders a static terrain colour map once at startup, then leaves the camera
/// disabled so it has no per-frame rendering cost.
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class TerrainColorCaptureOnce : MonoBehaviour
{
    private Camera captureCamera;

    private void Awake()
    {
        captureCamera = GetComponent<Camera>();
        captureCamera.enabled = true;
    }

    private IEnumerator Start()
    {
        // Let URP render this camera normally for the first frame. This avoids
        // Camera.Render(), which is not supported consistently by SRPs.
        yield return new WaitForEndOfFrame();
        captureCamera.enabled = false;
    }

    /// <summary>Schedules one fresh capture for callers that change the terrain later.</summary>
    public void RequestCapture()
    {
        if (captureCamera == null)
            captureCamera = GetComponent<Camera>();

        if (captureCamera.targetTexture == null)
        {
            Debug.LogWarning("Terrain color capture skipped because no target RenderTexture is assigned.", this);
            return;
        }

        captureCamera.enabled = true;
        StartCoroutine(DisableAfterFrame());
    }

    private IEnumerator DisableAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        captureCamera.enabled = false;
    }
}
