using System.Collections.Generic;
using UnityEngine;

public class VFXPool
{
    private readonly Queue<GameObject> pool =
        new Queue<GameObject>();

    private readonly VFXId id;
    private readonly GameObject prefab;
    private readonly bool expandable;
    private readonly Transform parent;

    public VFXPool(
        VFXId id,
        GameObject prefab,
        int initialSize,
        bool expandable,
        Transform parent)
    {
        this.id = id;
        this.prefab = prefab;
        this.expandable = expandable;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateInstance();
            Return(obj);
        }
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        if (!expandable)
            return null;

        return CreateInstance();
    }

    public void Return(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
        obj.transform.SetParent(parent);

        pool.Enqueue(obj);
    }

    private GameObject CreateInstance()
    {
        GameObject obj = Object.Instantiate(prefab, parent);
        obj.name = $"{prefab.name}_{id}";
        obj.SetActive(false);
        return obj;
    }
}
