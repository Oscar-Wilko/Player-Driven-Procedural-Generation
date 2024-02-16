using System;
using UnityEngine;

public static class Noise
{
    #region Perlin
    // Help from Lejynn (https://www.youtube.com/watch?v=XpG3YqUkCTY)
    // and Sebastian Lague (https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3)
    /// <summary>
    /// Generate a two dimensional set of floats representing perlin noise generation
    /// </summary>
    /// <param name="size"></param> Array ouput size (width and height)
    /// <param name="octaves"></param> Number of times to repeat layering
    /// <param name="seed"></param> Input seed for generation
    /// <param name="persistance"></param> Remaining impact of layering
    /// <param name="lacunarity"></param> Frequency change of layering
    /// <param name="scale"></param> Multiplier for shinking or enlarging generations
    /// <param name="offset"></param> Position offset of generation
    /// <returns>2D float array of noise values</returns>
    public static float[,] Generate2DLevels(Vector2Int size, int octaves, int seed, float persistance, float lacunarity, Vector2 scale, Vector2 offset)
    {
        float time = Time.realtimeSinceStartup;
        float[,] map_levels = new float[size.x, size.y];

        System.Random rng = new System.Random(seed);
        Vector2[] octave_offsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i ++) 
            octave_offsets[i] = new Vector2(
                rng.Next(-100000,100000) + offset.x, 
                rng.Next(-100000,100000) - offset.y);

        if (scale.y < 0.0001f) 
            scale.y = 0.0001f;
        if (scale.x < 0.0001f) 
            scale.x = 0.0001f;

        float half_width = size.x * 0.5f;
        float half_height = size.y * 0.5f;

        float min_value = float.MaxValue;
        float max_value = float.MinValue;

        for (int x = 0; x < size.x; x ++)
        {
            for (int y = 0; y < size.y; y ++)
            {
                float temp_amp = 1;
                float temp_freq = 1;
                float noise_height = 0;

                for(int i = 0; i < octaves; i ++)
                {
                    float sample_x = (x - half_width) / scale.x * temp_freq + octave_offsets[i].x * temp_freq;
                    float sample_y = (y - half_height) / scale.y * temp_freq - octave_offsets[i].y * temp_freq;

                    float perlin_value = Mathf.PerlinNoise(sample_x, sample_y) * 2 - 1;
                    noise_height += perlin_value * temp_amp;
                    temp_amp *= persistance;
                    temp_freq *= lacunarity;
                }

                if (noise_height < min_value) 
                    min_value= noise_height;
                if (noise_height > max_value) 
                    max_value= noise_height;

                map_levels[x, y] = noise_height;
            }
        }

