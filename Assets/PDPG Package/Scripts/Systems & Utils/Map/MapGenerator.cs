using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("References")]
    public NoiseField surface_field;
    public NoiseField cave_field;
    public NoiseField large_clump_field;
    public NoiseField small_clump_field;
    public NoiseField dots_field;
    public NoiseField water_field;
    public TunnelField s_tunnel_field;
    public TunnelField v_tunnel_field;
    public TunnelField h_tunnel_field;
    public TunnelField f_tunnel_field;
    public ValueEditor structure_seed_editor;
    private ProgressBar progress;

    [Header("Tweaking Values")]
    public bool loop_generating;
    public bool output_stats;
    private int biome_size;
    private float transition_percentage;

    [Header("Procedural Factors")]
    public WaveVariables properties_surface;
    private int max_perc_height;
    private int min_perc_height;
    public WaveVariables properties_cave;
    public WaveVariables properties_l_clump;
    public WaveVariables properties_s_clump;
    public WaveVariables properties_dots;
    public WaveVariables properties_water;

    [Header("Tunnels")]
    public TunnelVariables tunnelSurface;
    public TunnelVariables tunnelVertical;
    public TunnelVariables tunnelHorizontal;
    public TunnelVariables tunnelFlat;

    [Header("Structures")]
    private int min_surf_treasure;
    private int max_surf_treasure;
    private int min_surf_depth;
    private int max_surf_depth;
    private int min_crystal_cluster;
    private int max_crystal_cluster;
    private int min_crystal_radius;
    private int max_crystal_radius;
    private int structure_seed;

    [Header("Tracking Values")]
    private TileInfo cur_output = new TileInfo(0,0);
    private Layers cur_layers = new Layers();
    private bool generating = false;

    private void Awake()
    {
        progress = FindObjectOfType<ProgressBar>();
    }

    private void Start()
    {
        InitializeFields();
    }

    /// <summary>
    /// Generate a map from a given MapInfo input, output is given to cur_ouput after coroutine
    /// </summary>
    /// <param name="info">MapInfo input information about biome map</param>
    /// <returns>IEnumerator of coroutine</returns>
    public IEnumerator GenerateMap(MapInfo info)
    {
        if (generating)
            yield break;
        generating = true;

        progress.SetProgressAmount(0.0f, "", "Starting Up");
        yield return new WaitForEndOfFrame();

        // Declare Variables
        float t = Time.realtimeSinceStartup;
        float t_pre; float t_exp = 0; float t_con = 0; float t_lay = 0; float t_wat = 0; float t_str = 0;
        TileInfo tiles = new TileInfo(0, 0);
        Layers map_layers = new Layers();

        // Expand biome array with transitions
        t_pre = Time.realtimeSinceStartup;
        Biome[,] b_map = ExpandMap(info);
        Biome[] biome_map_array = new Biome[b_map.GetLength(0) * b_map.GetLength(1)];
        for (int x = 0; x < b_map.GetLength(0); x++)
            for (int y = 0; y < b_map.GetLength(1); y++)
                biome_map_array[x + y * b_map.GetLength(0)] = b_map[x, y];
        tiles.biome_map = biome_map_array;
        t_exp = Time.realtimeSinceStartup - t_pre;

        progress.SetProgressAmount(0.1f, "Biome expanding", "Procedural Layers");
        yield return new WaitForEndOfFrame();

        // Convert biome array to map
        t_pre = Time.realtimeSinceStartup;
        tiles.map = new TileID[b_map.GetLength(0) * b_map.GetLength(1)];
        tiles.width = b_map.GetLength(0);
        tiles.height = b_map.GetLength(1);
        map_layers.width = b_map.GetLength(0);
        map_layers.height = b_map.GetLength(1);

        ProceduralLayers layers = new ProceduralLayers();
        Vector2Int size = new Vector2Int(b_map.GetLength(0), b_map.GetLength(1));

        // -----SURFACE LAYER-----
        layers.surface_height = Noise.Generate1DLevels(size.x, properties_surface);
        for (int i = 0; i < size.x; i++)
            layers.surface_height[i] = ((max_perc_height - min_perc_height) * 0.01f) * size.y
                * layers.surface_height[i] + (min_perc_height * 0.01f) * size.y;
        // -----------------------

        progress.SetProgressAmount(0.2f, "Surface Layer", "Cave Layer");
        yield return new WaitForEndOfFrame();

        // -----CAVE LAYER-----
        float[,] caveWeight = Noise.Generate2DLevels(size, properties_cave);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y) - 16), 16, true, false, false, false, true);
        map_layers.layer_cave_without = PCGUtilities.ConvertTo1D(PCGUtilities.ThresholdPass(caveWeight, properties_cave.threshold));
        caveWeight = TunnelPass(caveWeight, layers.surface_height);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y)), 16, false, true, true, true, true);
        layers.layer_cave = PCGUtilities.ThresholdPass(caveWeight, properties_cave.threshold);
        map_layers.layer_cave_with = PCGUtilities.ConvertTo1D(layers.layer_cave);
        // --------------------

        progress.SetProgressAmount(0.3f, "Cave Layer", "Large Clump Layer");
        yield return new WaitForEndOfFrame();

        // -----LARGE CLUMP LAYER-----
        float[,] largeClumpWeight = Noise.Generate2DLevels(size, properties_l_clump);
        layers.layer_large_clump = PCGUtilities.ThresholdPass(largeClumpWeight, properties_l_clump.threshold);
        map_layers.layer_large_clump = PCGUtilities.ConvertTo1D(layers.layer_large_clump);
        // ---------------------------

        progress.SetProgressAmount(0.4f, "Large Clump Layer", "Small Clump Layer");
        yield return new WaitForEndOfFrame();

        // -----SMALL CLUMP LAYER-----
        float[,] smallClumpWeight = Noise.Generate2DLevels(size, properties_s_clump);
        layers.layer_small_clump = PCGUtilities.ThresholdPass(smallClumpWeight, properties_s_clump.threshold);
        map_layers.layer_small_clump = PCGUtilities.ConvertTo1D(layers.layer_small_clump);
        // ---------------------------

        progress.SetProgressAmount(0.5f, "Small Clump Layer", "Dots Layer");
        yield return new WaitForEndOfFrame();

        // -----DOTS LAYER-----
        float[,] dotsWeight = Noise.Generate2DLevels(size, properties_dots);
        layers.layer_dots = PCGUtilities.ThresholdPass(dotsWeight, properties_dots.threshold);
        map_layers.layer_dots = PCGUtilities.ConvertTo1D(layers.layer_dots);
        // --------------------

        progress.SetProgressAmount(0.6f, "Dots Layer", "Water Layer");
        yield return new WaitForEndOfFrame();

        // -----WATER LAYER-----
        t_lay += Time.realtimeSinceStartup - t_pre;
        t_pre = Time.realtimeSinceStartup;
        float[,] waterWeight = Noise.Generate2DLevels(size, properties_water);
        map_layers.layer_water_before = PCGUtilities.ConvertTo1D(PCGUtilities.ThresholdPass(waterWeight, properties_water.threshold));
        layers.layer_water = SettleWater(PCGUtilities.ThresholdPass(waterWeight, properties_water.threshold),layers.layer_cave, layers.surface_height);
        map_layers.layer_water_after = PCGUtilities.ConvertTo1D(layers.layer_water);
        t_wat += Time.realtimeSinceStartup - t_pre;
        t_lay += Time.realtimeSinceStartup - t_pre;
        // --------------------

        progress.SetProgressAmount(0.7f, "Water Layer", "Converting Biomes To Tiles");
        yield return new WaitForEndOfFrame();

        t_pre = Time.realtimeSinceStartup;
        for (int x = 0; x < b_map.GetLength(0); x++)
        {
            // Gradually update progress bar
            if (x % 64 == 0)
            {
                progress.SetProgressAmount(0.7f + (float)x / b_map.GetLength(0) * 0.1f, $"Tile Conversion {x * 100/b_map.GetLength(0)}%", "Structure Generation");
                yield return new WaitForEndOfFrame();
            }
            for (int y = 0; y < b_map.GetLength(1); y++)
            {
                // Convert every tile to TileID based on layer input
                tiles.map[x + y * tiles.width] = BiomeToTile(b_map[x, y], new Vector2Int(x, y), layers);
            }
        }
        t_con = Time.realtimeSinceStartup - t_pre;
        progress.SetProgressAmount(0.8f, "Converting Biomes To Tiles", "Structure Generation");
        yield return new WaitForEndOfFrame();
        t_pre = Time.realtimeSinceStartup;

        // Structure Generation
        tiles.map = StructurePass(tiles.map, b_map, layers);

        t_str = Time.realtimeSinceStartup - t_pre;
        progress.SetProgressAmount(0.9f, "Structure Generation", "Finalising Processes");
        yield return new WaitForEndOfFrame();

        // Finalise process
        progress.SetVisible(false);
        cur_output = tiles;
        cur_layers = map_layers;
        generating = false;
        ScrambleSeeds();
        // Stat Output
        if (output_stats)
            Debug.Log($"It took {Time.realtimeSinceStartup - t} seconds to generate map from biome map.\n" +
                $"Expanding map took {t_exp} seconds. Making layers took {t_lay} seconds (Of which water was {t_wat} seconds). " +
                $"Converting map took {t_con} seconds. Structures took {t_str} seconds.");

        // Repeat if looping enabled
        if (loop_generating)
        {
            yield return new WaitForEndOfFrame();
            StartCoroutine(GenerateMap(info));
        }
        yield break;
    }

    /// <summary>
    /// Generate a map from a given MapInfo input [PURELY FOR A RESULT, DOES NOT INVOLVE ANY COROUTINE]
    /// </summary>
    /// <param name="info">MapInfo input information about biome map</param>
    /// <returns>TileInfo of generated map</returns>
    public TileInfo GenerateMapResult(MapInfo info)
    {
        // Declare Variables
        float t = Time.realtimeSinceStartup;
        float t_pre; float t_exp = 0; float t_con = 0; float t_lay = 0; float t_wat = 0; float t_str = 0;
        TileInfo tiles = new TileInfo(0, 0);

        // Expand biome array with transitions
        t_pre = Time.realtimeSinceStartup;
        Biome[,] b_map = ExpandMap(info);
        Biome[] biome_map_array = new Biome[b_map.GetLength(0) * b_map.GetLength(1)];
        for (int x = 0; x < b_map.GetLength(0); x++)
            for (int y = 0; y < b_map.GetLength(1); y++)
                biome_map_array[x + y * b_map.GetLength(0)] = b_map[x, y];
        tiles.biome_map = biome_map_array;
        t_exp = Time.realtimeSinceStartup - t_pre;

        // Convert biome array to map
        t_pre = Time.realtimeSinceStartup;
        tiles.map = new TileID[b_map.GetLength(0) * b_map.GetLength(1)];
        tiles.width = b_map.GetLength(0);
        tiles.height = b_map.GetLength(1);

        ProceduralLayers layers = new ProceduralLayers();
        Vector2Int size = new Vector2Int(b_map.GetLength(0), b_map.GetLength(1));

        // -----SURFACE LAYER-----
        layers.surface_height = Noise.Generate1DLevels(size.x, properties_surface);
        for (int i = 0; i < size.x; i++)
            layers.surface_height[i] = ((max_perc_height - min_perc_height) * 0.01f) * size.y
                * layers.surface_height[i] + (min_perc_height * 0.01f) * size.y;
        // -----CAVE LAYER-----
        float[,] caveWeight = Noise.Generate2DLevels(size, properties_cave);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y) - 16), 16, true, false, false, false, true);
        caveWeight = TunnelPass(caveWeight, layers.surface_height);
        caveWeight = PCGUtilities.FeatherLevels(caveWeight, new Vector2Int(0, 0),
            new Vector2Int(size.x, (int)(max_perc_height * 0.01f * size.y)), 16, false, true, true, true, true);
        layers.layer_cave = PCGUtilities.ThresholdPass(caveWeight, properties_cave.threshold);
        // -----LARGE CLUMP LAYER-----
        float[,] largeClumpWeight = Noise.Generate2DLevels(size, properties_l_clump);
        layers.layer_large_clump = PCGUtilities.ThresholdPass(largeClumpWeight, properties_l_clump.threshold);
        // -----SMALL CLUMP LAYER-----
        float[,] smallClumpWeight = Noise.Generate2DLevels(size, properties_s_clump);
        layers.layer_small_clump = PCGUtilities.ThresholdPass(smallClumpWeight, properties_s_clump.threshold);
        // -----DOTS LAYER-----
        float[,] dotsWeight = Noise.Generate2DLevels(size, properties_dots);
        layers.layer_dots = PCGUtilities.ThresholdPass(dotsWeight, properties_dots.threshold);
        // -----WATER LAYER-----
        t_lay += Time.realtimeSinceStartup - t_pre;
        t_pre = Time.realtimeSinceStartup;
        float[,] waterWeight = Noise.Generate2DLevels(size, properties_water);
        layers.layer_water = SettleWater(PCGUtilities.ThresholdPass(waterWeight, properties_water.threshold),
            layers.layer_cave, layers.surface_height);
        t_wat += Time.realtimeSinceStartup - t_pre;
        // --------------------

        t_lay += Time.realtimeSinceStartup - t_pre;
        t_pre = Time.realtimeSinceStartup;
        for (int x = 0; x < b_map.GetLength(0); x++)
        {
            for (int y = 0; y < b_map.GetLength(1); y++)
            {
                // Convert every tile to TileID based on layer input
                tiles.map[x + y * tiles.width] = BiomeToTile(b_map[x, y], new Vector2Int(x, y), layers);
            }
        }
        t_con = Time.realtimeSinceStartup - t_pre;
        t_pre = Time.realtimeSinceStartup;

        // Structure Generation
        tiles.map = StructurePass(tiles.map, b_map, layers);

        t_str = Time.realtimeSinceStartup - t_pre;
        ScrambleSeeds();
        if (output_stats)
            Debug.Log($"It took {Time.realtimeSinceStartup - t} seconds to generate map from biome map.\n" +
                $"Expanding map took {t_exp} seconds. Making layers took {t_lay} seconds (Of which water was {t_wat} seconds). " +
                $"Converting map took {t_con} seconds. Structures took {t_str} seconds.");

        return tiles;
    }

    private TileID[] StructurePass(TileID[] t_map, Biome[,] b_map, ProceduralLayers layers)
    {
        // ------- TRESURE --------
        System.Random rng = new System.Random(structure_seed);
        // Get Possible Generation Positions
        List<int> possiblePositions = new List<int>();
        Dictionary<int, int> xDepths = new Dictionary<int, int>();
        for (int x = 1; x < b_map.GetLength(0)-1; x++)
        {
            int depth = rng.Next(Mathf.Min(min_surf_depth,max_surf_depth),Mathf.Max(min_surf_depth,max_surf_depth));
            int surf = (int)layers.surface_height[x];
            if (surf - depth < 0)
                continue;
            // If any of the 5 tiles are cave tiles then skip
            if (layers.layer_cave[x, surf] ||
                layers.layer_cave[x, surf - depth] ||
                layers.layer_cave[x + 1, surf - depth] ||
                layers.layer_cave[x - 1, surf - depth] ||
                layers.layer_cave[x, surf - depth + 1] ||
                layers.layer_cave[x, surf - depth - 1])
                continue;
            possiblePositions.Add(x);
            xDepths.Add(x, depth);
        }
        possiblePositions = possiblePositions.OrderBy(x => rng.Next(0,100) * 0.01f).ToList();
        int maxCountTreasure = Mathf.Min(rng.Next(Mathf.Min(min_surf_treasure,max_surf_treasure),Mathf.Max(min_surf_treasure,max_surf_treasure)), possiblePositions.Count);
        // Generate Tresure
        for(int i = 0; i < maxCountTreasure; i ++) // MAKE RANDOM BY LIMITS
        {
            int surf = (int)layers.surface_height[possiblePositions[i]];
            int depth = xDepths[possiblePositions[i]];
            t_map[possiblePositions[i] + surf * b_map.GetLength(0)] = TileID.Mud;
            t_map[possiblePositions[i] + (surf - depth) * b_map.GetLength(0)] = TileID.BurriedItem;
            if (t_map[possiblePositions[i] + 1 + (surf - depth) * b_map.GetLength(0)] != TileID.BurriedItem)
                t_map[possiblePositions[i] + 1 + (surf - depth) * b_map.GetLength(0)] = TileID.Mud;
            if (t_map[possiblePositions[i] - 1 + (surf - depth) * b_map.GetLength(0)] != TileID.BurriedItem)
                t_map[possiblePositions[i] - 1 + (surf - depth) * b_map.GetLength(0)] = TileID.Mud;
            if (t_map[possiblePositions[i] + (surf - depth + 1) * b_map.GetLength(0)] != TileID.BurriedItem)
                t_map[possiblePositions[i] + (surf - depth + 1) * b_map.GetLength(0)] = TileID.Mud;
            if (t_map[possiblePositions[i] + (surf - depth - 1) * b_map.GetLength(0)] != TileID.BurriedItem)
                t_map[possiblePositions[i] + (surf - depth - 1) * b_map.GetLength(0)] = TileID.Mud;
        }

        // ------- CRYSTAL CLUSTER --------
        // Get Possible Generation Positions
        List<Vector2Int> possiblePositionsCrystal = new List<Vector2Int>();
        Dictionary<Vector2Int, int> clusterRadius = new Dictionary<Vector2Int, int>();
        int radius = rng.Next(Mathf.Min(min_crystal_radius, max_crystal_radius), Mathf.Max(min_crystal_radius, max_crystal_radius));
        for (int x = 0; x < (b_map.GetLength(0) - radius); x++)
        {
            for (int y = 0; y < (int)layers.surface_height[x] - radius; y++)
            {
                if (x < radius || y < radius)
                    continue;
                if (b_map[x, y] != Biome.Rocky && b_map[x, y] != Biome.SharpRocky)
                    continue;
                possiblePositionsCrystal.Add(new Vector2Int(x, y));
                clusterRadius.Add(new Vector2Int(x, y), radius);
                radius = rng.Next(Mathf.Min(min_crystal_radius, max_crystal_radius), Mathf.Max(min_crystal_radius, max_crystal_radius));
            }
        }
        int maxCountCrystal = Mathf.Min(rng.Next(Mathf.Min(min_crystal_cluster, max_crystal_cluster),
                Mathf.Max(min_crystal_cluster, max_crystal_cluster)),possiblePositionsCrystal.Count);
        // Generate Clusters
        for (int i = 0; i < maxCountCrystal; i++)
        {
            bool voronoiOrWorley = rng.Next(0,2) == 0; // False = Worley, True = Voronoi
            int index = rng.Next(0, possiblePositionsCrystal.Count);
            Vector2Int pos = possiblePositionsCrystal[index];
            possiblePositionsCrystal.RemoveAt(index);
            float[,] noisef = null;
            int[,] noisei = null;
            if (voronoiOrWorley)
                noisei = Noise.GenerateVoronoi(new Vector2Int(1 + clusterRadius[pos] * 2, 1 + clusterRadius[pos] * 2), 20, rng.Next(100000, 100000000));
            else
                noisef = Noise.GenerateWorley(new Vector2Int(1 + clusterRadius[pos] * 2, 1 + clusterRadius[pos] * 2), 20, rng.Next(100000, 100000000), 1, clusterRadius[pos]*0.5f);
            for (int x = pos.x - clusterRadius[pos]; x < pos.x + clusterRadius[pos]; x ++)
            {
                for (int y = pos.y - clusterRadius[pos]; y < pos.y + clusterRadius[pos]; y++)
                {
                    if ((Vector2Int.Distance(pos, new Vector2Int(x, y)) > clusterRadius[pos])
                        || layers.layer_cave[x,y])
                        continue;
                    if (voronoiOrWorley && noisei != null)
                    {
                        switch (noisei[x - pos.x + clusterRadius[pos], y - pos.y + clusterRadius[pos]] % 6)
                        {
                            case 0: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal01; break;
                            case 1: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal02; break;
                            case 2: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal03; break;
                            case 3: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal04; break;
                            case 4: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal05; break;
                            case 5: t_map[x + y * b_map.GetLength(0)] = TileID.Crystal06; break;
                        }
                    }
                    else if (!voronoiOrWorley && noisef != null)
                    {
                        float val = noisef[x - pos.x + clusterRadius[pos], y - pos.y + clusterRadius[pos]];
                        if (val < 1 / 6f)       t_map[x + y * b_map.GetLength(0)] = TileID.Crystal01;
                        else if (val < 2 / 6f)  t_map[x + y * b_map.GetLength(0)] = TileID.Crystal02;
                        else if (val < 3 / 6f)  t_map[x + y * b_map.GetLength(0)] = TileID.Crystal03;
                        else if (val < 4 / 6f)  t_map[x + y * b_map.GetLength(0)] = TileID.Crystal04;
                        else if (val < 5 / 6f)  t_map[x + y * b_map.GetLength(0)] = TileID.Crystal05;
                        else                    t_map[x + y * b_map.GetLength(0)] = TileID.Crystal06;
                    }
                }
            }
        }
        return t_map;
    }

    /// <summary>
    /// Carve tunnels into an input map based on tunnel values and surface layer
    /// </summary>
    /// <param name="input">2D float array input map</param>
    /// <param name="surface">1D float array of surface heights</param>
    /// <returns>2D float array of new map</returns>
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

    /// <summary>
    /// Update water map by settling water into cave layer
    /// </summary>
    /// <param name="water_map">2D bool array of water map</param>
    /// <param name="cave_layer">2D bool array of cave layer</param>
    /// <param name="surface_layer">1D float array of surface heights</param>
    /// <returns>2D bool array of new water map</returns>
    private static bool[,] SettleWater(bool[,] water_map, bool[,] cave_layer, float[] surface_layer)
    {
        Vector2Int size = new Vector2Int(water_map.GetLength(0), water_map.GetLength(1));
        bool[,] w_map = new bool[size.x, size.y];
        for (int y = 0; y < size.y; y++)
        {
            for (int x = size.x-1; x >= 0; x--)
            {
                // Go to next tile if either: not a water tile, not an open cave tile, not below surface level
                if (!water_map[x, y] || !cave_layer[x, y] || y > (int)surface_layer[x])
                    continue;
                bool moving = true; bool hitLeft = false;
                Vector2Int curShift = Vector2Int.zero;
                while (moving)
                {
                    moving = false;
                    // If out of bounds, go to next tile
                    if (y + curShift.y - 1 <= 0)
                        continue;
                    // If tile below currently checked tile is empty in both water and cave layer then shift down
                    if (!w_map[x + curShift.x, y + curShift.y - 1] && cave_layer[x + curShift.x, y + curShift.y - 1])
                    {
                        curShift.y -= 1;
                        hitLeft = false;
                        moving = true;
                    }
                    // If haven't hit a left tile yet
                    else if (!hitLeft)
                    {
                        // If tile to the left is a water tile or occupied cave tile then trigger hit left bool
                        if (w_map[x + curShift.x - 1, y + curShift.y] || !cave_layer[x + curShift.x - 1, y + curShift.y])
                        {
                            hitLeft = true;
                            moving = true;
                        }
                        // Otherwise move left and continue shifting
                        else
                        {
                            curShift.x -= 1;
                            moving = true;
                        }
                    }
                    // Diagonal left (covers some extra miss cases)
                    else if (!w_map[x + curShift.x - 1, y + curShift.y - 1] && cave_layer[x + curShift.x - 1, y + curShift.y - 1])
                    {
                        curShift.x -= 1;
                        curShift.y -= 1;
                        moving = true;
                    }
                    // Otherwise, shift to the right if there is no water or cave tile
                    else if (!w_map[x + curShift.x + 1, y + curShift.y] && cave_layer[x + curShift.x + 1, y + curShift.y])
                    {
                        curShift.x += 1;
                        moving = true;
                    }
                }
                w_map[x + curShift.x, y + curShift.y] = true;
            }
        }
        // Line checks to fill irregular water
        bool fillCheck = false;
        for (int y = 0; y < size.y; y++)
        {
            for (int x = size.x - 1; x >= 0; x--)
            {
                if (!cave_layer[x,y])
                {
                    fillCheck = false;
                }
                else if (w_map[x, y])
                {
                    fillCheck = true;
                }
                else if (fillCheck)
                {
                    w_map[x, y] = true;
                }
            }
        }
        return w_map;
    }

    /// <summary>
    /// Scramble all seeds of noise and tunnel variables
    /// </summary>
    private void ScrambleSeeds()
    {
        // Noise Fields
        properties_surface.seed = Mathf.Abs((int)(properties_surface.seed * 4.51f) % 10000000);
        properties_cave.seed = Mathf.Abs((int)(properties_cave.seed * 5.16f) % 10000000);
        properties_l_clump.seed = Mathf.Abs((int)(properties_l_clump.seed * 6.84f) % 10000000);
        properties_s_clump.seed = Mathf.Abs((int)(properties_s_clump.seed * 8.67f) % 10000000);
        properties_dots.seed = Mathf.Abs((int)(properties_dots.seed * 6.56f) % 10000000);
        properties_water.seed = Mathf.Abs((int)(properties_dots.seed * 6.12f) % 10000000);
        surface_field.SetNewSeed(properties_surface.seed);
        cave_field.SetNewSeed(properties_cave.seed);
        large_clump_field.SetNewSeed(properties_l_clump.seed);
        small_clump_field.SetNewSeed(properties_s_clump.seed);
        dots_field.SetNewSeed(properties_dots.seed);
        water_field.SetNewSeed(properties_water.seed);

        // Tunnel Fields
        tunnelSurface.seed = Mathf.Abs((int)(tunnelSurface.seed * 5.953f) % 10000000);
        tunnelVertical.seed = Mathf.Abs((int)(tunnelVertical.seed * 7.213f) % 10000000);
        tunnelHorizontal.seed = Mathf.Abs((int)(tunnelHorizontal.seed * 6.213f) % 10000000);
        tunnelFlat.seed = Mathf.Abs((int)(tunnelFlat.seed * 5.213f) % 10000000);
        s_tunnel_field.SetNewSeed(tunnelSurface.seed);
        v_tunnel_field.SetNewSeed(tunnelVertical.seed);
        h_tunnel_field.SetNewSeed(tunnelHorizontal.seed);
        f_tunnel_field.SetNewSeed(tunnelFlat.seed);

        // Other Fields
        structure_seed = Mathf.Abs((int)(structure_seed * 9.345f) % 10000000);
        structure_seed_editor.IntValueChanged.Invoke(structure_seed);
    }

    /// <summary>
    /// Expand an input biome map up to a larger size with transitions
    /// </summary>
    /// <param name="inp_map">MapInfo of inputted biome map</param>
    /// <returns>2D Biome array of output map</returns>
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

    #region Calculations & Conversions
    /// <summary>
    /// Generate biome array transitioning between 4 possible biome corners
    /// </summary>
    /// <param name="tlc">Biome of top left corner</param>
    /// <param name="trc">Biome of top right corner</param>
    /// <param name="blc">Biome of bottom left corner</param>
    /// <param name="brc">Biome of bottom right corner</param>
    /// <param name="lock_x">Bool if x flipping should be locked (e.g. side of map, don't want to flip a None biome)</param>
    /// <param name="lock_y">Bool if y flipping should be locked (e.g. bottom of map, don't want to flip a None biome)</param>
    /// <returns>2D Biome array of multiple biomes transitioning</returns>
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
    private static HashSet<Vector2Int> GetBiomeCorners(MapInfo inp_map)
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

    /// <summary>
    /// Convert a biome from a position into a TileID based on previous map layers
    /// </summary>
    /// <param name="biome">Biome of inputted biome</param>
    /// <param name="pos">Vector2Int of tile position</param>
    /// <param name="layers">ProceduralLayers of inputted generation layers</param>
    /// <returns>TileID of converted tile</returns>
    private TileID BiomeToTile(Biome biome, Vector2Int pos, ProceduralLayers layers)
    {
        switch (biome)
        {
            case Biome.None:        return TileID.None;
            case Biome.Standard:    return BGen.StandardGeneration(pos, layers);
            case Biome.Frozen:      return BGen.FrozenGeneration(pos, layers);
            case Biome.Desert:      return BGen.DesertGeneration(pos, layers);
            case Biome.Swamp:       return BGen.SwampGeneration(pos, layers);
            case Biome.Rocky:       return BGen.RockyGeneration(pos, layers);
            case Biome.SharpRocky:  return BGen.SharpRockyGeneration(pos, layers);
            case Biome.Lava:        return BGen.LavaGeneration(pos, layers);
            case Biome.Water:       return BGen.WaterGeneration(pos, layers);
            case Biome.Ocean:       return BGen.OceanGeneration(pos, layers);
            case Biome.Jungle:      return BGen.JungleGeneration(pos, layers);
            case Biome.Radioactive: return BGen.RadioactiveGeneration(pos, layers);
            case Biome.Luscious:    return BGen.LushiousGeneration(pos, layers);
        }
        return TileID.None;
    }
    #endregion
    #region Quick Functions
    public bool Looping() => loop_generating;
    public bool ShowVisual() => !generating;
    public TileInfo Output() => cur_output;
    public Layers GetLayers() => cur_layers;
    public void LoadTiles(TileInfo info) => cur_output = info;
    public void LoadLayers(Layers info) => cur_layers = info;
    #endregion
    #region UI
    public void UpdateBiomeSize(int new_size) => biome_size = new_size;
    public void UpdateTransitionPercentage(float new_perc) => transition_percentage = new_perc;

    /// <summary>
    /// Update internal noise values based on UI change
    /// </summary>
    /// <param name="type">NoiseType of what noise to change</param>
    /// <param name="var">NoiseVariable of what variable to change</param>
    /// <param name="value">Object of what value to change to</param>
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
            case NoiseType.Water01: wave = properties_water; break;
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
            case NoiseType.Water01: properties_water = wave; break;
            default: return;
        }
    }

    /// <summary>
    /// Update internal tunnel values based on UI change
    /// </summary>
    /// <param name="type">TunnelType of what tunnel to change</param>
    /// <param name="var">TunnelVariable of what variable to change</param>
    /// <param name="value">Object of what value to change to</param>
    public void UpdateTunnelValue(TunnelType type, TunnelVariable var, object value)
    {
        TunnelVariables wave;
        switch (type)
        {
            case TunnelType.Surface01: wave = tunnelSurface; break;
            case TunnelType.Vertical01: wave = tunnelVertical; break;
            case TunnelType.Horizontal01: wave = tunnelHorizontal; break;
            case TunnelType.Flat01: wave = tunnelFlat; break;
            default: return;
        }
        switch (var)
        {
            case TunnelVariable.Seed: wave.seed = (int)value; break;
            case TunnelVariable.MinTunnels: wave.minTunnels = (int)value; break;
            case TunnelVariable.MaxTunnels: wave.maxTunnels = (int)value; break;
            case TunnelVariable.MinVertexCount: wave.minVertexCount = (int)value; break;
            case TunnelVariable.MaxVertexCount: wave.maxVertexCount = (int)value; break;
            case TunnelVariable.MinVertexDist: wave.minVertexDist = (float)value; break;
            case TunnelVariable.MaxVertexDist: wave.maxVertexDist = (float)value; break;
            case TunnelVariable.MinRatio: wave.minRatio = (float)value; break;
            case TunnelVariable.MaxRatio: wave.maxRatio = (float)value; break;
            case TunnelVariable.Thickness: wave.thickness = (int)value; break;
            default: return;
        }
        switch (type)
        {
            case TunnelType.Surface01: tunnelSurface = wave; break;
            case TunnelType.Vertical01: tunnelVertical = wave; break;
            case TunnelType.Horizontal01: tunnelHorizontal = wave; break;
            case TunnelType.Flat01: tunnelFlat = wave; break;
            default: return;
        }
    }

    public void UpdateSurfaceMinHeight(int perc) => min_perc_height = perc;
    public void UpdateSurfaceMaxHeight(int perc) => max_perc_height = perc;

    /// <summary>
    /// Initialise all UI fields
    /// </summary>
    private void InitializeFields()
    {
        // Noise fields
        cave_field.Initialize(properties_cave);
        surface_field.Initialize(properties_surface);
        large_clump_field.Initialize(properties_l_clump);
        small_clump_field.Initialize(properties_s_clump);
        dots_field.Initialize(properties_dots);
        water_field.Initialize(properties_water);

        // Tunnel fields
        s_tunnel_field.Initialize(tunnelSurface);
        v_tunnel_field.Initialize(tunnelVertical);
        h_tunnel_field.Initialize(tunnelHorizontal);
        f_tunnel_field.Initialize(tunnelFlat);
}
    public void UpdateMinTreasure(int val) => min_surf_treasure = val;
    public void UpdateMaxTreasure(int val) => max_surf_treasure = val;
    public void UpdateMinTreasureDepth(int val) => min_surf_depth = val;
    public void UpdateMaxTreasureDepth(int val) => max_surf_depth = val;
    public void UpdateMinCrystals(int val) => min_crystal_cluster = val;
    public void UpdateMaxCrystals(int val) => max_crystal_cluster = val;
    public void UpdateMinCrystalsRadius(int val) => min_crystal_radius = val;
    public void UpdateMaxCrystalsRadius(int val) => max_crystal_radius= val;
    public void UpdateStructureSeed(int val) => structure_seed = val;
    #endregion
}
