using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Structs
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
public class WaveFunctionCollapse : MonoBehaviour
{
    // Tweaks
    [Header("Tweaking Variables")]
    public bool frame_by_frame;
    public bool loop_generating;
    // Trackers
    [Header("Trackers")]
    public MapInfo cur_output;
    private bool generating;

    #region WFC Generation
    /// <summary>
    /// Generate biome map with Wave Function Collapse
    /// </summary>
    /// <param name="inp_map">Biome[] of input biome map</param>
    /// <param name="in_size">Vector2Int of input map size</param>
    /// <param name="out_size">Vector2Int of output map size</param>
    /// <returns>IEnumerator of function</returns>
    public IEnumerator GenerateWFC(Biome[] inp_map, Vector2Int in_size, Vector2Int out_size)
    {
        WFCInput input = new WFCInput();
        input.ruleset = GenerateRuleset(inp_map, in_size);
        input.width = out_size.x;
        input.height = out_size.y;
        StartCoroutine(GenerateWFC(input));
        return null;
    }

    /// <summary>
    /// Generate biome map with Wave Function Collapse
    /// </summary>
    /// <param name="inp">WFCInput of WFC based variables</param>
    /// <returns>IEnumerator of function</returns>
    public IEnumerator GenerateWFC(WFCInput inp)
    {
        if (generating)
            yield break;
        generating = true;
        // Time Trackers
        float t = Time.realtimeSinceStartup;
        float t_pre, t_col = 0, t_ent = 0, t_upd = 0;
        // Initialise WFC Output
        MapInfo wfc = new MapInfo();
        wfc.map = new Biome[inp.width * inp.height];
        wfc.width = inp.width;
        wfc.height = inp.height;

        // Initialise cells with default weighting
        Cell[] cells = InitCells(inp);

        int max_passes = inp.width * inp.height;
        int pass = 0;
        // Loop until finished collapsing or reaching max passes
        while (!FinishCheck(cells) && pass < max_passes)
        {
            // Find Cell of lowest entropy
            t_pre = Time.realtimeSinceStartup;
            Cell low_e = LowestEntropy(cells);
            t_ent += Time.realtimeSinceStartup - t_pre;

            // Collapse Cell
            t_pre = Time.realtimeSinceStartup;
            low_e.Collapse(inp.ruleset);
            t_col += Time.realtimeSinceStartup - t_pre;

            // Update neighbouring cells
            t_pre = Time.realtimeSinceStartup;
            Vector2Int g_pos = new Vector2Int(low_e.index % inp.width, low_e.index / inp.width);
            Rules cur_rules = RulesOfBiome(inp.ruleset, low_e.biome);
            if (g_pos.x > 0)            cells[low_e.index - 1].Update(cur_rules.left_rule);
            if (g_pos.x < inp.width-1)  cells[low_e.index + 1].Update(cur_rules.right_rule);
            if (g_pos.y > 0)            cells[low_e.index - inp.width].Update(cur_rules.down_rule);
            if (g_pos.y < inp.height-1) cells[low_e.index + inp.width].Update(cur_rules.up_rule);
            t_upd += Time.realtimeSinceStartup - t_pre;

            pass++;

            if (frame_by_frame)
            {
                for (int i = 0; i < cells.Length; i++)
                    wfc.map[i] = cells[i].biome;
                cur_output = wfc;
                yield return new WaitForEndOfFrame();
            }
        }

        for(int i = 0; i < cells.Length; i++)
            wfc.map[i] = cells[i].biome;
        cur_output = wfc;

        if (!frame_by_frame)
        {
            Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to generate the WFC biome map.");
            Debug.Log("Updates took " + t_upd + " seconds. Collapses took " + t_col + " seconds. Entropy took " + t_ent + " seconds.");
        }
        if (loop_generating)
        {
            yield return new WaitForEndOfFrame();
            generating = false;
            StartCoroutine(GenerateWFC(inp));
        }
        else
        {
            generating = false;
        }
    }
    #endregion
    #region Calculations & Generations
    /// <summary>
    /// Generate a ruleset based on an inputted biome map
    /// </summary>
    /// <param name="inp_map">Biome[] map of biomes</param>
    /// <param name="size">Vector2Int of inputted map size</param>
    /// <returns></returns>
    public static Ruleset GenerateRuleset(Biome[] inp_map, Vector2Int size)
    {
        float t = Time.realtimeSinceStartup;
        // Make Ruleset based off inp_map
        Ruleset set = new Ruleset();
        set.rules = new List<Rules>();
        set.height = new List<HeightVariance>();
        set.length = new List<LengthVariance>();
        int biome_count = System.Enum.GetValues(typeof(Biome)).Length;
        for (int i = 0; i < biome_count; i++)
        {
            Rules _r = new Rules();
            // Biome Rule
            _r.biome = (Biome)i;
            _r.left_rule = new Rule(false);
            _r.right_rule = new Rule(false);
            _r.up_rule = new Rule(false);
            _r.down_rule = new Rule(false);
            // Height Variance
            HeightVariance _h = new HeightVariance();
            _h.biome = (Biome)i;
            _h.h_chance = new float[size.y];
            // Length Variance
            LengthVariance _l = new LengthVariance();
            _l.biome = (Biome)i;
            _l.l_chance = new float[size.x];
            // Go Through Each Cell And Calculate Both
            for(int y = 0; y < size.y; y ++)
            {
                int counter = 0;
                for (int x = 0; x < size.x; x++)
                    if (inp_map[x + y * size.x] == (Biome)i)
                    {
                        _l.l_chance[x]++;
                        counter++;
                        if (x > 0)
                            _r.left_rule.wl[(int)inp_map[(x - 1) + y * size.x]].impact += 1;
                        if (x < size.x - 1)
                            _r.right_rule.wl[(int)inp_map[(x + 1) + y * size.x]].impact += 1;
                        if (y > 0)
                            _r.down_rule.wl[(int)inp_map[x + (y - 1) * size.x]].impact += 1;
                        if (y < size.y - 1)
                            _r.up_rule.wl[(int)inp_map[x + (y + 1) * size.x]].impact += 1;
                    }
                _h.h_chance[size.y - (y+1)] = counter;
            }
            set.rules.Add(_r);
            set.height.Add(_h);
            set.length.Add(_l);
        }
        Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to generate the ruleset.");
        return set;
    }

