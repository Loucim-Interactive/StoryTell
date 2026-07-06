using System;

[Serializable]
public readonly struct VFXId : IEquatable<VFXId>
{
    public readonly VFXType Type;
    public readonly VFXVariant Variant;

    public VFXId(VFXType type, VFXVariant variant)
    {
        Type = type;
        Variant = variant;
    }

    public bool Equals(VFXId other)
    {
        return Type == other.Type && Variant == other.Variant;
    }

    public override bool Equals(object obj)
    {
        return obj is VFXId other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Type * 397) ^ (int)Variant;
        }
    }

    public override string ToString()
    {
        return $"{Type}_{Variant}";
    }
}
