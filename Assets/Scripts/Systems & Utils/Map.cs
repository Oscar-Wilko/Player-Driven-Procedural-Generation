using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct TileAsset
{
    public Tile tile;
    public int ID;
    public string name;
}

public class Map : MonoBehaviour
{
    [Header("Wave Variables")]
    public WaveVariables dirt_generation;
    public WaveVariables mixed_dirt_generation;
    public WaveVariables stone_generation;
    public WaveVariables cave_generation;
    public WaveVariables surface_cave_generation;
    [Header("Map Variables")]
    public Tilemap tilemap;
    public Vector2Int map_size;
    public Vector2Int map_offset;
    [Header("Generation Variables")]
    // Heights
    public int max_dirt_height;
    public int min_dirt_height;
    public int stone_height;
    // Thresholds
    public float cave_threshold;
    public float surface_cave_threshold;
    public float mixed_dirt_threshold;
    public float stone_threshold;
    // Other
    public int minimum_cave_fill;
    [Header("Tilemap Assets")]
    public List<TileAsset> tiles;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        tilemap.ClearAllTiles();

        // Gather necessary tiles
        Tile stone_tile = GetTileOfName("Stone");
        Tile dirt_tile = GetTileOfName("Dirt");
        Tile grass_tile = GetTileOfName("Grass");

        // DIRT LAYER
        // Convert blocks from perlin dirt down to min stone all to dirt, with surface blocks being grass
        float[] dirt_layer = Noise.Generate1DLevels(map_size.x, dirt_generation);
        dirt_generation = RandomizeSeed(dirt_generation);
        for (int x = 0; x < map_size.x; x ++)
        {
            int x_height = Mathf.RoundToInt(dirt_layer[x] * (max_dirt_height - min_dirt_height) + min_dirt_height);
            for (int y = stone_height; y <= x_height; y++)
            {
                tilemap.SetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0), x_height == y ? grass_tile :dirt_tile);
            }
        }

        // STONE LAYER
        // Convert blocks from perlin stone down to bottom of map all to stone
        tilemap.BoxFill(new Vector3Int(map_offset.x, map_offset.y, 0), stone_tile, map_offset.x, map_offset.y, map_offset.x + map_size.x, map_offset.y + stone_height - 1);
        float[,] stone_levels = Noise.Generate2DLevels(map_size, stone_generation);
        stone_levels = PCGUtilities.FeatherLevels(stone_levels, Vector2Int.zero, new Vector2Int(map_size.x, max_dirt_height), max_dirt_height-stone_height, true, false, true);
        bool[,] stone_levels_bool = PCGUtilities.ThresholdPass(stone_levels, stone_threshold);
        stone_generation = RandomizeSeed(stone_generation);

        for (int x = 0; x < map_size.x; x++)
        {
            for (int y = 0; y < max_dirt_height; y++)
            {
                if (!stone_levels_bool[x, y]) continue;
                if (tilemap.GetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0)) != dirt_tile) continue;

                tilemap.SetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0), stone_tile);
            }
        }

        // DIRT IN STONE
        // Add small patches of dirt across the whole map
        float[,] mixed_dirt_levels = Noise.Generate2DLevels(map_size, mixed_dirt_generation);
        mixed_dirt_levels = PCGUtilities.FeatherLevels(mixed_dirt_levels, Vector2Int.zero, new Vector2Int(map_size.x, min_dirt_height), 16, true, false, true);
        bool[,] mixed_dirt_levels_bool = PCGUtilities.ThresholdPass(mixed_dirt_levels, mixed_dirt_threshold);
        mixed_dirt_generation = RandomizeSeed(mixed_dirt_generation);

        for (int x = 0; x < map_size.x; x++)
        {
            for (int y = 0; y < stone_height; y++)
            {
                if (!mixed_dirt_levels_bool[x, y]) continue;
                if (tilemap.GetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0)) != stone_tile) continue;
                tilemap.SetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0), dirt_tile);
            }
        }

        // CAVES
        // Add holes to stone layer
        float[,] cave_levels = Noise.Generate2DLevels(map_size, cave_generation);
        cave_levels = PCGUtilities.FeatherLevels(cave_levels, Vector2Int.zero, new Vector2Int(map_size.x,stone_height+32), 16, true, false, false);
        bool[,] cave_levels_bool = PCGUtilities.ThresholdPass(cave_levels,cave_threshold);
        cave_levels_bool = PCGUtilities.FillCountPass(cave_levels_bool, true, minimum_cave_fill);
        cave_generation = RandomizeSeed(cave_generation);

        for (int x = 0; x < map_size.x; x++)
        {
            for(int y = 0; y < stone_height+32; y++)
            {
                if (!cave_levels_bool[x, y]) continue;
                tilemap.SetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0), null);
            }
        }
        /*
        // Add caves tunneling from surface down to stone layer
        float[,] surface_cave_levels = Noise.Generate2DLevels(map_size, surface_cave_generation);
        bool[,] surface_cave_levels_bool = ThresholdPass(surface_cave_levels,surface_cave_threshold);
        surface_cave_generation = RandomizeSeed(surface_cave_generation);

        for (int x = 0; x < map_size.x; x++)
        {
            for(int y = stone_height - 64; y < max_dirt_height; y++)
            {
                if (!surface_cave_levels_bool[x, y]) continue;
                tilemap.SetTile(new Vector3Int(x + map_offset.x, y + map_offset.y, 0), null);
            }
        }*/
    }

    public Tile GetTileOfName(string name)
    {
        foreach(TileAsset asset in tiles) if (asset.name == name) return asset.tile;

        return null;
    }

    private WaveVariables RandomizeSeed(WaveVariables wave_var)
    {
        wave_var.seed = Mathf.Abs((wave_var.seed * 19851854) % 9900000) + 100000;
        return wave_var;
    }
}
