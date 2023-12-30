using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Tweaking Values")]
    public int biome_size;
    public float transition_percentage;
    [Header("Procedural Factors")]
    public WaveVariables properties_surface;
    public int max_perc_height;
    public int min_perc_height;
    public WaveVariables properties_cave;
    public float cave_threshold;
    public WaveVariables properties_l_clump;
    public float l_clump_threshold;
    public WaveVariables properties_s_clump;
    public float s_clump_threshold;
    public WaveVariables properties_dots;
    public float dots_threshold;
    [Header("Tracking Values")]
    public TileInfo cur_output = new TileInfo(0,0);
    private bool generating = false;

    public IEnumerator GenerateMap(MapInfo info)
    {
        if (generating)
            yield break;
        // Declare Variables
        float t = Time.realtimeSinceStartup;
        float t_pre; float t_exp = 0; float t_con = 0;
        generating = true;

        // Expand biome array with transitions
        t_pre = Time.realtimeSinceStartup;
        Biome[,] b_map = ExpandMap(info);
        t_exp += Time.realtimeSinceStartup - t_pre;
        // Convert biome array to map
        t_pre = Time.realtimeSinceStartup;
        TileInfo t_map = ConvertBiomesToMap(b_map);
        t_con += Time.realtimeSinceStartup - t_pre;

        cur_output = t_map;
        generating = false;
        Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to generate map from biome map.\n" +
            "Expanding map took " + t_exp + " seconds. Converting map took " + t_con + " seconds.");
        yield break;
    }

    private Biome[,] ExpandMap(MapInfo inp_map)
    {
        Biome[,] map = new Biome[inp_map.width * biome_size, inp_map.height * biome_size];
        HashSet<Vector2Int> corners = GetBiomeCorners(inp_map);
        // For Each Biome Segment
        for (int x = 0; x <= inp_map.width; x++)
        {
            for (int y = 0; y <= inp_map.height; y++)
            {
                // Get minimum and maximum, bottom left and top right corner positions
                Vector2Int bot_left = new Vector2Int(Mathf.Max(x * biome_size - Mathf.FloorToInt(biome_size / 2), 0),
                                                     Mathf.Max(y * biome_size - Mathf.FloorToInt(biome_size / 2), 0));
                Vector2Int top_right = new Vector2Int(Mathf.Min(x * biome_size + Mathf.FloorToInt((biome_size - 1) / 2), inp_map.width * biome_size - 1),
                                                     Mathf.Min(y * biome_size + Mathf.FloorToInt((biome_size - 1) / 2), inp_map.height * biome_size - 1));

                int index = x + y * inp_map.width;
                if (x == inp_map.width)
                    index -= 1;
                if (y == inp_map.height)
                    index -= inp_map.width;
                // Transition Corner
                if (corners.Contains(new Vector2Int(x, y)))
                {
                    // Get Biomes of Each Corner
                    Biome tl_biome = Biome.None; Biome tr_biome = Biome.None;
                    Biome bl_biome = Biome.None; Biome br_biome = Biome.None;
                    bool lock_x = true; bool lock_y = true;
                    if (x > 0 && y > 0)
                    {
                        lock_x = false;
                        lock_y = false;
                        bl_biome = inp_map.map[index - 1 - inp_map.width];
                    }
                    if (x > 0 && y <= inp_map.height)
                    {
                        lock_x = false;
                        tl_biome = inp_map.map[index - 1];
                    }
                    if (x <= inp_map.width && y > 0)
                    {
                        lock_y = false;
                        br_biome = inp_map.map[index - inp_map.width];
                    }
                    tr_biome = inp_map.map[index];

                    Biome[,] transition_biome = GenerateTransition(tl_biome, tr_biome, bl_biome, br_biome, lock_x, lock_y);
                    for (int t_x = bot_left.x; t_x <= top_right.x; t_x++)
                        for (int t_y = bot_left.y; t_y <= top_right.y; t_y++)
                            map[t_x, t_y] = transition_biome[
                                (int)(t_x - (x - 0.5f) * biome_size), 
                                (int)(t_y - (y - 0.5f) * biome_size)];
                }
                // Non-Transition Corner
                else
                {
                    for (int c_x = bot_left.x; c_x <= top_right.x; c_x++)
                        for (int c_y = bot_left.y; c_y <= top_right.y; c_y++)
                            map[c_x, c_y] = inp_map.map[index];
                }
            }
        }
        return map;
    }

    private Biome[,] GenerateTransition(Biome tlc, Biome trc, Biome blc, Biome brc, bool lock_x, bool lock_y)
    {
        Biome[,] biomes = new Biome[biome_size, biome_size];
        for (int x = 0; x < biome_size; x++)
        {
            // Get x position relative to 'centre'
            float delta_x = x - (biome_size-1) * 0.5f;
            // Convert to percentage location
            float delta_x_perc = (Mathf.Abs(delta_x) / ((biome_size-1) * 0.5f)) / (transition_percentage * 0.01f);
            for (int y = 0; y < biome_size; y++)
            {
                // Get y position relative to 'centre'
                float delta_y = y - (biome_size-1) * 0.5f;
                // Convert to percentage location
                float delta_y_perc = (Mathf.Abs(delta_y) / ((biome_size-1) * 0.5f)) / (transition_percentage * 0.01f);

                // Randomize flip chance to produce transition effect
                bool flip_x = false; bool flip_y = false;
                if (delta_x_perc <= 1  && delta_y_perc <= 1)
                {
                    float rand = Random.Range(0, 100) * 0.01f;
                    flip_x = (rand > (0.5f + delta_x_perc * 0.5f) && !lock_x);
                    rand = Random.Range(0, 100) * 0.01f;
                    flip_y = rand > (0.5f + delta_y_perc * 0.5f) && !lock_y;
                }

                // Set biome based on flip checks
                if (delta_x < 0 && delta_y < 0)
                    biomes[x, y] = flip_x ? (flip_y ? trc : brc) : (flip_y ? tlc : blc);
                else if (delta_y < 0)
                    biomes[x, y] = flip_x ? (flip_y ? tlc : blc) : (flip_y ? trc : brc);
                else if (delta_x < 0)
                    biomes[x, y] = flip_x ? (flip_y ? brc : trc) : (flip_y ? blc : tlc);
                else
                    biomes[x, y] = flip_x ? (flip_y ? blc : tlc) : (flip_y ? brc : trc);
            }
        }
        return biomes;
    }

    /// <summary>
    /// Calculate all corners in a biome map that will have a transition
    /// </summary>
    /// <param name="inp_map">MapInfo of biome map information</param>
    /// <returns>Hashset<Vector2Int> of all corner positions</returns>
    private HashSet<Vector2Int> GetBiomeCorners(MapInfo inp_map)
    {
        HashSet<Vector2Int> corners = new HashSet<Vector2Int>();
        for (int x = 0; x < inp_map.width; x++)
        {
            for (int y = 0; y < inp_map.height; y++)
            {
                bool check_up = y < inp_map.height - 1;
                bool check_right = x < inp_map.width - 1;
                int cur_index = x + y * inp_map.width;
                if (check_up)
                {
                    // If current biome is different to above biome
                    if (inp_map.map[cur_index] != inp_map.map[cur_index + inp_map.width])
                    {
                        // Add both above corners to hash
                        corners.Add(new Vector2Int(x, y + 1));
                        corners.Add(new Vector2Int(x + 1, y + 1));
                    }
                }
                if (check_right)
                {
                    // If Current biome is different to right biome
                    if (inp_map.map[cur_index] != inp_map.map[cur_index + 1])
                    {
                        // Add both right corners to hash
                        corners.Add(new Vector2Int(x + 1, y));
                        corners.Add(new Vector2Int(x + 1, y + 1));
                    }
                }
            }
        }
        return corners;
    }

    private TileInfo ConvertBiomesToMap(Biome[,] inp_map)
    {
        TileInfo tiles = new TileInfo();
        tiles.map = new TileID[inp_map.GetLength(0) * inp_map.GetLength(1)];
        tiles.width = inp_map.GetLength(0);
        tiles.height = inp_map.GetLength(1);

        ProceduralLayers layers = GenerateLayers(inp_map.GetLength(0), inp_map.GetLength(1));

        for (int x = 0; x < inp_map.GetLength(0); x++)
            for (int y = 0; y < inp_map.GetLength(1); y++)
                tiles.map[x + y * tiles.width] = BiomeToTile(inp_map[x, y], new Vector2Int(x,y), layers);

        return tiles;
    }

    private TileID BiomeToTile(Biome biome, Vector2Int pos, ProceduralLayers layers)
    {
        switch (biome)
        {
            case Biome.None:        return TileID.None;
            case Biome.Standard:    return StandardGeneration(pos, layers);
            case Biome.Frozen:      return FrozenGeneration(pos, layers);
            case Biome.Desert:      return DesertGeneration(pos, layers);
            case Biome.Swamp:       return SwampGeneration(pos, layers);
            case Biome.Rocky:       return RockyGeneration(pos, layers);
            case Biome.SharpRocky:  return SharpRockyGeneration(pos, layers);
            case Biome.Lava:        return LavaGeneration(pos, layers);
            case Biome.Water:       return WaterGeneration(pos, layers);
            case Biome.Ocean:       return OceanGeneration(pos, layers);
            case Biome.Jungle:      return JungleGeneration(pos, layers);
            case Biome.Radioactive: return RadioactiveGeneration(pos, layers);
            case Biome.Lushious:    return LushiousGeneration(pos, layers);
        }
        return TileID.None;
    }

    #region Biome Generation
    private TileID StandardGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Gravel;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.LowValOre;
            else
                return Random.Range(0, 100) > 80 ? TileID.Stone : TileID.Dirt;
        }
    }

    private TileID FrozenGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // In Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (pos.y + 1 >= (int)layers.layer_cave.GetLength(1))
                return TileID.None;
            else if (!layers.layer_cave[pos.x, pos.y + 1] && Random.Range(0,100) > 70)
                return TileID.Icicles;
            else
                return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Snow;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.HardIce;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.ColdWater;
            else
                return TileID.Ice;
        }
    }

    private TileID DesertGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Sand;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.HardSand;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.BurriedItem;
            else
                return TileID.Sand;
        }
    }

    private TileID SwampGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Mud;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Gravel;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Dirt;
        }
    }

    private TileID RockyGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Gravel;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.LowValOre;
            else
                return TileID.Stone;
        }
    }

    private TileID SharpRockyGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Gravel;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.LowValOre;
            else
                return TileID.Stone;
        }
    }

    private TileID LavaGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Magma;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Magma;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.Obsidian;
            else
                return TileID.Molten;
        }
    }

    private TileID WaterGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        return TileID.Water;
    }

    private TileID OceanGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        return TileID.Water;
    }

    private TileID JungleGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] + 1 || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x] + 1)
            return Random.Range(0,100) > 80 ? TileID.Bushes : TileID.None;
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Gravel;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Stone;
        }
    }

    private TileID RadioactiveGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Cobblestone;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.RadioactiveBlock;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.HighValOre;
            else
                return TileID.Stone;
        }
    }

    private TileID LushiousGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface or in cave
        if (pos.y > (int)layers.surface_height[pos.x] || layers.layer_cave[pos.x, pos.y])
            return TileID.None;
        // Surface
        else if (pos.y == (int)layers.surface_height[pos.x])
            return TileID.Grass;
        // Below Surface and out cave
        else
        {
            if (layers.layer_large_clump[pos.x, pos.y])
                return TileID.Dirt;
            else if (layers.layer_small_clump[pos.x, pos.y])
                return TileID.Water;
            else
                return TileID.Stone;
        }
    }
    #endregion

    private ProceduralLayers GenerateLayers(int size_x, int size_y)
    {
        ProceduralLayers layers = new ProceduralLayers();

        Vector2Int size = new Vector2Int(size_x,size_y);
        layers.surface_height       = Noise.Generate1DLevels(size_x, properties_surface);
        for (int i = 0; i < size_x; i++)
        {
            layers.surface_height[i] = ((max_perc_height-min_perc_height)*0.01f) * size_y * layers.surface_height[i] + (min_perc_height*0.01f) * size_y;
        }
        layers.layer_cave           = PCGUtilities.ThresholdPass(Noise.Generate2DLevels(size, properties_cave), cave_threshold);
        layers.layer_large_clump    = PCGUtilities.ThresholdPass(Noise.Generate2DLevels(size, properties_l_clump), l_clump_threshold);
        layers.layer_small_clump    = PCGUtilities.ThresholdPass(Noise.Generate2DLevels(size, properties_s_clump),s_clump_threshold);
        layers.layer_dots           = PCGUtilities.ThresholdPass(Noise.Generate2DLevels(size, properties_dots), dots_threshold);

        return layers;
    }
}
