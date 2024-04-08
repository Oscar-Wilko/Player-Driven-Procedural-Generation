using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NAME SHORTENED FROM BIOMEGENERATION TO BGEN TO MAKE CODING FASTER
public class BGen
{
    #region Biome Generation
    public static TileID StandardGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;
        else if (pos.y + 1 == (int)layers.surface_height[pos.x] 
            || pos.y + 2 == (int)layers.surface_height[pos.x]
            || pos.y + 3 == (int)layers.surface_height[pos.x]
            || pos.y + 4 == (int)layers.surface_height[pos.x])
            return TileID.Dirt;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Gravel;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.LowValOre;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Dirt;
        else
            return TileID.Stone;
    }

    public static TileID FrozenGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.ColdWater;
            else if (pos.y + 1 >= layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y + 1] && Random.Range(0, 100) > 70)
                return TileID.Icicles;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Snow;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.HardIce;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.ColdWater;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.HardIce;
        else
            return TileID.Ice;
    }

    public static TileID DesertGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Sand;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.HardSand;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.HardSand;
        else
            return TileID.Sand;
    }

    public static TileID SwampGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Mud;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Gravel;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.Mud;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Mud;
        else
            return TileID.Dirt;
    }

    public static TileID RockyGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Gravel;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.LowValOre;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Gravel;
        else
            return TileID.Stone;
    }

    public static TileID SharpRockyGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else if (pos.y + 1 >= layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y + 1] && Random.Range(0, 100) > 70)
                return TileID.Stalagtite;
            else if (pos.y - 1 >= layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y - 1] && Random.Range(0, 100) > 70)
                return TileID.Stalagmite;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Gravel;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.LowValOre;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Gravel;
        else
            return TileID.Stone;
    }

    public static TileID LavaGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Lava;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Magma;

        // Below Surface and out cave
        else if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Magma;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.Obsidian;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Magma;
        else
            return TileID.Molten;
    }

    public static TileID JungleGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] + 1)
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else if (pos.y + 1 >= layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y + 1] && Random.Range(0, 100) > 70)
                return TileID.Vines;
            else if (pos.y - 1 >= layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y - 1] && Random.Range(0, 100) > 70)
                return TileID.Flowers;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x] + 1)
            return Random.Range(0, 100) > 80 ? TileID.Bushes : TileID.None;
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;

        // Below Surface and out cave
        if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Gravel;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.Mud;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Mud;
        else
            return TileID.Stone;
    }

    public static TileID RadioactiveGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;

        // Below Surface and out cave
        if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.RadioactiveBlock;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.HighValOre;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Gravel;
        else
            return TileID.Stone;
    }

    public static TileID LushiousGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;

        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (layers.layer_water[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Wall;

        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;

        // Below Surface and out cave
        if (layers.layer_large_clump[pos.x, pos.y])
            return TileID.Dirt;
        else if (layers.layer_small_clump[pos.x, pos.y])
            return TileID.Grass;
        else if (layers.layer_dots[pos.x, pos.y])
            return TileID.Grass;
        else
            return TileID.Stone;
    }
    #endregion
}
