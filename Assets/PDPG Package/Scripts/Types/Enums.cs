using UnityEngine;

public enum Biome
{
    None = 0,
    Standard = 1,
    Frozen = 2,
    Desert = 3,
    Swamp = 4,
    Rocky = 5,
    SharpRocky = 6,
    Lava = 7,
    //Water = 8, Removed
    //Ocean = 9, Removed
    Jungle = 8,
    Radioactive = 9,
    Luscious = 10
}
/// <summary>
/// TileID enum, numbered if IDs get removed
/// </summary>
public enum TileID
{
    None = 0,
    Stone = 1,
    Cobblestone = 2,
    Gravel = 3,
    Grass = 4,
    Dirt = 5,
    Mud = 6,
    Ice = 7,
    Sand = 8,
    HardSand = 9,
    Molten = 10,
    Magma = 11,
    Cloud = 12,
    Air = 13,
    BurriedItem = 14,
    TreeLog = 15,
    TreeLeaves = 16,
    Obsidian = 17,
    LowValOre = 18,
    HighValOre = 19,
    Vines = 20,
    Bushes = 21,
    Flowers = 22,
    RadioactiveBlock = 23,
    HardIce = 24,
    ColdWater = 25,
    Icicles = 26,
    Snow = 27,
    Water = 28,
    Wall = 29,
    Stalagtite = 30,
    Stalagmite = 31,
    Lava = 32,
    Crystal01 = 33,
    Crystal02 = 34,
    Crystal03 = 35,
    Crystal04 = 36,
    Crystal05 = 37,
    Crystal06 = 38,
}
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}
public enum NoiseType
{
    Cave01 = 0,
    Surface01 = 1,
    SmallClump01 = 2,
    LargeClump01 = 3,
    Dots01 = 4,
    Water01 = 5
}
public enum TunnelType
{
    Surface01 = 0,
    Vertical01 = 1,
    Horizontal01 = 2,
    Flat01 = 3
}
public enum NoiseVariable
{
    Seed,
    ScaleX,
    ScaleY,
    Octaves,
    Persistance,
    Lacunarity,
    Threshold
}
public enum TunnelVariable
{
    Seed,
    MinTunnels,
    MaxTunnels,
    MinVertexCount,
    MaxVertexCount,
    MinVertexDist,
    MaxVertexDist,
    MinRatio,
    MaxRatio,
    Thickness
}

public enum Layer
{
    FullGen,
    CaveWith,
    CaveWithout,
    LargeClump,
    SmallClump,
    Dots,
    WaterBefore,
    WaterAfter
}