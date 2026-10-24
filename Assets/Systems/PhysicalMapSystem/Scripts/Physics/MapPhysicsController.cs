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

    private Rigidbody mapRigidbody;
    private bool physicalActive = true;

    private void Awake()
    {
        mapRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    private void FixedUpdate()
    {
        if (!followHoldPose || settings == null || viewAnchor == null)
            return;

        ApplyHoldForces();
    }

    public void ApplySettings(MapSettings newSettings, Transform newViewAnchor)
    {
        settings = newSettings;
        viewAnchor = newViewAnchor;
        ConfigureRigidbody();
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
        mapRigidbody.useGravity = active;
        mapRigidbody.isKinematic = !active;

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
        mapRigidbody.useGravity = physicalActive;
        mapRigidbody.isKinematic = !physicalActive;
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
