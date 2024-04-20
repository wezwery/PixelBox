using Godot;
using PixelBox.Scenes;
using WezweryGodotTools;
using WezweryGodotTools.Extensions;
using static PixelBox.Scripts.Enums.PixelDataEnums;
using static PixelBox.Scripts.Enums.PixelDataIDs;

namespace PixelBox.Scripts;

public static class PixelBoxPhysics
{
    private const float smoke_chance = 1f;

    private static void SetRayPixels(PixelData dataToSet, Vector2I from, Vector2 axis, int distance, PixelData[,] simulationData)
    {
        for (int i = 0; i < distance; i++)
        {
            var pos = (from + axis * i).RoundToInt();
            if (simulationData.IsValid(pos.X, pos.Y) == false || simulationData[pos.X, pos.Y].HasPixel()) return;
            SetPixel(new(pos.X, pos.Y), dataToSet);
            simulationData[pos.X, pos.Y].Updated = true;
        }
    }

    public static void SetFire(Vector2I pos, bool fire)
    {
        MainGame.Instance.SimulationData[pos.X, pos.Y].Fire = fire;
        MainGame.Instance.SimulationData[pos.X, pos.Y].Updated = true;
    }
    public static Vector2I GetChunkByPoint(Vector2I point)
    {
        int x = 0;
        int y = 0;
        while (x * (MainGame.Instance.SimulationSize.X / MainGame.Instance.ChunksCount.X) < point.X)
        {
            x++;
        }
        while (y * (MainGame.Instance.SimulationSize.Y / MainGame.Instance.ChunksCount.Y) < point.Y)
        {
            y++;
        }
        return new(x == 0 ? x : x - 1, y == 0 ? y : y - 1);
    }
    public static void SetPixel(Vector2I pos, PixelData data)
    {
        var pixelsInChunks = MainGame.Instance.PixelsInChunks;
        var simulationData = MainGame.Instance.SimulationData;
        var index = GetChunkByPoint(pos);
        var empty = data == default;
        if ((empty && simulationData[pos.X, pos.Y] != default) || (empty == false && simulationData[pos.X, pos.Y] == default)) pixelsInChunks[index.X, index.Y] += empty ? -1 : 1;
        simulationData[pos.X, pos.Y] = data;
        if (empty == false)
        {
            simulationData[pos.X, pos.Y].Updated = true;
        }
    }