        for (int x = 0; x < size.x; x++) 
            for (int y = 0; y < size.y; y++) 
                map_levels[x, y] = Mathf.InverseLerp(min_value, max_value, map_levels[x, y]);
        //Debug.Log($"It took {Time.realtimeSinceStartup - time} seconds to generate the 2D Noise Map.");
        return map_levels;
    }

    /// <summary>
    /// Generate a two dimensional set of floats representing perlin noise generation
    /// </summary>
    /// <param name="size">Array output size (width and height)</param>
    /// <param name="properties">WaveVariables containing all generation based values</param>
    /// <returns>2D float array of noise values</returns>
    public static float[,] Generate2DLevels(Vector2Int size, WaveVariables properties)
    {
        return Generate2DLevels(size, properties.octaves, properties.seed, properties.persistance, properties.lacunarity, properties.scale, properties.offset);
    }
    
    /// <summary>
    /// Generate a one dimensional set of floats representing perlin noise generation
    /// </summary>
    /// <param name="size">Array output size</param>
    /// <param name="properties">WaveVariables containg all generation based values</param>
    /// <returns>Float array of noise values</returns>
    public static float[] Generate1DLevels(int size, WaveVariables properties)
    {
        return Generate1DLevels(size, properties.octaves, properties.seed, properties.persistance, properties.lacunarity, properties.scale, properties.offset);
    }
    
    /// <summary>
    /// Generate a one dimensional set of floats representing perlin noise generation
    /// </summary>
    /// <param name="size"></param> Array ouput size
    /// <param name="octaves"></param> Number of times to repeat layering
    /// <param name="seed"></param> Input seed for generation
    /// <param name="persistance"></param> Remaining impact of layering
    /// <param name="lacunarity"></param> Frequency change of layering
    /// <param name="scale"></param> Multiplier for shinking or enlarging generations
    /// <param name="offset"></param> Position offset of generation
    /// <returns>Float array of noise values</returns>
    public static float[] Generate1DLevels(int size, int octaves, int seed, float persistance, float lacunarity, Vector2 scale, Vector2 offset)
    {
        float[] map_levels = new float[size];

        System.Random rng = new System.Random(seed);
        float[] octave_offsets = new float[octaves];
        for (int i = 0; i < octaves; i++) 
            octave_offsets[i] = rng.Next(-100000, 100000) + offset.x;

        if (scale.x <= 0.0001f) 
            scale.x = 0.0001f;

        float half_width = size * 0.5f;

        float min_value = float.MaxValue;
        float max_value = float.MinValue;

        for (int x = 0; x < size; x++)
        {
            float temp_amp = 1;
            float temp_freq = 1;
            float noise_height = 0;

            for (int i = 0; i < octaves; i++)
            {
                float sample_x = (x - half_width) / scale.x * temp_freq + octave_offsets[i] * temp_freq;

                float perlin_value = Mathf.PerlinNoise(sample_x,0) * 2 - 1;
                noise_height += perlin_value * temp_amp;
                temp_amp *= persistance;
                temp_freq *= lacunarity;
            }

            if (noise_height < min_value) 
                min_value = noise_height;
            if (noise_height > max_value) 
                max_value = noise_height;

            map_levels[x] = noise_height;
        }

        for (int x = 0; x < size; x++)
            map_levels[x] = Mathf.InverseLerp(min_value, max_value, map_levels[x]);

        return map_levels;
    }
    #endregion
    #region Worley
    /// <summary>
    /// Generate a noise map based on random point locations
    /// </summary>
    /// <returns>2D float array of noise values</returns>
    public static float[,] GenerateWorley(Vector2Int size, int region_count, int seed, int closest_index, float max_dist)
    {
        float time = Time.realtimeSinceStartup;
        float[,] noise_map = new float[size.x, size.y];
        Vector2Int[] region_points = new Vector2Int[region_count];
        System.Random rng = new System.Random(seed);
        for (int i = 0; i < region_count; i++)
            region_points[i] = new Vector2Int(rng.Next(0, size.x), rng.Next(0, size.y));
        
        float[] region_dists = new float[region_count];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for(int i = 0; i < region_count; i ++)
                {
                    region_dists[i] = Vector2Int.Distance(new Vector2Int(x, y), region_points[i]);
                }
                Array.Sort(region_dists);
                noise_map[x, y] = 1 - Mathf.Clamp(region_dists[closest_index]/max_dist,0,1);
            }
        }
        Debug.Log("Generation of Worley output took " + (Time.realtimeSinceStartup - time) + " seconds.");
        return noise_map;
    }
    #endregion
    #region Voronoi

    /// <summary>
    /// Generate a region map based on random point locations
    /// </summary>
    /// <returns>2D int array of voronoi region values</returns>
    public static int[,] GenerateVoronoi(Vector2Int size, int region_count, int seed)
    {
        float time = Time.realtimeSinceStartup;
        int[,] region_map = new int[size.x, size.y];
        Vector2Int[] region_points = new Vector2Int[region_count];
        System.Random rng = new System.Random(seed);
        for (int i = 0; i < region_count; i++)
            region_points[i] = new Vector2Int(rng.Next(0, size.x), rng.Next(0, size.y));

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                float temp_dist = float.MaxValue;
                float calc_dist;
                for (int i = 0; i < region_count; i++)
                {
                    calc_dist = Vector2Int.Distance(new Vector2Int(x, y), region_points[i]);
                    if (calc_dist < temp_dist)
                    {
                        temp_dist = calc_dist;
                        region_map[x,y] = i;
                    }
                }
            }
        }
        Debug.Log("Generation of Voronoi output took " + (Time.realtimeSinceStartup - time) + " seconds.");

        return region_map;
    }

    #endregion
}
