using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public struct WFCOutput
{
    public Biome[] map;
    public int width;
    public int height;
}

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

public class WaveFunctionCollapse : MonoBehaviour
{
    public class Cell
    {
        public bool collapsed = false;
        public Biome biome;
        public List<BiomeWeight> options;
        public int index;
        public float perc_height;

        public Biome Collapse(Ruleset ruleset)
        {
            if (collapsed || options.Count <= 0)
                return biome;

            List<BiomeWeight> weights = new List<BiomeWeight>();
            float total_weight = 0;
            for (int i = 0; i < options.Count; i++)
            {
                BiomeWeight weight = options[i];
                weight.impact *= WeightAtHeight(ruleset, weight.biome, perc_height);
                total_weight += weight.impact;
                if (weight.impact != 0)
                    weights.Add(weight);
            }
            float rand_point = Random.Range(0, total_weight);
            int index = -1;
            foreach (BiomeWeight w in weights)
            {
                index++;
                if (w.impact < rand_point)
                    rand_point -= w.impact;
                else if (w.impact >= rand_point)
                    break;
            }

            index = Mathf.Clamp(index, 0, weights.Count-1);
            if (weights.Count != 0)
            {
                biome = weights[index].biome;
            }
            else
            {
                Debug.LogWarning("No Valid Biomes For Collapsing");
                biome = 0;
            }

            collapsed = true;
            return biome;
        }

        public void Update(Rule rule)
        {
            if (collapsed || options.Count <= 0 || rule.wl_all)
                return;
            List<BiomeWeight> new_options = new List<BiomeWeight>();
            foreach (BiomeWeight option in options)
                foreach (BiomeWeight w in rule.wl)
                    if (w.biome == option.biome)
                    {
                        BiomeWeight new_w = option;
                        new_w.impact *= w.impact;
                        new_options.Add(new_w);
                        break;
                    }
            options = new_options;
            if (options.Count == 0)
                Debug.LogError("No more options");
        }

        public float Entropy()
        {
            return options.Count;
        }
    }

    public bool frame_by_frame;
    public WFCOutput cur_output;
    private bool generating;
    public bool loop_generating;

    public IEnumerator GenerateWFC(Biome[] inp_map, Vector2Int in_size, Vector2Int out_size)
    {
        WFCInput input = new WFCInput();
        input.ruleset = GenerateRuleset(inp_map, in_size);
        input.width = out_size.x;
        input.height = out_size.y;
        StartCoroutine(GenerateWFC(input));
        return null;
    }
    
    public IEnumerator GenerateWFC(Ruleset ruleset, Vector2Int size)
    {
        WFCInput input = new WFCInput();
        input.ruleset = ruleset;
        input.width = size.x;
        input.height = size.y;
        StartCoroutine(GenerateWFC(input));
        return null;
    }

    public IEnumerator GenerateWFC(WFCInput inp)
    {
        if (generating)
            yield break;
        // Initialise WFC Ouput
        generating = true;
        float t = Time.realtimeSinceStartup;
        WFCOutput wfc = new WFCOutput();
        wfc.map = new Biome[inp.width * inp.height];
        wfc.width = inp.width;
        wfc.height = inp.height;

        // Initialise cells with default weighting
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
        }

        int max_passes = inp.width * inp.height;
        int pass = 0;
        while (!FinishCheck(cells) && pass < max_passes)
        {
            // Find Cell of lowest entropy
            Cell low_e = LowestEntropy(cells);

            // Collapse Cell
            low_e.Collapse(inp.ruleset);

            // Update neighbouring cells
            Vector2Int g_pos = new Vector2Int(low_e.index % inp.width, low_e.index / inp.width);
            Rules cur_rules = RulesOfBiome(inp.ruleset, low_e.biome);
            if (g_pos.x > 0)            cells[low_e.index - 1].Update(cur_rules.left_rule);
            if (g_pos.x < inp.width-1)  cells[low_e.index + 1].Update(cur_rules.right_rule);
            if (g_pos.y > 0)            cells[low_e.index - inp.width].Update(cur_rules.down_rule);
            if (g_pos.y < inp.height-1) cells[low_e.index + inp.width].Update(cur_rules.up_rule);

            pass++;
            for (int i = 0; i < cells.Length; i++)
                wfc.map[i] = cells[i].biome;
            cur_output = wfc;

            if (frame_by_frame)
                yield return new WaitForEndOfFrame();
        }

        for(int i = 0; i < cells.Length; i++)
            wfc.map[i] = cells[i].biome;
        cur_output = wfc;

        if (!frame_by_frame)
            Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to generate the WFC biome map.");
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

    /// <summary>
    /// Checks if all cells have collapsed, a.k.a. finished
    /// </summary>
    /// <param name="cells">Cell[] of all cells to check</param>
    /// <returns>Bool if all cells collapsed</returns>
    private static bool FinishCheck(Cell[] cells)
    {
        foreach(Cell cell in cells)
            if (!cell.collapsed)
                return false;
        return true;
    }

    public static Ruleset GenerateRuleset(Biome[] inp_map, Vector2Int size)
    {
        // Make Ruleset based off inp_map
        Ruleset set = new Ruleset();
        set.rules = new List<Rules>();
        set.height = new List<HeightVariance>();
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
            // Go Through Each Cell And Calculate Both
            for(int y = 0; y < size.y; y ++)
            {
                int counter = 0;
                for (int x = 0; x < size.x; x++)
                    if (inp_map[x + y * size.x] == (Biome)i)
                    {
                        counter++;
                        if (x > 0)
                        {
                            _r.left_rule.wl[(int)inp_map[(x - 1) + y * size.x]].impact += 1;
                        }
                        if (x < size.x - 1)
                        {
                            _r.right_rule.wl[(int)inp_map[(x + 1) + y * size.x]].impact += 1;
                        }
                        if (y > 0)
                        { 
                            _r.down_rule.wl[(int)inp_map[x + (y - 1) * size.x]].impact += 1;
                        }
                        if (y < size.y - 1)
                        {
                            _r.up_rule.wl[(int)inp_map[x + (y + 1) * size.x]].impact += 1;
                        }
                    }
                _h.h_chance[size.y - (y+1)] = counter;
            }
            set.rules.Add(_r);
            set.height.Add(_h);
        }
        return set;
    }

    private static int IndexOfBiomeweight(BiomeWeight[] w, Biome b)
    {
        for(int i = 0; i < w.Length; i ++)
            if (w[i].biome == b)
                return i;
        return 0;
    }

    /// <summary>
    /// Calculate which cell has the lowest entropy
    /// </summary>
    /// <param name="cells">Cell[] of all cells to check</param>
    /// <returns>Cell of chosen lowest entropy</returns>
    private static Cell LowestEntropy(Cell[] cells)
    {
        List<Cell> joint_lowest = new List<Cell>();
        float lowest_entropy = float.MaxValue;
        foreach (Cell cell in cells)
        {
            if (cell.collapsed)
                continue;
            if (cell.options.Count < lowest_entropy)
            {
                lowest_entropy = cell.Entropy();
                joint_lowest.Clear();
                joint_lowest.Add(cell);
            }
            else if (cell.options.Count == lowest_entropy)
                joint_lowest.Add(cell);
        }
        return joint_lowest[Random.Range(0,joint_lowest.Count)];
    }

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
}