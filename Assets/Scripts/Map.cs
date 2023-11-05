using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Security.Cryptography;

[System.Serializable]
public struct TileAsset
{
    public Tile tile;
    public int ID;
    public string name;
}

[System.Serializable]
public struct WaveVariables
{
    [Range(0, 50)]
    public int octaves;
    public float frequency;
    [Range(100000, 10000000)]
    public int seed;
    [Range(0.0f, 1.0f)]
    public float persistance;
    [Range(0.0f, 1.0f)]
    public float lacunarity;
    public Vector2 scale;
    public Vector2 offset;
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
        stone_levels = FeatherLevels(stone_levels, Vector2Int.zero, new Vector2Int(map_size.x, max_dirt_height), max_dirt_height-stone_height, true, false, true);
        bool[,] stone_levels_bool = ThresholdPass(stone_levels, stone_threshold);
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
        mixed_dirt_levels = FeatherLevels(mixed_dirt_levels, Vector2Int.zero, new Vector2Int(map_size.x, min_dirt_height), 16, true, false, true);
        bool[,] mixed_dirt_levels_bool = ThresholdPass(mixed_dirt_levels, mixed_dirt_threshold);
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
        cave_levels = FeatherLevels(cave_levels, Vector2Int.zero, new Vector2Int(map_size.x,stone_height+32), 16, true, false, false);
        bool[,] cave_levels_bool = ThresholdPass(cave_levels,cave_threshold);
        cave_levels_bool = FillCountPass(cave_levels_bool, true, minimum_cave_fill);
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

    private float[,] FeatherLevels(float[,] input_levels, Vector2Int start_point, Vector2Int end_point, int border_scale, bool vertical_feather, bool horizontal_feather, bool root_falloff)
    {
        float[,] new_levels = input_levels;
        // Set Levels Outside Bounds To 1
        for (int x = 0; x < start_point.x; x++) for (int y = 0; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;
        for (int x = end_point.x; x < new_levels.GetLength(0); x++) for (int y = 0; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;
        for (int x = start_point.x; x < end_point.x; x++) for (int y = 0; y < start_point.y; y++) new_levels[x, y] = 1;
        for (int x = start_point.x; x < end_point.x; x++) for (int y = end_point.y; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;

        float level_delta;
        for(int x = start_point.x; x < end_point.x; x++)
        {
            for (int y = start_point.y; y < end_point.y; y++)
            {
                level_delta = 1;
                // Calculate falloff based on distance from nearest bound
                if (vertical_feather)
                {
                    if (y < border_scale) level_delta = (y-start_point.y) / (float)border_scale;
                    else if (y >= end_point.y - border_scale) level_delta = (end_point.y - y - 1) / (float)border_scale;
                }
                if (horizontal_feather)
                { 
                    if(x < border_scale) level_delta = (x-start_point.x) / (float)border_scale;
                    else if (x >= end_point.x - border_scale) level_delta = (end_point.x - x - 1) / (float)border_scale;
                }

                // Alter level delta based on falloff type
                if (root_falloff) level_delta = Mathf.Sqrt(level_delta);
                new_levels[x, y] += 1 - level_delta;
            }
        }
        return new_levels;
    }

    /// <summary>
    /// Return a 2D boolean array on if cells from a float array pass a threshold check
    /// </summary>
    /// <param name="input_levels">2D float array of levels to check</param>
    /// <param name="threshold">Float of where the level threshold is</param>
    /// <returns>2D boolean array of threshold passes or fails</returns>
    private bool[,] ThresholdPass(float[,] input_levels, float threshold)
    {
        bool[,] threshold_levels = new bool[input_levels.GetLength(0), input_levels.GetLength(1)];
        for(int x = 0; x < input_levels.GetLength(0); x ++)
        {
            for(int y = 0; y < input_levels.GetLength(1); y ++)
            {
                threshold_levels[x, y] = input_levels[x, y] <= threshold;
            }
        }
        return threshold_levels;
    }

    /// <summary>
    /// Return a 2D boolean array filtering an input 2D boolean array by limiting the size of holes
    /// </summary>
    /// <param name="input_levels">2D bool array of levels to check</param>
    /// <param name="min_fill">Bool on if to check for maximum or minimum fill count</param>
    /// <param name="quantity">Int on size of fill count allowed or dissallowed</param>
    /// <returns>2D boolean array of filtered levels</returns>
    private bool[,] FillCountPass(bool[,] input_levels,bool min_fill, int quantity)
    {
        bool[,] fill_array = input_levels;
        List<Vector2Int> directions = new List<Vector2Int>()
        {
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(1,0),
            new Vector2Int(0,-1)
        };
        List<Vector2Int> temp_tiles;
        Dictionary<Vector2Int,bool> checked_tiles = new Dictionary<Vector2Int, bool>();
        List<Vector2Int> cave_tiles = new List<Vector2Int>();

        for(int x = 0; x < input_levels.GetLength(0); x ++)
        {
            for(int y = 0; y < input_levels.GetLength(1); y ++)
            {
                if (input_levels[x,y] && !checked_tiles.ContainsKey(new Vector2Int(x,y)))
                {
                    // Found new cave tile
                    List<Vector2Int> new_tiles = new List<Vector2Int> { new Vector2Int(x, y) };
                    cave_tiles.Add(new Vector2Int(x, y));
                    while (new_tiles.Count != 0)
                    {
                        temp_tiles = new List<Vector2Int>(new_tiles);
                        new_tiles.Clear();
                        foreach(Vector2Int tile in temp_tiles)
                        {
                            foreach(Vector2Int dir in directions)
                            {
                                Vector2Int cur_vec = tile + dir;
                                if (cur_vec.x < 0 || cur_vec.y < 0) continue;
                                if (cur_vec.x >= input_levels.GetLength(0) || cur_vec.y >= input_levels.GetLength(1)) continue;
                                if (!input_levels[cur_vec.x, cur_vec.y]) continue;
                                if (checked_tiles.ContainsKey(cur_vec)) continue;
                                new_tiles.Add(cur_vec);
                                checked_tiles.Add(cur_vec,true);
                                cave_tiles.Add(cur_vec);
                            }
                        }
                    }
                    if (min_fill)
                    {
                        if (cave_tiles.Count < quantity) foreach (Vector2Int tile in cave_tiles) fill_array[tile.x, tile.y] = false;
                    }
                    else
                    {
                        if (cave_tiles.Count > quantity) foreach (Vector2Int tile in cave_tiles) fill_array[tile.x, tile.y] = false;
                    }
                    cave_tiles.Clear();
                }
            }
        }

        return fill_array;
    }
}
