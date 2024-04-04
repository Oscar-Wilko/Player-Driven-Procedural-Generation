using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#region WFC
public struct WFCInput
{
    public Ruleset ruleset;
    public int width;
    public int height;
    public MapInfo biome_map;
}

[System.Serializable]
public struct Ruleset
{
    public List<Rules> rules;
    public List<HeightVariance> height;
    public List<LengthVariance> length;
}

[System.Serializable]
public struct Rules
{
    public Biome biome;
    // WL => Whitelist
    public Rule up_rule;
    public Rule right_rule;
    public Rule down_rule;
    public Rule left_rule;
}

[System.Serializable]
public struct Rule
{
    public bool wl_all;
    public BiomeWeight[] wl;
    public Rule(bool all)
    {
        wl_all = all;
        int biomeCount = System.Enum.GetValues(typeof(Biome)).Length;
        wl = new BiomeWeight[biomeCount];
        for (int i = 0; i < biomeCount; i++)
            wl[i] = new BiomeWeight((Biome)i, 0);
    }
}

[System.Serializable]
public struct BiomeWeight
{
    public BiomeWeight(Biome b, float i)
    {
        biome = b;
        impact = i;
    }
    public Biome biome;
    public float impact;
}

[System.Serializable]
public struct HeightVariance
{
    public Biome biome;
    public float[] h_chance;
}

[System.Serializable]
public struct LengthVariance
{
    public Biome biome;
    public float[] l_chance;
}
#endregion
#region Canvas
[System.Serializable]
public struct BiomePixel
{
    public Biome biome;
    public Color colour;
}

[System.Serializable]
public struct MapInfo
{
    public Biome[] map;
    public int width;
    public int height;
}
#endregion
#region MapGen
[System.Serializable]
public struct TileDictionaryInstance
{
    public TileID id;
    public Tile tile;
}
[System.Serializable]
public struct TileInfo
{
    public TileID[] map;
    public Biome[] biome_map;
    public int width;
    public int height;
    public TileInfo(int x, int y)
    {
        map = new TileID[x * y];
        biome_map = new Biome[x * y];
        width = x;
        height = y;
    }
}

public struct ProceduralLayers
{
    public float[] surface_height;
    public bool[,] layer_cave;
    public bool[,] layer_large_clump;
    public bool[,] layer_small_clump;
    public bool[,] layer_dots;
    public bool[,] layer_water;
}

[System.Serializable]
public struct WaveVariables
{
    [Range(0, 50)]
    public int octaves;
    [Range(100000, 10000000)]
    public int seed;
    [Range(0.0f, 1.0f)]
    public float persistance;
    [Range(0.0f, 1.0f)]
    public float lacunarity;
    public Vector2 scale;
    public Vector2 offset;
    [Range(0.0f, 1.0f)]
    public float threshold;
}

[System.Serializable]
public struct TunnelVariables
{
    [Range(100000, 10000000)] public int seed;
    [Range(0, 25)] public int minTunnels;
    [Range(0, 25)] public int maxTunnels;
    [Range(1, 25)] public int minVertexCount;
    [Range(1, 25)] public int maxVertexCount;
    [Range(1, 100)] public float minVertexDist;
    [Range(1, 100)] public float maxVertexDist;
    [Range(0,1)] public float minRatio;
    [Range(0,1)] public float maxRatio;
    [Range(1, 8)] public int thickness;
}

[System.Serializable]
public struct Layers
{
    public int width;
    public int height;
    public bool[] layer_cave_without;
    public bool[] layer_cave_with;
    public bool[] layer_large_clump;
    public bool[] layer_small_clump;
    public bool[] layer_dots;
    public bool[] layer_water_before;
    public bool[] layer_water_after;
}
#endregion