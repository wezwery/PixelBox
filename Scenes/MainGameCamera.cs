using Godot;
using WezweryGodotTools;
using WezweryGodotTools.Extensions;

namespace PixelBox.Scenes;

public partial class MainGameCamera : Camera2D
{
    public override void _Process(double delta)
    {
        if (MyInput.IsKeyJustPressed(Key.Escape)) GetTree().Quit();
        if (MyInput.IsKeyJustPressed(Key.F11)) WezweryGodotToolsEngine.ToggleFullscreen(false);

        Position += new Vector2(MyInput.Axis(Key.A, Key.D),
                                MyInput.Axis(Key.W, Key.S)).Normalized() * ((float)delta * 200f);
        Position = Position.Clamp(new(0, 0, MainGame.Instance.SimulationSize));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ZoomOut"))
        {
            Zoom = (Zoom - new Vector2(0.1f, 0.1f)).ClampMin(new Vector2(3f, 3f));
        }
        else if (Input.IsActionJustPressed("ZoomIn"))
        {
            Zoom += new Vector2(0.1f, 0.1f);
            Zoom = Zoom.ClampMax(10f);
        }
    }
}