    public static PixelData[,] Update(int chunkX, int chunkY)
    {
        var bounds = MainGame.Instance.ChunkRects[chunkX, chunkY];
        var simulationData = MainGame.Instance.SimulationData;
        for (int x = bounds.Position.X; x < bounds.End.X; x++)
        {
            for (int y = bounds.Position.Y; y < bounds.End.Y; y++)
            {
                if (simulationData[x, y].HasPixel() == false) continue;
                simulationData[x, y].Updated = false;
            }
        }

        bool IsValid(int x, int y) => simulationData.IsValid(x, y);
        bool HasPixel(int x, int y) => IsValid(x, y) && simulationData[x, y].HasPixel();
        bool IsFireOrSmokeOrNone(int x, int y) => IsValid(x, y) && (HasPixel(x, y) == false || simulationData[x, y].ID == FIRE_ID || simulationData[x, y].ID == SMOKE_ID);
        bool IsFluid(int x, int y) => HasPixel(x, y) && (simulationData[x, y].Material == PixelData.MaterialEnum.Fluid);
        bool IsGas(int x, int y) => HasPixel(x, y) && simulationData[x, y].Material == PixelData.MaterialEnum.Gas;
        bool IsAcidDestroyable(int x, int y) => HasPixel(x, y) && (simulationData[x, y].Material == PixelData.MaterialEnum.Static || simulationData[x, y].Material == PixelData.MaterialEnum.Sand || simulationData[x, y].Material == PixelData.MaterialEnum.HardSand);
        //bool IsFlamable(int x, int y) => HasPixel(x, y) && simulationData[x, y].Flamable;
        bool IsSupportedForFire(int x, int y) => IsFireOrSmokeOrNone(x - 1, y) || IsFireOrSmokeOrNone(x + 1, y) || IsFireOrSmokeOrNone(x, y - 1) || IsFireOrSmokeOrNone(x, y + 1);
        bool IsReadyToFire(int x, int y) => HasPixel(x, y) && simulationData[x, y].Flamable && MyMath.RandomPercent <= simulationData[x, y].GetChanceToFlame() && IsSupportedForFire(x, y);
        //bool IsUpdated(int x, int y) => HasPixel(x, y) && simulationData[x, y].Updated;
        bool IsValidAndEmpty(int x, int y) => IsValid(x, y) && HasPixel(x, y) == false;
        bool IsPixel(int x, int y, byte id) => HasPixel(x, y) && simulationData[x, y].ID == id;

        for (int x = bounds.Position.X; x < bounds.End.X; x++)
        {
            for (int y = bounds.Position.Y; y < bounds.End.Y; y++)
            {
                PixelData data = simulationData[x, y];
                if (data.HasPixel() == false || data.Updated) continue;
                data.Updated = true;
                SetPixel(new(x, y), default);
                Vector2I newPos = new(x, y);
                switch (data.Material)
                {
                    case PixelData.MaterialEnum.Static:
                        {
                            #region Структура
                            SetPixel(new(x, y), data);
                            switch (data.ID)
                            {
                                case CLOUD_ID:
                                    {
                                        if (MyMath.RandomPercent <= 1f && IsValidAndEmpty(x, y + 1))
                                        {
                                            SetPixel(new(x, y + 1), WATER);
                                        }
                                        break;
                                    }
                                case STORM_CLOUD_ID:
                                    {
                                        if (MyMath.RandomPercent <= 10f && IsValidAndEmpty(x, y + 1))
                                        {
                                            SetPixel(new(x, y + 1), WATER);
                                        }
                                        if (MyMath.RandomPercent <= 0.1f && IsValidAndEmpty(x, y + 1))
                                        {
                                            SetRayPixels(FIRE, new(x, y + 1), new(MyMath.Random(-0.5f, 0.5f), 1f), MyMath.Random(50, 1000), simulationData);
                                        }
                                        break;
                                    }
                                case BLACK_HOLE_ID:
                                    {
                                        if (HasPixel(x - 1, y) && simulationData[x - 1, y].ID != BLACK_HOLE_ID)
                                        {
                                            SetPixel(new(x - 1, y), default);
                                        }
                                        if (HasPixel(x + 1, y) && simulationData[x + 1, y].ID != BLACK_HOLE_ID)
                                        {
                                            SetPixel(new(x + 1, y), default);
                                        }
                                        if (HasPixel(x, y - 1) && simulationData[x, y - 1].ID != BLACK_HOLE_ID)
                                        {
                                            SetPixel(new(x, y - 1), default);
                                        }
                                        if (HasPixel(x, y + 1) && simulationData[x, y + 1].ID != BLACK_HOLE_ID)
                                        {
                                            SetPixel(new(x, y + 1), default);
                                        }
                                        break;
                                    }
                                case LIGHTER_ID:
                                    {
                                        if (IsValidAndEmpty(x, y - 1))
                                        {
                                            SetPixel(new(x, y - 1), FIRE);
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            break;
                        }
                    case PixelData.MaterialEnum.Sand:
                        {
                            #region Песок
                            newPos = SandSimulation(x, y, data);
                            #endregion
                            break;
                        }
                    case PixelData.MaterialEnum.HardSand:
                        {
                            #region Тяжелый песок
                            newPos = HardSandSimulation(x, y, data);
                            #endregion
                            break;
                        }
                    case PixelData.MaterialEnum.Gas:
                        {
                            #region Газы
                            newPos = GasSimulation(x, y, data);
                            switch (data.ID)
                            {
                                case STEAM_ID:
                                    {
                                        if (MyMath.RandomPercent <= 0.1f && (IsValid(newPos.X, newPos.Y - 1) == false || simulationData[newPos.X, newPos.Y].ID == 5))
                                        {
                                            SetPixel(new(newPos.X, newPos.Y), WATER);
                                        }
                                        break;
                                    }
                                case FIRE_ID:
                                    {
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X - 1, newPos.Y, WATER_ID))
                                        {
                                            SetPixel(new(newPos.X - 1, newPos.Y), STEAM);
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X + 1, newPos.Y, WATER_ID))
                                        {
                                            SetPixel(new(newPos.X + 1, newPos.Y), STEAM);
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X, newPos.Y - 1, WATER_ID))
                                        {
                                            SetPixel(new(newPos.X, newPos.Y - 1), STEAM);
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X, newPos.Y + 1, WATER_ID))
                                        {
                                            SetPixel(new(newPos.X, newPos.Y + 1), STEAM);
                                            continue;
                                        }

                                        if (IsReadyToFire(newPos.X - 1, newPos.Y))
                                        {
                                            SetFire(new(newPos.X - 1, newPos.Y), true);
                                        }
                                        if (IsReadyToFire(newPos.X + 1, newPos.Y))
                                        {
                                            SetFire(new(newPos.X + 1, newPos.Y), true);
                                        }
                                        if (IsReadyToFire(newPos.X, newPos.Y - 1))
                                        {
                                            SetFire(new(newPos.X, newPos.Y - 1), true);
                                        }
                                        if (IsReadyToFire(newPos.X, newPos.Y + 1))
                                        {
                                            SetFire(new(newPos.X, newPos.Y + 1), true);
                                        }
                                        if (MyMath.RandomPercent <= smoke_chance && IsValidAndEmpty(newPos.X, newPos.Y - 1))
                                        {
                                            SetPixel(new(newPos.X, newPos.Y - 1), SMOKE);
                                        }
                                        if (MyMath.RandomPercent <= 30f)
                                        {
                                            SetPixel(new(newPos.X, newPos.Y), default);
                                        }
                                        break;
                                    }
                            }
                            #endregion
                            break;
                        }
                    case PixelData.MaterialEnum.Fluid:
                        {
                            #region Жидкости
                            newPos = FluidSimulation(x, y, data);
                            switch (data.ID)
                            {
                                case ACID_ID:
                                    {
                                        if (MyMath.RandomPercent <= 5f && IsValid(newPos.X, newPos.Y + 1) && IsAcidDestroyable(newPos.X, newPos.Y + 1))
                                        {
                                            SetPixel(new(newPos.X, newPos.Y + 1), default);
                                            if (MyMath.RandomPercent <= 1f)
                                            {
                                                SetPixel(new(newPos.X, newPos.Y), default);
                                            }
                                        }

                                        break;
                                    }
                            }
                            #endregion
                            break;
                        }
                }
                #region Механика горящих пикселей
                if (simulationData[newPos.X, newPos.Y] == data && data.Fire)
                {
                    if (IsValidAndEmpty(newPos.X, newPos.Y - 1))
                    {
                        SetPixel(new(newPos.X, newPos.Y - 1), FIRE);
                    }
                    if (IsSupportedForFire(newPos.X, newPos.Y) == false)
                    {
                        data.Fire = false;
                    }
                    else
                    {
                        if (IsReadyToFire(newPos.X - 1, newPos.Y))
                        {
                            SetFire(new(newPos.X - 1, newPos.Y), true);
                        }
                        if (IsReadyToFire(newPos.X + 1, newPos.Y))
                        {
                            SetFire(new(newPos.X + 1, newPos.Y), true);
                        }
                        if (IsReadyToFire(newPos.X, newPos.Y - 1))
                        {
                            SetFire(new(newPos.X, newPos.Y - 1), true);
                        }
                        if (IsReadyToFire(newPos.X, newPos.Y + 1))
                        {
                            SetFire(new(newPos.X, newPos.Y + 1), true);
                        }
                        if (IsReadyToFire(newPos.X + 1, newPos.Y + 1))
                        {
                            SetFire(new(newPos.X + 1, newPos.Y + 1), true);
                        }
                        if (IsReadyToFire(newPos.X - 1, newPos.Y + 1))
                        {
                            SetFire(new(newPos.X - 1, newPos.Y + 1), true);
                        }
                        if (IsReadyToFire(newPos.X + 1, newPos.Y - 1))
                        {
                            SetFire(new(newPos.X + 1, newPos.Y - 1), true);
                        }
                        if (IsReadyToFire(newPos.X - 1, newPos.Y - 1))
                        {
                            SetFire(new(newPos.X - 1, newPos.Y - 1), true);
                        }
                        if (MyMath.RandomPercent <= simulationData[newPos.X, newPos.Y].GetChanceToDestroyByFire())
                        {
                            if (data.ID == WOOD_ID) SetPixel(new(newPos.X, newPos.Y), COAL with
                            {
                                Fire = true
                            });
                            else SetPixel(new(newPos.X, newPos.Y), default);
                            continue;
                        }
                    }
                    SetPixel(new(newPos.X, newPos.Y), data);
                }
                #endregion
            }
        }

        return simulationData;

        #region Симуляции
        Vector2I FluidSimulation(int x, int y, PixelData data)
        {
            int xAxis = MyMath.Random<int>(-1, 1);
            bool needSwap = MyMath.RandomPercent <= 5f;
            Vector2I newPos = new(x, y);
            Vector2I swapAxis = new(MyMath.Random(-1, 1), MyMath.Random(-1, 1));
            if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && HasPixel(x + xAxis, y + 1) == false)
            {
                SetPixel(new(x + xAxis, y + 1), data);
                newPos = new(x + xAxis, y + 1);
            }
            else if (needSwap && swapAxis.Any() && simulationData.IsValid(x + swapAxis.X, y + swapAxis.Y) && IsFluid(x + swapAxis.X, y + swapAxis.Y) && simulationData[x + swapAxis.X, y + swapAxis.Y].ID != data.ID)
            {
                SetPixel(new(x, y), simulationData[x + swapAxis.X, y + swapAxis.Y]);
                SetPixel(new(x + swapAxis.X, y + swapAxis.Y), data);
                newPos = new(x + swapAxis.X, y + swapAxis.Y);
            }
            else
            {
                int xSide = MyMath.RandomPercent <= 50f ? -1 : 1;
                if (simulationData.IsValid(x + xSide, y) && HasPixel(x + xSide, y) == false)
                {
                    SetPixel(new(x + xSide, y), data);
                    newPos = new(x + xSide, y);
                }
                else
                {
                    SetPixel(new(x, y), data);
                }
            }

            return newPos;
        }
        Vector2I GasSimulation(int x, int y, PixelData data)
        {
            bool needSwap = MyMath.RandomPercent <= 5f;
            Vector2I swapAxis = new(MyMath.Random(-1, 1), MyMath.Random(-1, 1));
            Vector2I axis = new(MyMath.RandomPercent <= 50f ? 0 : MyMath.RandomPercent <= 50f ? -1 : 1, MyMath.RandomPercent <= 75f ? -1 : 1);
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y - 1) && IsFluid(x, y - 1))
            {
                SetPixel(new(x, y), simulationData[x, y - 1]);
                SetPixel(new(x, y - 1), data);
                newPos = new(x, y - 1);
            }
            else if (simulationData.IsValid(x + axis.X, y + axis.Y) && HasPixel(x + axis.X, y + axis.Y) == false)
            {
                SetPixel(new(x + axis.X, y + axis.Y), data);
                newPos = new(x + axis.X, y + axis.Y);
            }
            else if (needSwap && swapAxis.Any() && simulationData.IsValid(x + swapAxis.X, y + swapAxis.Y) && IsGas(x + swapAxis.X, y + swapAxis.Y) && simulationData[x + swapAxis.X, y + swapAxis.Y].ID != data.ID)
            {
                SetPixel(new(x, y), simulationData[x + swapAxis.X, y + swapAxis.Y]);
                SetPixel(new(x + swapAxis.X, y + swapAxis.Y), data);
                newPos = new(x + swapAxis.X, y + swapAxis.Y);
            }
            else
            {
                SetPixel(new(x, y), data);
            }

            return newPos;
        }
        Vector2I SandSimulation(int x, int y, PixelData data)
        {
            int xAxis = MyMath.Random<int>(-1, 1);
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y + 1) && IsGas(x, y + 1))
            {
                SetPixel(new(x, y), simulationData[x, y + 1]);
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && IsGas(x + xAxis, y + 1))
            {
                SetPixel(new(x, y), simulationData[x + xAxis, y + 1]);
                SetPixel(new(x + xAxis, y + 1), data);
                newPos = new(x + xAxis, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && IsFluid(x, y + 1))
            {
                SetPixel(new(x, y), simulationData[x, y + 1]);
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && IsFluid(x + xAxis, y + 1))
            {
                SetPixel(new(x, y), simulationData[x + xAxis, y + 1]);
                SetPixel(new(x + xAxis, y + 1), data);
                newPos = new(x + xAxis, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && HasPixel(x + xAxis, y + 1) == false)
            {
                SetPixel(new(x + xAxis, y + 1), data);
                newPos = new(x + xAxis, y + 1);
            }
            else
            {
                SetPixel(new(x, y), data);
            }

            return newPos;
        }
        Vector2I HardSandSimulation(int x, int y, PixelData data)
        {
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y + 1) && IsFluid(x, y + 1))
            {
                SetPixel(new(x, y), simulationData[x, y + 1]);
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                SetPixel(new(x, y + 1), data);
                newPos = new(x, y + 1);
            }
            else
            {
                SetPixel(new(x, y), data);
            }

            return newPos;
        }
        #endregion
    }
}
