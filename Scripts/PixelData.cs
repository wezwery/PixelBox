using Godot;
using System;

namespace PixelBox.Scripts;

public struct PixelData : IEquatable<PixelData>
{
    public enum MaterialEnum
    {
        Static, HardSand, Sand, Fluid, Gas
    }

    public byte ID;
    public MaterialEnum Material;
    public Color Color;
    public bool Updated;
    public bool Fire;
    public bool Flamable;
    public bool Replacable;
    public byte ChanceToDestroyByFire;
    public byte ChanceToFlame;

    public PixelData(byte id)
    {
        ID = id;
        Color = default;
        Material = MaterialEnum.Static;
        Updated = false;
        Fire = false;
        Flamable = false;
        Replacable = true;
        ChanceToDestroyByFire = 0;
        ChanceToFlame = 0;
    }

    public readonly bool HasPixel() => ID > 0;
    public readonly float GetChanceToDestroyByFire() => ChanceToDestroyByFire / (float)255 * 100f;
    public readonly float GetChanceToFlame() => ChanceToFlame / (float)255 * 100f;

    public static bool operator ==(PixelData from, PixelData other) => from.Equals(other);
    public static bool operator !=(PixelData from, PixelData other) => !from.Equals(other);

    public readonly bool Equals(PixelData other)
    {
        return other.ID == ID
            && other.Color == Color
            && other.Material == Material
            && Updated == other.Updated
            && Fire == other.Fire
            && Flamable == other.Flamable
            && Replacable == other.Replacable
            && ChanceToDestroyByFire == other.ChanceToDestroyByFire
            && ChanceToFlame == other.ChanceToFlame;
    }
}
