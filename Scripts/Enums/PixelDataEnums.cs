using Godot;
using System.Linq;
using WezweryGodotTools;
using WezweryGodotTools.Extensions;
using static PixelBox.Scripts.Enums.PixelDataIDs;

namespace PixelBox.Scripts.Enums;

public static class PixelDataEnums
{
    private static float ColorOffset => MyMath.Random(0.99f, 1.01f);

    public static readonly int Length = typeof(PixelDataEnums).GetProperties().Length - 1;
    public static readonly string[] Names = typeof(PixelDataEnums).GetProperties().Skip(1).Select(x => x.Name).ToArray();

    public static PixelData[] ALL => typeof(PixelDataEnums).GetProperties().Skip(1).Select(x => x.GetValue(null)).Cast<PixelData>().ToArray();

    public static PixelData SAND => new(SAND_ID)
    {
        Color = (Color.Color8(255, 191, 105) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Sand,
        Replacable = false
    };
    public static PixelData SMOKE => new(SMOKE_ID)
    {
        Color = (Color.Color8(33, 37, 41) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Gas,
        Replacable = false
    };
    public static PixelData ACID => new(ACID_ID)
    {
        Color = (Color.Color8(61, 235, 30) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Fluid,
        Replacable = false
    };
    public static PixelData WATER => new(WATER_ID)
    {
        Color = (Color.Color8(72, 202, 228) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Fluid,
        Replacable = false
    };
    public static PixelData STEAM => new(STEAM_ID)
    {
        Color = (Color.Color8(180, 180, 180) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Gas,
        Replacable = false
    };
    public static PixelData BRICK => new(BRICK_ID)
    {
        Color = (MyMath.Random(Color.Color8(112, 47, 38), Color.Color8(158, 51, 48), Color.Color8(192, 109, 83)) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static
    };
    public static PixelData COAL => new(COAL_ID)
    {
        Color = (Color.Color8(25, 25, 25) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Sand,
        Replacable = false,
        Flamable = true,
        ChanceToDestroyByFire = 3,
        ChanceToFlame = 10
    };
    public static PixelData FIRE => new(FIRE_ID)
    {
        Color = Color.Color8(247, 127, 0).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Gas,
        Replacable = false
    };
    public static PixelData CLOUD => new(CLOUD_ID)
    {
        Color = (Color.Color8(240, 240, 240) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static
    };
    public static PixelData STORM_CLOUD => new(STORM_CLOUD_ID)
    {
        Color = (Color.Color8(180, 180, 180) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static
    };
    public static PixelData GRAVEL => new(GRAVEL_ID)
    {
        Color = (Color.Color8(114, 114, 114) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.HardSand,
        Replacable = false
    };
    public static PixelData BLACK_HOLE => new(BLACK_HOLE_ID)
    {
        Color = (Color.Color8(128, 0, 128) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static,
    };
    public static PixelData WOOD => new(WOOD_ID)
    {
        Color = (Color.Color8(218, 109, 66) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static,
        Flamable = true,
        ChanceToDestroyByFire = 50,
        ChanceToFlame = 50
    };
    public static PixelData SAW_DAST => new(SAW_DAST_ID)
    {
        Color = (Color.Color8(218, 109, 66) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Sand,
        Flamable = true,
        ChanceToDestroyByFire = 50,
        ChanceToFlame = 50
    };
    public static PixelData WEB => new(WEB_ID)
    {
        Color = (Color.Color8(240, 240, 240) * ColorOffset).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static,
        Flamable = true,
        ChanceToDestroyByFire = 255,
        ChanceToFlame = 255
    };
    public static PixelData LIGHTER => new(LIGHTER_ID)
    {
        Color = Color.Color8(200, 200, 200).ToMyVector3Byte(),
        Material = PixelData.MaterialEnum.Static
    };
}
