using Godot;
using PixelBox.Scripts;
using PixelBox.Scripts.Enums;
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
    private int radius = 2;
    private bool paused = false;
    private int selectedPixelType = 0;

    public static MainGame Instance { get; private set; }

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
    }

    public override void _Process(double delta)
    {
#if DEBUG
        Debugger.DisplayText($"TPS: {debug_lastTicks} / {ticksPerSecond}"); // TICKS PER SECOND
        Debugger.DisplayText($"SLL: {simulationUpdater.SkipLostLoops}"); // SKIP LOST LOOPS
        Debugger.DisplayText($"Paused: {paused}"); // PAUSED
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

        simulationUpdater.WaitTime = Input.IsActionPressed("SpeedUp") ? (double)((1f / ticksPerSecond) / 2f) : (double)(1f / ticksPerSecond);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionPressed("LMB"))
        {
            for (int x = -radius + 1; x < radius; x++)
            {
                for (int y = -radius + 1; y < radius; y++)
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
            for (int x = -radius + 1; x < radius; x++)
            {
                for (int y = -radius + 1; y < radius; y++)
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
            paused = !paused;
        }
        if (Input.IsActionJustPressed("Clear"))
        {
            SimulationData = new PixelData[SimulationSize.X, SimulationSize.Y];
        }
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

        if (paused == false)
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
                    if (x.Distance(MousePoint.X) < radius && y.Distance(MousePoint.Y) < radius)
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
