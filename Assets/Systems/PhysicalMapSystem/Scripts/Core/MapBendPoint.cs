using System;
using UnityEngine;

namespace StoryTell.PhysicalMap
{
[Serializable]
public struct MapBendPoint
{
    public Vector2 position;
    public Vector2 direction;
    [Range(-1f, 1f)] public float intensity;
    [Min(0.01f)] public float radius;
}
}
