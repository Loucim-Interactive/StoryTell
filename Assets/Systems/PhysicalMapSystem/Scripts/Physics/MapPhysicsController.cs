using UnityEngine;

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MapPhysicsController : MonoBehaviour
{
    [SerializeField] private MapSettings settings;
    [SerializeField] private Transform viewAnchor;
    [SerializeField] private bool followHoldPose = true;
    [Tooltip("Readable first-person paper without physics lag, collision or wind impulses.")]
    [SerializeField] private bool stableHold;

    private Rigidbody mapRigidbody;
    private bool physicalActive = true;

    private void Awake()
    {
        mapRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    private void FixedUpdate()
    {
        if (stableHold) return;
        if (!followHoldPose || settings == null || viewAnchor == null)
            return;

        ApplyHoldForces();
    }

    private void LateUpdate()
    {
        if (!stableHold || !physicalActive || settings == null || viewAnchor == null) return;
        transform.SetPositionAndRotation(viewAnchor.TransformPoint(settings.localHoldPosition),
            viewAnchor.rotation * Quaternion.Euler(settings.localHoldEulerAngles));
    }

    public void ApplySettings(MapSettings newSettings, Transform newViewAnchor)
    {
        settings = newSettings;
        viewAnchor = newViewAnchor;
        ConfigureRigidbody();
    }

    public void SnapToPose(Vector3 position, Quaternion rotation)
    {
        if (mapRigidbody == null) mapRigidbody = GetComponent<Rigidbody>();
        mapRigidbody.position = position;
        mapRigidbody.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        ResetMotion();
    }

    public void ResetMotion()
    {
        if (mapRigidbody == null)
            mapRigidbody = GetComponent<Rigidbody>();

        if (mapRigidbody.isKinematic) return;

        mapRigidbody.linearVelocity = Vector3.zero;
        mapRigidbody.angularVelocity = Vector3.zero;
    }

    public void SetPhysicalActive(bool active)
    {
        if (mapRigidbody == null)
            mapRigidbody = GetComponent<Rigidbody>();

        followHoldPose = active;
        physicalActive = active;
        mapRigidbody.useGravity = active && !stableHold;
        mapRigidbody.isKinematic = !active || stableHold;

        if (!active)
            ResetMotion();
    }

    public void AddWind(Vector3 direction, float strength, float torque)
    {
        if (mapRigidbody == null || strength <= 0f)
            return;

        if (!physicalActive || mapRigidbody.isKinematic)
            return;

        Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : transform.forward;

        mapRigidbody.AddForce(normalizedDirection * strength, ForceMode.Acceleration);
        mapRigidbody.AddTorque(Vector3.Cross(transform.up, normalizedDirection) * torque, ForceMode.Acceleration);
    }

    private void ConfigureRigidbody()
    {
        if (mapRigidbody == null)
            mapRigidbody = GetComponent<Rigidbody>();

        mapRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        mapRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        mapRigidbody.mass = 0.08f;
        mapRigidbody.linearDamping = 5f;
        mapRigidbody.angularDamping = 7f;
        mapRigidbody.useGravity = physicalActive && !stableHold;
        mapRigidbody.isKinematic = !physicalActive || stableHold;
        mapRigidbody.detectCollisions = !stableHold;
    }

    private void ApplyHoldForces()
    {
        Vector3 targetPosition = viewAnchor.TransformPoint(settings.localHoldPosition);
        Quaternion targetRotation = viewAnchor.rotation * Quaternion.Euler(settings.localHoldEulerAngles);

        Vector3 toTarget = targetPosition - mapRigidbody.position;
        Vector3 desiredVelocity = Vector3.ClampMagnitude(toTarget * settings.followStrength, settings.maxFollowSpeed);
        mapRigidbody.AddForce(desiredVelocity - mapRigidbody.linearVelocity, ForceMode.VelocityChange);

        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(mapRigidbody.rotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        if (axis.sqrMagnitude < 0.0001f)
            return;

        Vector3 angularCorrection = axis.normalized * (angle * Mathf.Deg2Rad * settings.rotationStrength);
        mapRigidbody.AddTorque(angularCorrection - mapRigidbody.angularVelocity, ForceMode.VelocityChange);
    }
}
}
