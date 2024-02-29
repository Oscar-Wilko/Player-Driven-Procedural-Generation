using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class MapGenerator : MonoBehaviour
{
    [Header("References")]
    public NoiseField surface_field;
    public NoiseField cave_field;
    public NoiseField large_clump_field;
    public NoiseField small_clump_field;
    public NoiseField dots_field;
    public ValueEditor min_height_editor;
    public ValueEditor max_height_editor;

    [Header("Tweaking Values")]
    public int biome_size;
    public float transition_percentage;
    public bool frame_by_frame;
    public bool loop_generating;
    public bool output_stats;

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

    public TunnelVariables tunnelSurface;
    public TunnelVariables tunnelVertical;
    public TunnelVariables tunnelHorizontal;
    public TunnelVariables tunnelFlat;

    [Header("Tracking Values")]
    [HideInInspector] public TileInfo cur_output = new TileInfo(0,0);
    private bool generating = false;

    private ProgressBar progress;

    private void Awake()
    {
        progress = FindObjectOfType<ProgressBar>();
    }

    private void Start()
    {
        InitializeFields();
    }

    public IEnumerator GenerateMap(MapInfo info)
    {
        if (generating)
            yield break;

        progress.SetProgressAmount(0.0f);

        // Declare Variables
        float t = Time.realtimeSinceStartup;
        float t_pre; float t_exp = 0; float t_con = 0; float t_lay = 0;
        generating = true;
        TileInfo tiles = new TileInfo(0,0);

        // Expand biome array with transitions
        t_pre = Time.realtimeSinceStartup;
        Biome[,] b_map = ExpandMap(info);
        Biome[] biome_map_array = new Biome[b_map.GetLength(0) * b_map.GetLength(1)];
        for (int x = 0; x < b_map.GetLength(0); x ++)
            for (int y = 0; y < b_map.GetLength(1); y ++)
                biome_map_array[x + y * b_map.GetLength(0)] = b_map[x, y];
        tiles.biome_map = biome_map_array;
        t_exp = Time.realtimeSinceStartup - t_pre;

        progress.SetProgressAmount(0.2f);
        yield return new WaitForEndOfFrame();

        // Convert biome array to map
        t_lay = Time.realtimeSinceStartup;
        tiles.map = new TileID[b_map.GetLength(0) * b_map.GetLength(1)];
        tiles.width = b_map.GetLength(0);
        tiles.height = b_map.GetLength(1);

        ProceduralLayers layers = new ProceduralLayers();

        Vector2Int size = new Vector2Int(b_map.GetLength(0), b_map.GetLength(1));

        // Layer Generation
        layers.surface_height = Noise.Generate1DLevels(size.x, properties_surface);
        for (int i = 0; i < size.x; i++)
            layers.surface_height[i] = ((max_perc_height - min_perc_height) * 0.01f) * size.y 
                * layers.surface_height[i] + (min_perc_height * 0.01f) * size.y;

        float[,] caveWeight = Noise.Generate2DLevels(size, properties_cave);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y)-16), 16, true, false, false, false, true);
        caveWeight = TunnelPass(caveWeight, layers.surface_height);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y)), 16, false, true, true, true, true);
        layers.layer_cave = PCGUtilities.ThresholdPass(caveWeight, properties_cave.threshold);

        progress.SetProgressAmount(0.3f);
        yield return new WaitForEndOfFrame();

        float[,] largeClumpWeight = Noise.Generate2DLevels(size, properties_l_clump);
        layers.layer_large_clump = PCGUtilities.ThresholdPass(largeClumpWeight, properties_l_clump.threshold);

        progress.SetProgressAmount(0.4f);
        yield return new WaitForEndOfFrame();

        float[,] smallClumpWeight = Noise.Generate2DLevels(size, properties_s_clump);
        layers.layer_small_clump = PCGUtilities.ThresholdPass(smallClumpWeight, properties_s_clump.threshold);

        progress.SetProgressAmount(0.5f);
        yield return new WaitForEndOfFrame();

        float[,] dotsWeight = Noise.Generate2DLevels(size, properties_dots);
        layers.layer_dots = PCGUtilities.ThresholdPass(dotsWeight, properties_dots.threshold);

        progress.SetProgressAmount(0.6f);
        yield return new WaitForEndOfFrame();

        t_lay = Time.realtimeSinceStartup - t_lay;
        t_pre = Time.realtimeSinceStartup;
        for (int x = 0; x < b_map.GetLength(0); x++)
        {
            if (x % 64 == 0)
            {
                progress.SetProgressAmount(0.6f + (float)x / b_map.GetLength(0) * 0.4f);
                if (frame_by_frame)
                    cur_output = tiles;
                yield return new WaitForEndOfFrame();
            }
            for (int y = 0; y < b_map.GetLength(1); y++)
            {
                tiles.map[x + y * tiles.width] = BiomeToTile(b_map[x, y], new Vector2Int(x, y), layers);
            }
        }
        t_con = Time.realtimeSinceStartup - t_pre;
        progress.SetVisible(false);
        cur_output = tiles;
        ScrambleSeeds();
        // Repeat if looping enabled
        if (!frame_by_frame && output_stats)
            Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to generate map from biome map.\n" +
            "Expanding map took " + t_exp + " seconds. Making layers took " + t_lay + " seconds. Converting map took " + t_con + " seconds.");

        if (loop_generating)
        {
            yield return new WaitForEndOfFrame();
            generating = false;
            StartCoroutine(GenerateMap(info));
        }
        else
        {
            generating = false;
        }
        yield break;
    }

    private float[,] TunnelPass(float[,] input, float[] surface)
    {
        float[,] map = input;
        System.Random rng = new System.Random(tunnelSurface.seed);
        // SURFACE TUNNELS
        int tunnelCount = rng.Next(Mathf.Min(tunnelSurface.minTunnels, tunnelSurface.maxTunnels), 
            Mathf.Max(tunnelSurface.minTunnels, tunnelSurface.maxTunnels));
        for (int i = 0; i < tunnelCount; i++)
        {
            List<Vector2Int> tunnel = Tunnel.GenerateTunnel(tunnelSurface, Direction.Down);
            int randX = rng.Next(Mathf.Min(16, input.GetLength(0)), Mathf.Max(0, input.GetLength(0)-16));
            Vector2Int point = new Vector2Int(randX, (int)surface[randX]);
            foreach(Vector2Int pos in tunnel)
            {
                Vector2Int target = pos + point;
                if (target.x < 0 || target.x >= input.GetLength(0)) continue;
                if (target.y < 0 || target.y >= input.GetLength(1)) continue;
                input[target.x, target.y] *= 0.25f;
            }
            tunnelSurface.seed = Mathf.Abs((int)(tunnelSurface.seed * 5.213f) % 10000000);
        }
        // UNDERGROUND VERTICAL
        tunnelCount = rng.Next(Mathf.Min(tunnelVertical.minTunnels, tunnelVertical.maxTunnels),
            Mathf.Max(tunnelVertical.minTunnels, tunnelVertical.maxTunnels));
        for (int i = 0; i < tunnelCount; i++)
        {
            List<Vector2Int> tunnel = Tunnel.GenerateTunnel(tunnelVertical, Direction.Down);
            int randX = rng.Next(Mathf.Min(16, input.GetLength(0)), Mathf.Max(0, input.GetLength(0) - 16));
            int randY = rng.Next(Mathf.Min(16, input.GetLength(1)), Mathf.Max(0, (int)surface[randX] - 16));
            Vector2Int point = new Vector2Int(randX, randY);
            foreach (Vector2Int pos in tunnel)
            {
                Vector2Int target = pos + point;
                if (target.x < 0 || target.x >= input.GetLength(0)) continue;
                if (target.y < 0 || target.y >= input.GetLength(1)) continue;
                input[target.x, target.y] *= 0.25f;
            }
            tunnelVertical.seed = Mathf.Abs((int)(tunnelVertical.seed * 5.213f) % 10000000);
        }

        // UNDERGROUND HORIZONTAL
        tunnelCount = rng.Next(Mathf.Min(tunnelHorizontal.minTunnels, tunnelHorizontal.maxTunnels),
            Mathf.Max(tunnelHorizontal.minTunnels, tunnelHorizontal.maxTunnels));
        for (int i = 0; i < tunnelCount; i++)
        {
            List<Vector2Int> tunnel = Tunnel.GenerateTunnel(tunnelHorizontal, rng.Next(0,2) == 1 ? Direction.Right : Direction.Left);
            int randX = rng.Next(Mathf.Min(16, input.GetLength(0)), Mathf.Max(0, input.GetLength(0) - 16));
            int randY = rng.Next(Mathf.Min(16, input.GetLength(1)), Mathf.Max(0, (int)surface[randX] - 16));
            Vector2Int point = new Vector2Int(randX, randY);
            foreach (Vector2Int pos in tunnel)
            {
                Vector2Int target = pos + point;
                if (target.x < 0 || target.x >= input.GetLength(0)) continue;
                if (target.y < 0 || target.y >= input.GetLength(1)) continue;
                input[target.x, target.y] *= 0.25f;
            }
            tunnelHorizontal.seed = Mathf.Abs((int)(tunnelHorizontal.seed * 5.213f) % 10000000);
        }

        // UNDERGROUND FLAT
        tunnelCount = rng.Next(Mathf.Min(tunnelFlat.minTunnels, tunnelFlat.maxTunnels),
            Mathf.Max(tunnelFlat.minTunnels, tunnelFlat.maxTunnels));
        for (int i = 0; i < tunnelCount; i++)
        {
            List<Vector2Int> tunnel = Tunnel.GenerateTunnel(tunnelFlat, rng.Next(0, 2) == 1 ? Direction.Right : Direction.Left);
            int randX = rng.Next(Mathf.Min(16, input.GetLength(0)), Mathf.Max(0, input.GetLength(0) - 16));
            int randY = rng.Next(Mathf.Min(16, input.GetLength(1)), Mathf.Max(0, (int)surface[randX] - 16));
            Vector2Int point = new Vector2Int(randX, randY);
            foreach (Vector2Int pos in tunnel)
            {
                Vector2Int target = pos + point;
                if (target.x < 0 || target.x >= input.GetLength(0)) continue;
                if (target.y < 0 || target.y >= input.GetLength(1)) continue;
                input[target.x, target.y] *= 0.25f;
            }
            tunnelFlat.seed = Mathf.Abs((int)(tunnelFlat.seed * 5.213f) % 10000000);
        }
        return map;
    }

    private void ScrambleSeeds()
    {
        properties_surface.seed = Mathf.Abs((int)(properties_surface.seed * 4.51f) % 10000000);
        properties_cave.seed = Mathf.Abs((int)(properties_cave.seed * 5.16f) % 10000000);
        properties_l_clump.seed = Mathf.Abs((int)(properties_l_clump.seed * 6.84f) % 10000000);
        properties_s_clump.seed = Mathf.Abs((int)(properties_s_clump.seed * 8.67f) % 10000000);
        properties_dots.seed = Mathf.Abs((int)(properties_dots.seed * 6.56f) % 10000000);
        surface_field.SetNewSeed(properties_surface.seed);
        cave_field.SetNewSeed(properties_cave.seed);
        large_clump_field.SetNewSeed(properties_l_clump.seed);
        small_clump_field.SetNewSeed(properties_s_clump.seed);
        dots_field.SetNewSeed(properties_dots.seed);
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
            case Biome.Luscious:    return LushiousGeneration(pos, layers);
        }
        return TileID.None;
    }

    #region Biome Generation
    private TileID StandardGeneration(Vector2Int pos, ProceduralLayers layers)
    {
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            if (pos.y + 1 >= (int)layers.layer_cave.GetLength(1))
                return TileID.Wall;
            else if (!layers.layer_cave[pos.x, pos.y + 1] && Random.Range(0,100) > 70)
                return TileID.Icicles;
            else
                return TileID.Wall;
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
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x,pos.y])
            return TileID.Wall;
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
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        if (pos.y > (int)layers.surface_height[pos.x] + 1)
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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
        // Above Surface
        if (pos.y > (int)layers.surface_height[pos.x])
            return TileID.None;
        // Cave
        else if (layers.layer_cave[pos.x, pos.y])
            return TileID.Wall;
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

    public bool Looping()
    {
        return generating && (frame_by_frame || loop_generating);
    }

    public bool ShowVisual() => !generating || frame_by_frame;

    public void UpdateBiomeSize(int new_size) => biome_size = new_size;
    public void UpdateNoiseValue(NoiseType type, NoiseVariable var, object value)
    {
        WaveVariables wave;
        switch (type)
        {
            case NoiseType.Cave01: wave = properties_cave; break;
            case NoiseType.Surface01: wave = properties_surface; break;
            case NoiseType.SmallClump01: wave = properties_s_clump; break;
            case NoiseType.LargeClump01: wave = properties_l_clump; break;
            case NoiseType.Dots01: wave = properties_dots; break;
            default: return;
        }
        switch (var)
        {
            case NoiseVariable.Seed: wave.seed = (int)value; break;
            case NoiseVariable.ScaleX: wave.scale.x = (float)value; break;
            case NoiseVariable.ScaleY: wave.scale.y = (float)value; break;
            case NoiseVariable.Octaves: wave.octaves = (int)value; break;
            case NoiseVariable.Persistance: wave.persistance = (float)value; break;
            case NoiseVariable.Lacunarity: wave.lacunarity = (float)value; break;
            case NoiseVariable.Threshold: wave.threshold = (float)value; break;
            default: return;
        }
        switch (type)
        {
            case NoiseType.Cave01: properties_cave = wave; break;
            case NoiseType.Surface01: properties_surface = wave; break;
            case NoiseType.SmallClump01: properties_s_clump = wave; break;
            case NoiseType.LargeClump01: properties_l_clump = wave; break;
            case NoiseType.Dots01: properties_dots = wave; break;
            default: return;
        }
    }

    public void UpdateSurfaceMinHeight(int perc) => min_perc_height = perc;
    public void UpdateSurfaceMaxHeight(int perc) => max_perc_height = perc;

    private void InitializeFields()
    {
        cave_field.Initialize(properties_cave);
        surface_field.Initialize(properties_surface);
        large_clump_field.Initialize(properties_l_clump);
        small_clump_field.Initialize(properties_s_clump);
        dots_field.Initialize(properties_dots);

        min_height_editor.UpdateIntValue(min_perc_height);
        max_height_editor.UpdateIntValue(max_perc_height);
    }
}
