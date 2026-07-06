using UnityEngine;

[DisallowMultipleComponent]
public sealed class VFXEmitter : MonoBehaviour
{
    [SerializeField] private VFXType type = VFXType.Explosion;
    [SerializeField] private VFXVariant variant = VFXVariant.Default;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnPlayerEnter;
    [SerializeField] private bool triggerOnce;
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public bool Play()
    {
        if (triggerOnce && hasTriggered)
            return false;

        Transform origin = spawnPoint != null ? spawnPoint : transform;

        bool played = VFXManager.PlayAt(
            type,
            variant,
            origin.position,
            origin.rotation,
            scale);

        if (played)
            hasTriggered = true;

        return played;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnPlayerEnter && other.CompareTag(playerTag))
            Play();
    }

    private void OnValidate()
    {
        scale.x = Mathf.Max(0.001f, scale.x);
        scale.y = Mathf.Max(0.001f, scale.y);
        scale.z = Mathf.Max(0.001f, scale.z);
    }
}
