using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public static class PCGUtilities
{
    public static bool[] ConvertTo1D(bool[,] input)
    {
        bool[] arr = new bool[input.Length];
        for(int x = 0; x < input.GetLength(0); x ++)
            for (int y = 0; y < input.GetLength(1); y++)
                arr[x + y * input.GetLength(0)] = input[x, y];
        return arr;
    }

    /// <summary>
    /// Return a 2D boolean array filtering an input 2D boolean array by limiting the size of holes
    /// </summary>
    /// <param name="input_levels">2D bool array of levels to check</param>
    /// <param name="min_fill">Bool on if to check for maximum or minimum fill count</param>
    /// <param name="quantity">Int on size of fill count allowed or dissallowed</param>
    /// <returns>2D boolean array of filtered levels</returns>
    public static bool[,] FillCountPass(bool[,] input_levels, bool min_fill, int quantity)
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
        Dictionary<Vector2Int, bool> checked_tiles = new Dictionary<Vector2Int, bool>();
        List<Vector2Int> cave_tiles = new List<Vector2Int>();

        for (int x = 0; x < input_levels.GetLength(0); x++)
        {
            for (int y = 0; y < input_levels.GetLength(1); y++)
            {
                if (!input_levels[x, y] || !checked_tiles.ContainsKey(new Vector2Int(x, y)))
                    break;

                // Found new cave tile
                List<Vector2Int> new_tiles = new List<Vector2Int> { new Vector2Int(x, y) };
                cave_tiles.Add(new Vector2Int(x, y));
                while (new_tiles.Count != 0)
                {
                    temp_tiles = new List<Vector2Int>(new_tiles);
                    new_tiles.Clear();
                    foreach (Vector2Int tile in temp_tiles)
                    {
                        foreach (Vector2Int dir in directions)
                        {
                            Vector2Int cur_vec = tile + dir;
                            if (cur_vec.x < 0 || cur_vec.y < 0) continue;
                            if (cur_vec.x >= input_levels.GetLength(0) || cur_vec.y >= input_levels.GetLength(1)) continue;
                            if (!input_levels[cur_vec.x, cur_vec.y]) continue;
                            if (checked_tiles.ContainsKey(cur_vec)) continue;
                            new_tiles.Add(cur_vec);
                            checked_tiles.Add(cur_vec, true);
                            cave_tiles.Add(cur_vec);
                        }
                    }
                }
                if ((min_fill && cave_tiles.Count < quantity) || (!min_fill && cave_tiles.Count > quantity))
                    foreach (Vector2Int tile in cave_tiles)
                        fill_array[tile.x, tile.y] = false;
                cave_tiles.Clear();
            }
        }
        return fill_array;
    }

    /// <summary>
    /// Return a 2D boolean array on if cells from a float array pass a threshold check
    /// </summary>
    /// <param name="input_levels">2D float array of levels to check</param>
    /// <param name="threshold">Float of where the level threshold is</param>
    /// <returns>2D boolean array of threshold passes or fails</returns>
    public static bool[,] ThresholdPass(float[,] input_levels, float threshold)
    {
        float time = Time.realtimeSinceStartup;
        Vector2Int size = new Vector2Int(input_levels.GetLength(0), input_levels.GetLength(1));
        bool[,] threshold_levels = new bool[size.x, size.y];
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                threshold_levels[x, y] = input_levels[x, y] <= threshold;
        //Debug.Log($"It took {Time.realtimeSinceStartup - time} seconds to calculate the threshold pass.");
        return threshold_levels;
    }

    /// <summary>
    /// Feather off float array levels based on crop values
    /// </summary>
    /// <param name="input_levels">2D float arry of input map</param>
    /// <param name="start_point">Vector2Int of crop start position (bottom left)</param>
    /// <param name="end_point">Vector2Int of crop end position (top right)</param>
    /// <param name="border_scale">Int of border scale (how far to feather)</param>
    /// <param name="top_feather">Bool if needing to feather top edge</param>
    /// <param name="right_feather">Bool if needing to feather right edge</param>
    /// <param name="bottom_feather">Bool if needing to feather bottom edge</param>
    /// <param name="left_feather">Bool if needing to feather left edge</param>
    /// <param name="root_falloff">Bool if feathering falls of non linearly</param>
    /// <returns>2D float array of feathered output</returns>
    public static float[,] FeatherLevels(float[,] input_levels, Vector2Int start_point, Vector2Int end_point, int border_scale, bool top_feather, bool right_feather, bool bottom_feather, bool left_feather, bool root_falloff)
    {
        float[,] new_levels = input_levels;
        // Set Levels Outside Bounds To 1
        for (int x = 0; x < start_point.x; x++) for (int y = 0; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;
        for (int x = end_point.x; x < new_levels.GetLength(0); x++) for (int y = 0; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;
        for (int x = start_point.x; x < end_point.x; x++) for (int y = 0; y < start_point.y; y++) new_levels[x, y] = 1;
        for (int x = start_point.x; x < end_point.x; x++) for (int y = end_point.y; y < new_levels.GetLength(1); y++) new_levels[x, y] = 1;

        float level_delta;
        for (int x = start_point.x; x < end_point.x; x++)
        {
            for (int y = start_point.y; y < end_point.y; y++)
            {
                level_delta = 1;
                // Calculate falloff based on distance from nearest bound
                if (y < border_scale && bottom_feather) level_delta *= (y - start_point.y) / (float)border_scale;
                if (y >= end_point.y - border_scale && top_feather) level_delta *= (end_point.y - y - 1) / (float)border_scale;                
                if (x < border_scale && left_feather) level_delta *= (x - start_point.x) / (float)border_scale;
                if (x >= end_point.x - border_scale && right_feather) level_delta *= (end_point.x - x - 1) / (float)border_scale;                

                // Alter level delta based on falloff type
                if (root_falloff) level_delta = Mathf.Sqrt(level_delta);
                new_levels[x, y] += 1 - level_delta;
            }
        }
        return new_levels;
    }
}
