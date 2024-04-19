using Godot;
using PixelBox.Scripts;
using PixelBox.Scripts.Enums;
using System.IO;
using System.Linq;
using WezweryGodotTools;
using WezweryGodotTools.Extensions;

namespace PixelBox.Scenes;

public partial class MainGame : Node2D
{
#if DEBUG
    private MyTimer debug_TicksUpdater;
    private int debug_lastTicks = 0;
    private int debug_currentTicks = 0;
#endif

    [Export, ExportCategory("Main")] private Sprite2D simulation;
    [Export] private Camera2D camera;
    [Export] private UI ui;
    [Export, ExportCategory("Settings")] public Vector2I SimulationSize { get; private set; } = new(500, 500);
    [Export] private int ticksPerSecond = 60;
    [Export] private bool skipLostLoops = true;

    private MyTimer simulationUpdater;
    private Image simulationImage;
    private ImageTexture simulationTexture;
    private int selectedPixelType = 0;

    public static MainGame Instance { get; private set; }

    public static string PathToScreenshotFolder = $"{OS.GetExecutablePath().GetBaseDir()}/Screenshots/";

    public bool Paused = false;
    public int Radius = 1;
    public Vector2I MousePoint { get; private set; } = new Vector2I();
    public bool IsMousePointValid => SimulationData.IsValid(MousePoint.X, MousePoint.Y);

    public PixelData[,] SimulationData { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }
    public override void _ExitTree()
    {
        Instance = null;
    }

    public override void _Ready()
    {
#if DEBUG
        debug_TicksUpdater = new MyTimer()
        {
            WaitTime = 1f,
            SkipLostLoops = false,
            OneShot = false
        };
        AddChild(debug_TicksUpdater);
        debug_TicksUpdater.Start();
        debug_TicksUpdater.Connect("timeout", Callable.From(() =>
        {
            debug_lastTicks = debug_currentTicks;
            debug_currentTicks = 0;
        }));
#endif

        #region Init Data
        SimulationData = new PixelData[SimulationSize.X, SimulationSize.Y];
        #endregion

        #region Init Texture
        simulationImage = Image.Create(SimulationSize.X, SimulationSize.Y, false, Image.Format.Rgba4444);
        simulationTexture = ImageTexture.CreateFromImage(simulationImage);
        simulation.Texture = simulationTexture;
        #endregion

        #region Init Updater
        simulationUpdater = new MyTimer
        {
            WaitTime = 1f / ticksPerSecond,
            SkipLostLoops = skipLostLoops,
            OneShot = false
        };
        AddChild(simulationUpdater);
        simulationUpdater.Start();
        simulationUpdater.Connect("timeout", Callable.From(UpdateSimulation));
        #endregion

        camera.Position = SimulationSize / 2;

        ui.CreatePixelDatas();
        ui.UpdateSelectedPixelData(selectedPixelType);
        ui.UpdatePauseIcon();
        ui.UpdateSpeedBtn(1);
        SetSpeed(1f);
    }

    public override void _Process(double delta)
    {
#if DEBUG
        Debugger.DisplayText($"TPS: {debug_lastTicks} / {ticksPerSecond}"); // TICKS PER SECOND
        Debugger.DisplayText($"SLL: {simulationUpdater.SkipLostLoops}"); // SKIP LOST LOOPS
        Debugger.DisplayText($"Paused: {Paused}"); // PAUSED
        Debugger.DisplayText($"Selected Pixel Type: {PixelDataEnums.Names[selectedPixelType]}"); // SELECTED PIXEL TYPE

        if (IsMousePointValid)
        {
            var pixelData = SimulationData[MousePoint.X, MousePoint.Y];

            Debugger.DisplayText("PixelData:");
            Debugger.DisplayText($"     ID: {pixelData.ID}");
            Debugger.DisplayText($"     Color: {pixelData.Color}");
            Debugger.DisplayText($"     Material: {pixelData.Material}");
            Debugger.DisplayText($"     Fire: {pixelData.Fire}");
            Debugger.DisplayText($"     Flamable: {pixelData.Flamable}");
            Debugger.DisplayText($"     ChanceToFlame: {pixelData.GetChanceToFlame()}%");
            Debugger.DisplayText($"     ChanceToDestroyByFire: {pixelData.GetChanceToDestroyByFire()}%");
        }
#endif

        MousePoint = GetGlobalMousePosition().FloorToInt();
    }

