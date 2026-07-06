using UnityEngine;

[CreateAssetMenu(menuName = "VFX/Config")]
public class VFXConfig : ScriptableObject
{
    public VFXType type;

    public VFXVariant variant = VFXVariant.Default;

    public GameObject prefab;

    public int initialPoolSize = 5;

    public bool expandable = true;

    public VFXId Id =>
        new VFXId(type, variant);

    private void OnValidate()
    {
        if (initialPoolSize < 0)
            initialPoolSize = 0;

        if (prefab != null && prefab.GetComponent<VFXInstance>() == null)
        {
            Debug.LogWarning(
                $"VFX config {name} uses prefab {prefab.name}, but it has no VFXInstance component.",
                this);
        }
    }
}
