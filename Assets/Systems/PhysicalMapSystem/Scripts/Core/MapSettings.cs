using UnityEngine;

namespace StoryTell.PhysicalMap
{
[CreateAssetMenu(menuName = "StoryTell/Physical Map/Map Settings")]
public class MapSettings : ScriptableObject
{
    [Header("Input")]
    public KeyCode mapKey = KeyCode.M;

    [Header("Hold Pose")]
    public Vector3 localHoldPosition = new Vector3(0f, -0.25f, 0.75f);
    public Vector3 localHoldEulerAngles = new Vector3(68f, 0f, 0f);
    [Min(0f)] public float openCloseDuration = 0.16f;
    [Range(0.5f, 1f)] public float closedScale = 0.88f;
    [Min(0.01f)] public float followStrength = 55f;
    [Min(0.01f)] public float rotationStrength = 18f;
    [Min(0f)] public float maxFollowSpeed = 6f;

    [Header("Mesh")]
    [Min(2)] public int subdivisionsX = 50;
    [Min(2)] public int subdivisionsY = 50;
    [Min(0.01f)] public float width = 0.72f;
    [Min(0.01f)] public float height = 0.48f;
    [Min(0.001f)] public float colliderThickness = 0.012f;

    [Header("Visual Deformation")]
    [Range(-1f, 1f)] public float bendStrength = 0.12f;
    [Range(0f, 1f)] public float foldAmount = 0.08f;
    [Min(0.01f)] public float noiseScale = 8f;
    [Min(0f)] public float paperMotionSpeed = 1.8f;

    [Header("Wind")]
    public Vector3 windDirection = new Vector3(1f, 0.2f, 0.2f);
    [Min(0f)] public float windStrength = 0.35f;
    [Min(0f)] public float windTorque = 0.08f;

    [Header("Bend Points")]
    public MapBendPoint[] bendPoints =
    {
        new MapBendPoint
        {
            position = new Vector2(-0.25f, 0f),
            direction = Vector2.up,
            intensity = 0.35f,
            radius = 0.28f
        },
        new MapBendPoint
        {
            position = new Vector2(0.25f, 0f),
            direction = Vector2.up,
            intensity = -0.25f,
            radius = 0.25f
        }
    };
}
}