    /// <summary>
    /// Initialise cell array with inp size
    /// </summary>
    /// <param name="inp">WFCInput for cell size</param>
    /// <returns>Cell[] of initialised cells</returns>
    public static Cell[] InitCells(WFCInput inp)
    {
        List<BiomeWeight> init_options = new List<BiomeWeight>();
        for (int i = 0; i < System.Enum.GetValues(typeof(Biome)).Length; i++)
            init_options.Add(new BiomeWeight((Biome)i, 1));
        Cell[] cells = new Cell[inp.width * inp.height];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new Cell();
            cells[i].options = init_options;
            cells[i].index = i;
            cells[i].perc_height = (i / inp.width) / (float)inp.height;
            cells[i].perc_length = (i % inp.width) / (float)inp.width;
        }
        return cells;
    }

    /// <summary>
    /// Checks if all cells have collapsed, a.k.a. finished
    /// </summary>
    /// <param name="cells">Cell[] of all cells to check</param>
    /// <returns>Bool if all cells collapsed</returns>
    private static bool FinishCheck(Cell[] cells)
    {
        foreach (Cell cell in cells)
            if (!cell.collapsed)
                return false;
        return true;
    }

    /// <summary>
    /// Calculate which cell has the lowest entropy
    /// </summary>
    /// <param name="cells">Cell[] of all cells to check</param>
    /// <returns>Cell of chosen lowest entropy</returns>
    private static Cell LowestEntropy(Cell[] cells)
    {
        List<int> indexes = new List<int>();
        int lowest_entropy = int.MaxValue;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].collapsed)
                continue;
            int entropy = cells[i].options.Count;
            if (entropy < lowest_entropy)
            {
                lowest_entropy = entropy;
                indexes.Clear();
                indexes.Add(i);
            }
            else if (entropy == lowest_entropy)
            {
                indexes.Add(i);
            }
        }
        int randomIndex = indexes[Random.Range(0, indexes.Count)];
        return cells[randomIndex];
    }
    #endregion
    #region Value Fetching
    /// <summary>
    /// Return the rules of a specific biome from a ruleset
    /// </summary>
    /// <param name="set">Ruleset of all rules</param>
    /// <param name="biome">Biome of requested biome</param>
    /// <returns>Rules of requested biome</returns>
    private static Rules RulesOfBiome(Ruleset set, Biome biome)
    {
        foreach(Rules rules in set.rules)
            if (rules.biome == biome)
                return rules;
        return set.rules[0];
    }

    /// <summary>
    /// Return weights of biome at a height (Y POS)
    /// </summary>
    /// <param name="set">Ruleset of all biome rules</param>
    /// <param name="biome">Biome of biome</param>
    /// <param name="perc_h">Float of percentage height of cell</param>
    /// <returns></returns>
    public static float WeightAtHeight(Ruleset set, Biome biome, float perc_h)
    {
        for(int i = 0; i < set.height.Count; i ++)
            if (set.height[i].biome == biome)
            {
                int index = Mathf.FloorToInt((1-perc_h) * set.height[i].h_chance.Length);
                index = Mathf.Clamp(index, 0, set.height[i].h_chance.Length - 1);
                return set.height[i].h_chance[index];
            }
        return 0;
    }

    /// <summary>
    /// Return weights of biome at a length (X POS)
    /// </summary>
    /// <param name="set">Ruleset of all biome rules</param>
    /// <param name="biome">Biome of biome</param>
    /// <param name="perc_l">Float of percentage length of cell</param>
    /// <returns></returns>
    public static float WeightAtLength(Ruleset set, Biome biome, float perc_l)
    {
        for(int i = 0; i < set.length.Count; i ++)
            if (set.height[i].biome == biome)
            {
                int index = Mathf.FloorToInt(perc_l * set.length[i].l_chance.Length);
                index = Mathf.Clamp(index, 0, set.length[i].l_chance.Length - 1);
                return set.length[i].l_chance[index];
            }
        return 0;
    }
    #endregion
}