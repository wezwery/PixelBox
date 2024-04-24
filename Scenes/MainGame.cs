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

    private static readonly Color bg_color = new(0.25f, 0.25f, 0.25f);

    [Export, ExportCategory("Main")] private Camera2D camera;
    [Export] private UI ui;
    [Export, ExportCategory("Settings")] public Vector2I SimulationSize { get; private set; } = new(500, 500);
    [Export] private int ticksPerSecond = 60;
    [Export] private bool skipLostLoops = true;
    [Export] public Vector2I ChunksCount = new(3, 3);

    private Sprite2D[,] chunks;
    public bool[,] RequestToUpdates;

    private MyTimer simulationUpdater;
    private Image[,] simulationImages;
    private ImageTexture[,] simulationTextures;
    public Rect2I[,] ChunkRects;
    private int selectedPixelType = 0;

    public static MainGame Instance { get; private set; }

    public readonly static string PathToScreenshotFolder = $"{OS.GetExecutablePath().GetBaseDir()}/Screenshots/";

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
        SimulationSize -= SimulationSize % 2;
        ChunksCount -= ChunksCount % 2;
        if (ChunksCount.X == 0) ChunksCount.X = 1;
        if (ChunksCount.Y == 0) ChunksCount.Y = 1;
        SimulationData = new PixelData[SimulationSize.X, SimulationSize.Y];
        chunks = new Sprite2D[ChunksCount.X, ChunksCount.Y];
        RequestToUpdates = new bool[ChunksCount.X, ChunksCount.Y];
        simulationImages = new Image[ChunksCount.X, ChunksCount.Y];
        simulationTextures = new ImageTexture[ChunksCount.X, ChunksCount.Y];
        ChunkRects = new Rect2I[ChunksCount.X, ChunksCount.Y];
        #endregion

        #region Init Chunks
        int xSize = SimulationSize.X / ChunksCount.X;
        int ySize = SimulationSize.Y / ChunksCount.Y;
        for (int x = 0; x < ChunksCount.X; x++)
        {
            for (int y = 0; y < ChunksCount.Y; y++)
            {
                simulationImages[x, y] = Image.Create(xSize, ySize, false, Image.Format.Rgba8);
                simulationImages[x, y].Fill(bg_color);
                simulationTextures[x, y] = ImageTexture.CreateFromImage(simulationImages[x, y]);
                var sprite = new Sprite2D();
                AddChild(sprite);
                var pos = new Vector2I(xSize * x, ySize * y);
                sprite.Centered = false;
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                sprite.Position = pos;
                chunks[x, y] = sprite;
                sprite.Texture = simulationTextures[x, y];
                ChunkRects[x, y] = new Rect2I(pos, xSize, ySize);
            }
        }
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

    private void UpdateChunks()
    {
        bool[,] chunksToUpdate = new bool[ChunksCount.X, ChunksCount.Y];

        for (int x = 0; x < ChunksCount.X; x++)
        {
            for (int y = 0; y < ChunksCount.Y; y++)
            {
                chunksToUpdate[x, y] = RequestToUpdates[x, y];
            }
        }

        for (int x = 0; x < SimulationSize.X; x++)
        {
            for (int y = 0; y < SimulationSize.Y; y++)
            {
                if (SimulationData[x, y].HasPixel() == false) continue;
                SimulationData[x, y].Updated = false;
            }
        }

        for (int x = 0; x < ChunksCount.X; x++)
        {
            for (int y = 0; y < ChunksCount.Y; y++)
            {
                //bool pointer = PixelBoxPhysics.GetChunkByPoint(MousePoint).Distance(new(x, y)) > 0.5f;
                if (chunksToUpdate[x, y] == false) continue;
                RequestToUpdates[x, y] = false;
                SimulationData = PixelBoxPhysics.Update(x, y);
                var bounds = ChunkRects[x, y];
                for (int xx = 0; xx < bounds.Size.X; xx++)
                {
                    for (int yy = 0; yy < bounds.Size.Y; yy++)
                    {
                        PixelData data = SimulationData[bounds.Position.X + xx, bounds.Position.Y + yy];
                        if (data.HasPixel())
                        {
                            Color color = data.Fire || data.ID == PixelDataIDs.FIRE_ID ? MyMath.Random(Color.Color8(247, 127, 0), Color.Color8(214, 40, 40), Color.Color8(252, 191, 73)) * PixelDataEnums.ColorOffset : data.Color;
                            simulationImages[x, y].SetPixel(xx, yy, color);
                        }
                        else
                        {
                            // Покрас пустых пикселей в белый под размер кисти
                            //if ((bounds.Position.X + xx).Distance(MousePoint.X) < Radius && (bounds.Position.Y + yy).Distance(MousePoint.Y) < Radius)
                            //{
                            //    simulationImages[x, y].SetPixel(xx, yy, Colors.White);
                            //}
                            //else
                            //{
                            //    simulationImages[x, y].SetPixel(xx, yy, bg_color);
                            //}
                            simulationImages[x, y].SetPixel(xx, yy, bg_color);
                        }
                    }
                }
                UpdateTexture(x, y);
            }
        }
    }

    //public void UpdateAllTextures()
    //{
    //    for (int x = 0; x < ChunksCount.X; x++)
    //    {
    //        for (int y = 0; y < ChunksCount.Y; y++)
    //        {
    //            UpdateTexture(x, y);
    //        }
    //    }
    //}
    public void UpdateTexture(int x, int y)
    {
        simulationTextures[x, y].Update(simulationImages[x, y]);
    }

    public override void _Process(double delta)
    {
#if DEBUG
        Debugger.DisplayText($"TPS: {debug_lastTicks} / {ticksPerSecond}"); // TICKS PER SECOND
        Debugger.DisplayText($"SLL: {simulationUpdater.SkipLostLoops}"); // SKIP LOST LOOPS
        Debugger.DisplayText($"Paused: {Paused}"); // PAUSED
        Debugger.DisplayText($"Selected Pixel Type: {PixelDataEnums.Names[selectedPixelType]}"); // SELECTED PIXEL TYPE
        Debugger.DisplayText($"Pointer: {MousePoint}"); // MOUSE POINT
        Debugger.DisplayText($"Simulation Size: {SimulationSize}"); // SIMULATION SIZE
        Debugger.DisplayText($"Chunks: {ChunksCount.X}x{ChunksCount.Y} ({ChunksCount.X * ChunksCount.Y})"); // CHUNKS
        Vector2I chunkIndex = PixelBoxPhysics.GetChunkByPoint(MousePoint);
        Debugger.DisplayText($"Pointer Chunk: {chunkIndex}"); // MOUSE POINT CHUNK

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
        using var image = Image.Create(SimulationSize.X, SimulationSize.Y, false, Image.Format.Rgba8);
        for (int x = 0; x < ChunksCount.X; x++)
        {
            for (int y = 0; y < ChunksCount.Y; y++)
            {
                image.BlitRect(simulationImages[x, y], new(Vector2I.Zero, simulationImages[x, y].GetSize()), ChunkRects[x, y].Position);
            }
        }
        for (int x = 0; x < SimulationSize.X; x++)
        {
            for (int y = 0; y < SimulationSize.Y; y++)
            {
                if (SimulationData[x, y].HasPixel() == false) image.SetPixel(x, y, Colors.Transparent);
            }
        }
        var err = image.SavePng($"{PathToScreenshotFolder}{index}.png");
        if (err > 0) $"Failed to make screenshot! ({err})".LogError();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Обновление чанка и всех соседних чанков для каждого пикселя кисти
        //if (@event is InputEventMouseMotion && IsMousePointValid)
        //{
        //    for (int x = -Radius + 1; x < Radius; x++)
        //    {
        //        for (int y = -Radius + 1; y < Radius; y++)
        //        {
        //            var chunk = PixelBoxPhysics.GetChunkByPoint(MousePoint + new Vector2I(x, y));
        //            foreach (var item in MyMath.IAxis4)
        //            {
        //                if (RequestToUpdates.IsValid(chunk.X + item.X, chunk.Y + item.Y)) PixelBoxPhysics.SetRequestToUpdate(chunk.X + item.X, chunk.Y + item.Y);
        //            }
        //            if (RequestToUpdates.IsValid(chunk.X, chunk.Y)) PixelBoxPhysics.SetRequestToUpdate(chunk.X, chunk.Y);
        //        }
        //    }
        //}
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
                        PixelBoxPhysics.SetPixel(new(posX, posY), data);
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
                        PixelBoxPhysics.SetPixel(new(posX, posY), default);
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
    }

    public void Clear()
    {
        SimulationData = new PixelData[SimulationSize.X, SimulationSize.Y];
        RequestToUpdates = new bool[ChunksCount.X, ChunksCount.Y];
        for (int x = 0; x < ChunksCount.X; x++)
        {
            for (int y = 0; y < ChunksCount.Y; y++)
            {
                simulationImages[x, y].Fill(bg_color);
                UpdateTexture(x, y);
            }
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

        if (Paused == false)
        {
            UpdateChunks();
        }
    }
}