    public void SetSpeed(float speed)
    {
        simulationUpdater.WaitTime = 1f / ticksPerSecond / speed;
    }

    public void MakeScreenshot()
    {
        if (OS.HasFeature("editor"))
        {
            $"Failed to make screenshot! (No editor!)".LogError();
            return;
        }
        if (Directory.Exists(PathToScreenshotFolder) == false) Directory.CreateDirectory(PathToScreenshotFolder);
        var index = Directory.EnumerateFiles(PathToScreenshotFolder).Count();
        var err = simulationImage.SavePng($"{PathToScreenshotFolder}{index}.png");
        if (err > 0) $"Failed to make screenshot! ({err})".LogError();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionPressed("LMB"))
        {
            for (int x = -Radius + 1; x < Radius; x++)
            {
                for (int y = -Radius + 1; y < Radius; y++)
                {
                    int posX = MousePoint.X + x;
                    int posY = MousePoint.Y + y;
                    PixelData data = PixelDataEnums.ALL[selectedPixelType];
                    if (SimulationData.IsValid(posX, posY) && (data.Replacable || SimulationData[posX, posY].HasPixel() == false))
                    {
                        SimulationData[posX, posY] = data;
                    }
                }
            }
        }
        if (Input.IsActionPressed("RMB"))
        {
            for (int x = -Radius + 1; x < Radius; x++)
            {
                for (int y = -Radius + 1; y < Radius; y++)
                {
                    int posX = MousePoint.X + x;
                    int posY = MousePoint.Y + y;
                    if (SimulationData.IsValid(posX, posY))
                    {
                        SimulationData[posX, posY] = default;
                    }
                }
            }
        }
        if (Input.IsActionJustPressed("Pause"))
        {
            Paused = !Paused;
            ui.UpdatePauseIcon();
        }
        if (Input.IsActionJustPressed("MakeScreenshot"))
        {
            MakeScreenshot();
        }
        if (Input.IsActionJustPressed("NextFrame"))
        {
            SimulationData = PixelBoxPhysics.Update(SimulationSize, SimulationData);
        }
    }

    public void Clear()
    {
        SimulationData = new PixelData[SimulationSize.X, SimulationSize.Y];
    }

    public void SetSelectedPixelType(int index)
    {
        selectedPixelType = index.Clamp(0, PixelDataEnums.Length - 1);
        ui.UpdateSelectedPixelData(index);
    }

    private void UpdateSimulation()
    {
#if DEBUG
        debug_currentTicks++;
#endif

        if (Paused == false)
        {
            SimulationData = PixelBoxPhysics.Update(SimulationSize, SimulationData);
        }

        for (int x = 0; x < SimulationSize.X; x++)
        {
            for (int y = 0; y < SimulationSize.Y; y++)
            {
                PixelData data = SimulationData[x, y];
                if (data.HasPixel())
                {
                    Color color = data.Fire || data.ID == PixelDataIDs.FIRE_ID ? MyMath.Random(Color.Color8(247, 127, 0), Color.Color8(214, 40, 40), Color.Color8(252, 191, 73)) : data.Color;
                    simulationImage.SetPixel(x, y, color);
                }
                else
                {
                    if (x.Distance(MousePoint.X) < Radius && y.Distance(MousePoint.Y) < Radius)
                    {
                        simulationImage.SetPixel(x, y, Colors.White);
                    }
                    else
                    {
                        simulationImage.SetPixel(x, y, new(0.25f, 0.25f, 0.25f));
                    }
                }
            }
        }

        simulationTexture.Update(simulationImage);
    }
}
