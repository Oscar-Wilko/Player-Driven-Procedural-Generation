using System.Collections.Generic;
using UnityEngine;

#region WFC
public struct WFCInput
{
    public Ruleset ruleset;
    public int width;
    public int height;
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
    public int ID;
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