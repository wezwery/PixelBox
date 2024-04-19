using Godot;

namespace PixelBox.Scripts;

public struct PixelData
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
}
