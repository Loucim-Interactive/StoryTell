using UnityEngine;

namespace StoryTell.PhysicalMap
{
[DisallowMultipleComponent]
[RequireComponent(typeof(MapPhysicsController))]
[RequireComponent(typeof(PaperDeformationController))]
public class MapWindReceiver : MonoBehaviour
{
    [SerializeField] private MapSettings settings;
    [SerializeField] private bool useSettingsWind = true;
    [SerializeField] private Vector3 windDirection = Vector3.right;
    [SerializeField, Min(0f)] private float windStrength = 0.35f;
    [SerializeField, Min(0f)] private float windTorque = 0.08f;

    private MapPhysicsController physicsController;
    private PaperDeformationController deformationController;

    private void Awake()
    {
        physicsController = GetComponent<MapPhysicsController>();
        deformationController = GetComponent<PaperDeformationController>();
    }

    private void FixedUpdate()
    {
        Vector3 direction = CurrentDirection();
        float strength = CurrentStrength();
        float torque = CurrentTorque();

        physicsController.AddWind(direction, strength, torque);
        deformationController.SetWind(direction, strength);
    }

    public void ApplyExternalWind(Vector3 direction, float strength)
    {
        useSettingsWind = false;
        windDirection = direction;
        windStrength = Mathf.Max(0f, strength);
    }

    public void ApplySettings(MapSettings newSettings)
    {
        settings = newSettings;
    }

    private Vector3 CurrentDirection()
    {
        if (useSettingsWind && settings != null)
            return settings.windDirection;

        return windDirection;
    }

    private float CurrentStrength()
    {
        if (useSettingsWind && settings != null)
            return settings.windStrength;

        return windStrength;
    }

    private float CurrentTorque()
    {
        if (useSettingsWind && settings != null)
            return settings.windTorque;

        return windTorque;
    }
}
}
