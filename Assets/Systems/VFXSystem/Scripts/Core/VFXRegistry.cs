using System.Collections.Generic;
using UnityEngine;

public class VFXRegistry : MonoBehaviour
{
    private const string DefaultResourcesPath = "VFX/Configs";

    [SerializeField]
    private string resourcesPath = DefaultResourcesPath;

    [SerializeField]
    private VFXConfig[] manualConfigs;

    private Dictionary<VFXId, VFXConfig> lookup;

    public void Initialize()
    {
        lookup = new ();

        RegisterConfigs(Resources.LoadAll<VFXConfig>(resourcesPath));
        RegisterConfigs(manualConfigs);
    }

    public bool TryGet(
        VFXType type,
        VFXVariant variant,
        out VFXConfig config)
    {
        if (lookup == null)
            Initialize();

        VFXId requestedId = new VFXId(type, variant);

        if (lookup.TryGetValue(requestedId, out config))
            return true;

        VFXId fallbackId = new VFXId(type, VFXVariant.Default);

        return variant != VFXVariant.Default &&
            lookup.TryGetValue(fallbackId, out config);
    }

    private void RegisterConfigs(IEnumerable<VFXConfig> configs)
    {
        if (configs == null)
            return;

        foreach (VFXConfig config in configs)
        {
            if (config == null)
                continue;

            VFXId id = config.Id;

            if (lookup.ContainsKey(id))
            {
                Debug.LogWarning(
                    $"Duplicate VFX config for {id}. Keeping {lookup[id].name}, ignoring {config.name}.");
                continue;
            }

            lookup.Add(id, config);
        }
    }
}
