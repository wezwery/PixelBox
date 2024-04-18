using Godot;
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
            simulationData[pos.X, pos.Y] = dataToSet;
            simulationData[pos.X, pos.Y].Updated = true;
        }
    }

    public static PixelData[,] Update(Vector2I simulationSize, PixelData[,] simulationData)
    {
        for (int x = 0; x < simulationSize.X; x++)
        {
            for (int y = 0; y < simulationSize.Y; y++)
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
        bool IsUpdated(int x, int y) => HasPixel(x, y) && simulationData[x, y].Updated;
        bool IsValidAndEmpty(int x, int y) => IsValid(x, y) && HasPixel(x, y) == false;
        bool IsPixel(int x, int y, byte id) => HasPixel(x, y) && simulationData[x, y].ID == id;

        for (int x = 0; x < simulationSize.X; x++)
        {
            for (int y = 0; y < simulationSize.Y; y++)
            {
                PixelData data = simulationData[x, y];
                if (data.HasPixel() == false || data.Updated) continue;
                data.Updated = true;
                simulationData[x, y] = default;
                if (data.Fire)
                {
                    if (IsValidAndEmpty(x, y - 1))
                    {
                        simulationData[x, y - 1] = FIRE;
                        simulationData[x, y - 1].Updated = true;
                    }
                    if (IsSupportedForFire(x, y) == false)
                    {
                        data.Fire = false;
                    }
                    else
                    {
                        simulationData[x, y] = data;
                        if (IsReadyToFire(x - 1, y))
                        {
                            simulationData[x - 1, y].Fire = true;
                            simulationData[x - 1, y].Updated = true;
                        }
                        if (IsReadyToFire(x + 1, y))
                        {
                            simulationData[x + 1, y].Fire = true;
                            simulationData[x + 1, y].Updated = true;
                        }
                        if (IsReadyToFire(x, y - 1))
                        {
                            simulationData[x, y - 1].Fire = true;
                            simulationData[x, y - 1].Updated = true;
                        }
                        if (IsReadyToFire(x, y + 1))
                        {
                            simulationData[x, y + 1].Fire = true;
                            simulationData[x, y + 1].Updated = true;
                        }
                        if (IsReadyToFire(x + 1, y + 1))
                        {
                            simulationData[x + 1, y + 1].Fire = true;
                            simulationData[x + 1, y + 1].Updated = true;
                        }
                        if (IsReadyToFire(x - 1, y + 1))
                        {
                            simulationData[x - 1, y + 1].Fire = true;
                            simulationData[x - 1, y + 1].Updated = true;
                        }
                        if (IsReadyToFire(x + 1, y - 1))
                        {
                            simulationData[x + 1, y - 1].Fire = true;
                            simulationData[x + 1, y - 1].Updated = true;
                        }
                        if (IsReadyToFire(x - 1, y - 1))
                        {
                            simulationData[x - 1, y - 1].Fire = true;
                            simulationData[x - 1, y - 1].Updated = true;
                        }
                        if (MyMath.RandomPercent <= simulationData[x, y].GetChanceToDestroyByFire())
                        {
                            if (data.ID == WOOD_ID) simulationData[x, y] = SMOKE;
                            else simulationData[x, y] = default;
                            continue;
                        }
                    }
                }
                switch (data.Material)
                {
                    case PixelData.MaterialEnum.Static:
                        {
                            simulationData[x, y] = data;
                            switch (data.ID)
                            {
                                case CLOUD_ID:
                                    {
                                        if (MyMath.RandomPercent <= 1f && IsValidAndEmpty(x, y + 1))
                                        {
                                            simulationData[x, y + 1] = WATER;
                                            simulationData[x, y + 1].Updated = true;
                                        }
                                        break;
                                    }
                                case STORM_CLOUD_ID:
                                    {
                                        if (MyMath.RandomPercent <= 10f && IsValidAndEmpty(x, y + 1))
                                        {
                                            simulationData[x, y + 1] = WATER;
                                            simulationData[x, y + 1].Updated = true;
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
                                            simulationData[x - 1, y] = default;
                                        }
                                        if (HasPixel(x + 1, y) && simulationData[x + 1, y].ID != BLACK_HOLE_ID)
                                        {
                                            simulationData[x + 1, y] = default;
                                        }
                                        if (HasPixel(x, y - 1) && simulationData[x, y - 1].ID != BLACK_HOLE_ID)
                                        {
                                            simulationData[x, y - 1] = default;
                                        }
                                        if (HasPixel(x, y + 1) && simulationData[x, y + 1].ID != BLACK_HOLE_ID)
                                        {
                                            simulationData[x, y + 1] = default;
                                        }
                                        break;
                                    }
                                case LIGHTER_ID:
                                    {
                                        if (IsValidAndEmpty(x, y - 1))
                                        {
                                            simulationData[x, y - 1] = FIRE;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    case PixelData.MaterialEnum.Sand:
                        {
                            var newPos = SandSimulation(simulationData, x, y, data);
                            break;
                        }
                    case PixelData.MaterialEnum.HardSand:
                        {
                            var newPos = HardSandSimulation(simulationData, x, y, data);
                            break;
                        }
                    case PixelData.MaterialEnum.Gas:
                        {
                            var newPos = GasSimulation(simulationData, x, y, data);
                            switch (data.ID)
                            {
                                case STEAM_ID:
                                    {
                                        if (MyMath.RandomPercent <= 0.1f && (IsValid(newPos.X, newPos.Y - 1) == false || simulationData[newPos.X, newPos.Y].ID == 5))
                                        {
                                            simulationData[newPos.X, newPos.Y] = WATER;
                                            simulationData[newPos.X, newPos.Y].Updated = true;
                                        }
                                        break;
                                    }
                                case FIRE_ID:
                                    {
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X - 1, newPos.Y, WATER_ID))
                                        {
                                            simulationData[newPos.X - 1, newPos.Y] = STEAM;
                                            simulationData[newPos.X - 1, newPos.Y].Updated = true;
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X + 1, newPos.Y, WATER_ID))
                                        {
                                            simulationData[newPos.X + 1, newPos.Y] = STEAM;
                                            simulationData[newPos.X + 1, newPos.Y].Updated = true;
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X, newPos.Y - 1, WATER_ID))
                                        {
                                            simulationData[newPos.X, newPos.Y - 1] = STEAM;
                                            simulationData[newPos.X, newPos.Y - 1].Updated = true;
                                            continue;
                                        }
                                        if (MyMath.RandomPercent <= 5f && IsPixel(newPos.X, newPos.Y + 1, WATER_ID))
                                        {
                                            simulationData[newPos.X, newPos.Y + 1] = STEAM;
                                            simulationData[newPos.X, newPos.Y + 1].Updated = true;
                                            continue;
                                        }

                                        if (IsReadyToFire(newPos.X - 1, newPos.Y))
                                        {
                                            simulationData[newPos.X - 1, newPos.Y].Fire = true;
                                        }
                                        if (IsReadyToFire(newPos.X + 1, newPos.Y))
                                        {
                                            simulationData[newPos.X + 1, newPos.Y].Fire = true;
                                        }
                                        if (IsReadyToFire(newPos.X, newPos.Y - 1))
                                        {
                                            simulationData[newPos.X, newPos.Y - 1].Fire = true;
                                        }
                                        if (IsReadyToFire(newPos.X, newPos.Y + 1))
                                        {
                                            simulationData[newPos.X, newPos.Y + 1].Fire = true;
                                        }
                                        if (MyMath.RandomPercent <= smoke_chance && IsValidAndEmpty(newPos.X, newPos.Y - 1))
                                        {
                                            simulationData[newPos.X, newPos.Y - 1] = SMOKE;
                                        }
                                        if (MyMath.RandomPercent <= 30f)
                                        {
                                            simulationData[newPos.X, newPos.Y] = default;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    case PixelData.MaterialEnum.Fluid:
                        {
                            var newPos = FluidSimulation(simulationData, x, y, data);
                            switch (data.ID)
                            {
                                case ACID_ID:
                                    {
                                        if (MyMath.RandomPercent <= 5f && IsValid(newPos.X, newPos.Y + 1) && IsAcidDestroyable(newPos.X, newPos.Y + 1))
                                        {
                                            simulationData[newPos.X, newPos.Y + 1] = default;
                                            if (MyMath.RandomPercent <= 1f)
                                            {
                                                simulationData[newPos.X, newPos.Y] = default;
                                            }
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }

        return simulationData;

        Vector2I FluidSimulation(PixelData[,] simulationData, int x, int y, PixelData data)
        {
            int xAxis = MyMath.Random<int>(-1, 1);
            bool needSwap = MyMath.RandomPercent <= 5f;
            Vector2I newPos = new(x, y);
            Vector2I swapAxis = new(MyMath.Random(-1, 1), MyMath.Random(-1, 1));
            if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && HasPixel(x + xAxis, y + 1) == false)
            {
                simulationData[x + xAxis, y + 1] = data;
                newPos = new(x + xAxis, y + 1);
            }
            else if (needSwap && swapAxis.Any() && simulationData.IsValid(x + swapAxis.X, y + swapAxis.Y) && IsFluid(x + swapAxis.X, y + swapAxis.Y) && simulationData[x + swapAxis.X, y + swapAxis.Y].ID != data.ID)
            {
                simulationData[x, y] = simulationData[x + swapAxis.X, y + swapAxis.Y];
                simulationData[x + swapAxis.X, y + swapAxis.Y] = data;
                newPos = new(x + swapAxis.X, y + swapAxis.Y);
            }
            else
            {
                int xSide = MyMath.RandomPercent <= 50f ? -1 : 1;
                if (simulationData.IsValid(x + xSide, y) && HasPixel(x + xSide, y) == false)
                {
                    simulationData[x + xSide, y] = data;
                    newPos = new(x + xSide, y);
                }
                else
                {
                    simulationData[x, y] = data;
                }
            }

            return newPos;
        }
        Vector2I GasSimulation(PixelData[,] simulationData, int x, int y, PixelData data)
        {
            bool needSwap = MyMath.RandomPercent <= 5f;
            Vector2I swapAxis = new(MyMath.Random(-1, 1), MyMath.Random(-1, 1));
            Vector2I axis = new(MyMath.RandomPercent <= 50f ? 0 : MyMath.RandomPercent <= 50f ? -1 : 1, MyMath.RandomPercent <= 75f ? -1 : 1);
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y - 1) && IsFluid(x, y - 1))
            {
                simulationData[x, y] = simulationData[x, y - 1];
                simulationData[x, y - 1] = data;
                newPos = new(x, y - 1);
            }
            else if (simulationData.IsValid(x + axis.X, y + axis.Y) && HasPixel(x + axis.X, y + axis.Y) == false)
            {
                simulationData[x + axis.X, y + axis.Y] = data;
                newPos = new(x + axis.X, y + axis.Y);
            }
            else if (needSwap && swapAxis.Any() && simulationData.IsValid(x + swapAxis.X, y + swapAxis.Y) && IsGas(x + swapAxis.X, y + swapAxis.Y) && simulationData[x + swapAxis.X, y + swapAxis.Y].ID != data.ID)
            {
                simulationData[x, y] = simulationData[x + swapAxis.X, y + swapAxis.Y];
                simulationData[x + swapAxis.X, y + swapAxis.Y] = data;
                newPos = new(x + swapAxis.X, y + swapAxis.Y);
            }
            else
            {
                simulationData[x, y] = data;
            }

            return newPos;
        }
        Vector2I SandSimulation(PixelData[,] simulationData, int x, int y, PixelData data)
        {
            int xAxis = MyMath.Random<int>(-1, 1);
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y + 1) && IsGas(x, y + 1))
            {
                simulationData[x, y] = simulationData[x, y + 1];
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && IsGas(x + xAxis, y + 1))
            {
                simulationData[x, y] = simulationData[x + xAxis, y + 1];
                simulationData[x + xAxis, y + 1] = data;
                newPos = new(x + xAxis, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && IsFluid(x, y + 1))
            {
                simulationData[x, y] = simulationData[x, y + 1];
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && IsFluid(x + xAxis, y + 1))
            {
                simulationData[x, y] = simulationData[x + xAxis, y + 1];
                simulationData[x + xAxis, y + 1] = data;
                newPos = new(x + xAxis, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x + xAxis, y + 1) && HasPixel(x + xAxis, y + 1) == false)
            {
                simulationData[x + xAxis, y + 1] = data;
                newPos = new(x + xAxis, y + 1);
            }
            else
            {
                simulationData[x, y] = data;
            }

            return newPos;
        }
        Vector2I HardSandSimulation(PixelData[,] simulationData, int x, int y, PixelData data)
        {
            Vector2I newPos = new(x, y);
            if (simulationData.IsValid(x, y + 1) && IsFluid(x, y + 1))
            {
                simulationData[x, y] = simulationData[x, y + 1];
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else if (simulationData.IsValid(x, y + 1) && HasPixel(x, y + 1) == false)
            {
                simulationData[x, y + 1] = data;
                newPos = new(x, y + 1);
            }
            else
            {
                simulationData[x, y] = data;
            }

            return newPos;
        }
    }
}
