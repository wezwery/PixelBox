using Godot;
using PixelBox.Scripts.Enums;

namespace PixelBox.Scenes;

public partial class UI : CanvasLayer
{
    [Export, ExportCategory("Pixels")] private PackedScene pixelDataScene;
    [Export] private Control grid;

    [Export, ExportCategory("Settings/SpeedControl")] private Button playPauseBtn;
    [Export] private Button speed0d5x, speed1x, speed1d5x, speed2x, speed3x;
    [Export] private Label currentSpeedLabel;
    [Export, ExportCategory("Settings/Simulation")] private Button clearBtn;
    [Export] private Button screenshotBtn;
    [Export, ExportCategory("Settings/PaintSize")] private Label paintSizeLabel;
    [Export] private Slider paintSizeSlider;

    public override void _Ready()
    {
        playPauseBtn.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.Paused = !MainGame.Instance.Paused;
            UpdatePauseIcon();
        }));
        speed0d5x.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.SetSpeed(0.5f);
            UpdateSpeedBtn(0);
        }));
        speed1x.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.SetSpeed(1f);
            UpdateSpeedBtn(1);
        }));
        speed1d5x.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.SetSpeed(1.5f);
            UpdateSpeedBtn(2);
        }));
        speed2x.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.SetSpeed(2f);
            UpdateSpeedBtn(3);
        }));
        speed3x.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.SetSpeed(3f);
            UpdateSpeedBtn(4);
        }));
        clearBtn.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.Clear();
        }));
        paintSizeSlider.Connect("value_changed", Callable.From<float>(x =>
        {
            MainGame.Instance.Radius = (int)x + 1;
            paintSizeLabel.Text = $"{(int)x * 2 + 1}x{(int)x * 2 + 1}";
        }));
        screenshotBtn.Connect("pressed", Callable.From(() =>
        {
            MainGame.Instance.MakeScreenshot();
        }));
    }

    public override void _Process(double delta)
    {
        Control gridParent = grid.GetParent<Control>();
        gridParent.SetSize(gridParent.Size with { Y = grid.Size.Y + 10 });
        gridParent.SetOffsetsPreset(Control.LayoutPreset.BottomWide, Control.LayoutPresetMode.KeepSize);
    }

    public void UpdatePauseIcon()
    {
        if (MainGame.Instance.Paused)
        {
            playPauseBtn.Text = Tr("PLAY");
        }
        else
        {
            playPauseBtn.Text = Tr("PAUSE");
        }
    }

    public void UpdateSpeedBtn(int index)
    {
        switch (index)
        {
            case 0:
                currentSpeedLabel.Text = "0.5x";
                break;
            case 1:
                currentSpeedLabel.Text = "1x";
                break;
            case 2:
                currentSpeedLabel.Text = "1.5x";
                break;
            case 3:
                currentSpeedLabel.Text = "2x";
                break;
            case 4:
                currentSpeedLabel.Text = "3x";
                break;
        }
    }

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
            ins.GetNode<ColorRect>("BG/Container/Selected").Color = item.Color;
            ins.GetNode<Label>("BG/Container/Label").Text = Tr(PixelDataEnums.Names[i]);
            grid.AddChild(ins);
            ins.CustomMinimumSize = ins.Size = new(20 + ins.GetNode<Label>("BG/Container/Label").Size.X, 15);
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
