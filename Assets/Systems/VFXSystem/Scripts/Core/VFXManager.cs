using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField]
    private VFXRegistry registry;

    [SerializeField]
    private string poolRootName = "VFX Pools";

    private readonly Dictionary<VFXId, VFXPool> pools =
        new ();

    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        EnsurePoolRoot();

        if (registry == null)
            registry = GetComponent<VFXRegistry>();

        if (registry == null)
            registry = gameObject.AddComponent<VFXRegistry>();

        registry.Initialize();
    }

    public static bool PlayAt(
        VFXType type,
        Vector3 position)
    {
        return PlayAt(type, VFXVariant.Default, position);
    }

    public static bool PlayAt(
        VFXType type,
        VFXVariant variant,
        Vector3 position)
    {
        return PlayAt(type, variant, position, Quaternion.identity, Vector3.one);
    }

    public static bool PlayAt(
        VFXType type,
        VFXVariant variant,
        Vector3 position,
        Quaternion rotation)
    {
        return PlayAt(type, variant, position, rotation, Vector3.one);
    }

    public static bool PlayAt(
        VFXType type,
        VFXVariant variant,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        if (Instance == null)
        {
            Debug.LogWarning("VFXManager is not available. Make sure Bootstrap instantiates it.");
            return false;
        }

        return Instance.Play(type, variant, position, rotation, scale);
    }

    public bool Play(
        VFXType type,
        Vector3 position
    )
    {
        return Play(type, VFXVariant.Default, position);
    }

    public bool Play(
        VFXType type,
        VFXVariant variant,
        Vector3 position)
    {
        return Play(type, variant, position, Quaternion.identity, Vector3.one);
    }

    public bool Play(
        VFXType type,
        VFXVariant variant,
        Vector3 position,
        Quaternion rotation)
    {
        return Play(type, variant, position, rotation, Vector3.one);
    }

    public bool Play(
        VFXType type,
        VFXVariant variant,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        if (!registry.TryGet(type, variant, out VFXConfig config))
        {
            Debug.LogWarning($"No VFX config registered for {type}_{variant}.");
            return false;
        }

        if (config.prefab == null)
        {
            Debug.LogWarning($"VFX config {config.name} has no prefab assigned yet.");
            return false;
        }

        VFXId id = config.Id;

        if (!pools.ContainsKey(id))
        {
            pools[id] = new VFXPool(
                id,
                config.prefab,
                config.initialPoolSize,
                config.expandable,
                poolRoot);
        }

        GameObject obj = pools[id].Get();

        if (obj == null)
        {
            Debug.LogWarning($"VFX pool for {id} is empty and not expandable.");
            return false;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.localScale = scale;

        obj.SetActive(true);

        VFXInstance vfx = obj.GetComponent<VFXInstance>();

        if (vfx == null)
        {
            Debug.LogWarning($"VFX prefab {config.prefab.name} needs a VFXInstance component.");
            pools[id].Return(obj);
            return false;
        }

        vfx.Play(() => pools[id].Return(obj));
        return true;
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject(poolRootName);
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        poolRoot = root.transform;
    }
}
