using UnityEngine;

public static class Noise
{
    // Help from Lejynn (https://www.youtube.com/watch?v=XpG3YqUkCTY)
    // and Sebastian Lague (https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3)
    public static float[,] Generate2DLevels(Vector2Int size, int octaves, float frequency, int seed, float persistance, float lacunarity, Vector2 scale, Vector2 offset)
    {
        float[,] map_levels = new float[size.x, size.y];

        Vector2[] octave_offsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i ++) 
            octave_offsets[i] = new Vector2((seed*64546) % 100000 + offset.x, (seed * 98413) % 100000 - offset.y);

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
                float temp_freq = frequency;
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

        for (int x = 0; x < size.x; x++) for (int y = 0; y < size.y; y++) map_levels[x, y] = Mathf.InverseLerp(min_value, max_value, map_levels[x, y]);

        return map_levels;
    }

    public static float[,] Generate2DLevels(Vector2Int size, WaveVariables properties)
    {
        return Generate2DLevels(size, properties.octaves, properties.frequency, properties.seed, properties.persistance, properties.lacunarity, properties.scale, properties.offset);
    }
    
    public static float[] Generate1DLevels(int size, WaveVariables properties)
    {
        return Generate1DLevels(size, properties.octaves, properties.frequency, properties.seed, properties.persistance, properties.lacunarity, properties.scale, properties.offset);
    }

    public static float[] Generate1DLevels(int size, int octaves, float frequency, int seed, float persistance, float lacunarity, Vector2 scale, Vector2 offset)
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
            float temp_freq = frequency;
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
}
