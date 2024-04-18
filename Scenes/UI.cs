using Godot;
using PixelBox.Scripts.Enums;

namespace PixelBox.Scenes;

public partial class UI : CanvasLayer
{
    [Export] private PackedScene pixelDataScene;
    [Export] private Control grid;

    public void CreatePixelDatas()
    {
        for (int i = 0; i < PixelDataEnums.ALL.Length; i++)
        {
            Scripts.PixelData item = PixelDataEnums.ALL[i];
            var ins = pixelDataScene.Instantiate<Button>();
            int index = i;
            ins.Connect("pressed", Callable.From(() =>
            {
                MainGame.Instance.SetSelectedPixelType(index);
            }));
            ins.GetNode<ColorRect>("BG/Selected").Color = item.Color;
            ins.GetNode<Label>("BG/Label").Text = Tr(PixelDataEnums.Names[i]);
            grid.AddChild(ins);
        }
    }
    public void UpdateSelectedPixelData(int selected)
    {
        for (int i = 0; i < grid.GetChildCount(); i++)
        {
            grid.GetChild(i).GetNode<ColorRect>("BG").Color = i == selected ? new Color(1f, 1f, 1f, 0.25f) : Colors.Transparent;
        }
    }
}
