using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapse : MonoBehaviour
{
    // Tweaks
    [Header("Tweaking Variables")]
    public bool frame_by_frame;
    public int frames_per_update;
    public bool loop_generating;
    public bool output_stats;
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
        input.ruleset = GenerateRuleset(inp_map, in_size, output_stats);
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
        List<Cell> lowest_entropy = new List<Cell>();
        // Loop until finished collapsing or reaching max passes
        while (!FinishCheck(cells) && pass < max_passes)
        {
            // Find Cell of lowest entropy
            t_pre = Time.realtimeSinceStartup;
            Cell low_e = LowestEntropy(cells, lowest_entropy);
            t_ent += Time.realtimeSinceStartup - t_pre;

            // Collapse Cell
            t_pre = Time.realtimeSinceStartup;
            low_e.Collapse(inp.ruleset);
            lowest_entropy.Remove(low_e);
            t_col += Time.realtimeSinceStartup - t_pre;

            // Update neighbouring cells
            t_pre = Time.realtimeSinceStartup;
            UpdateNeighbouringCells(cells, low_e, inp, lowest_entropy);
            t_upd += Time.realtimeSinceStartup - t_pre;

            pass++;

            // Update current output if going frame by frame
            if (frame_by_frame)
            {
                for (int i = 0; i < cells.Length; i++)
                    wfc.map[i] = cells[i].biome;
                cur_output = wfc;
                if (frames_per_update > 0)
                    if (pass % frames_per_update == 0)
                        yield return new WaitForEndOfFrame();
            }
        }

        // Final update for current output
        for (int i = 0; i < cells.Length; i++)
            wfc.map[i] = cells[i].biome;
        cur_output = wfc;

        if (!frame_by_frame && output_stats)
            Debug.Log(
                "It took " + (Time.realtimeSinceStartup - t) + " seconds to generate the WFC biome map.\n" +
                "Updates took " + t_upd + " seconds. Collapses took " + t_col + " seconds. Entropy took " + t_ent + " seconds.");

        // Repeat if looping enabled
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
    /// Update cells surrounding a selected cell based on current lowest entropy and WFC input variables
    /// </summary>
    /// <param name="cells">Cell[] of all cells</param>
    /// <param name="low_e">Cell of selected cell</param>
    /// <param name="inp">WFCInput of dimensions of cell map</param>
    /// <param name="lowest_entropy">List<Cell> of current lowest entropy cells</param>
    private static void UpdateNeighbouringCells(Cell[] cells, Cell low_e, WFCInput inp, List<Cell> lowest_entropy)
    {
        Vector2Int g_pos = new Vector2Int(low_e.index % inp.width, low_e.index / inp.width);
        Rules cur_rules = RulesOfBiome(inp.ruleset, low_e.biome);
        
        if (g_pos.x > 0)                // Left Cell
            UpdateCell(cells, low_e, inp, lowest_entropy, cur_rules, Direction.Left);        
        if (g_pos.x < inp.width - 1)    // Right Cell
            UpdateCell(cells, low_e, inp, lowest_entropy, cur_rules, Direction.Right);        
        if (g_pos.y > 0)                // Below Cell
            UpdateCell(cells, low_e, inp, lowest_entropy, cur_rules, Direction.Down);        
        if (g_pos.y < inp.height - 1)   // Above Cell
            UpdateCell(cells, low_e, inp, lowest_entropy, cur_rules, Direction.Up);
    }

    /// <summary>
    /// Update a cell 1 space away from another cell in a specific direction with new rules 
    /// </summary>
    /// <param name="cells">Cell[] of all cells</param>
    /// <param name="low_e">Cell of selected cell</param>
    /// <param name="inp">WFCInput of dimensions of cell map</param>
    /// <param name="lowest_entropy">List<Cell> of current lowest entropy cells</param>
    /// <param name="cur_rules">Rules of rules to dictate new cells traits</param>
    /// <param name="dir">Direction to check from selected cell</param>
    private static void UpdateCell(Cell[] cells, Cell low_e, WFCInput inp, List<Cell> lowest_entropy, Rules cur_rules, Direction dir)
    {
        // Get index based on direction
        int index = low_e.index;
        switch (dir)
        {
            case Direction.Up: index += inp.width; break;
            case Direction.Down: index -= inp.width; break;
            case Direction.Left: index -= 1; break;
            case Direction.Right: index += 1; break;
        }
        if (cells[index].collapsed)
            return;
        // Update cell with rules of that direction
        switch (dir)
        {
            case Direction.Up: cells[index].Update(cur_rules.up_rule); break;
            case Direction.Down: cells[index].Update(cur_rules.down_rule); break;
            case Direction.Left: cells[index].Update(cur_rules.left_rule); break;
            case Direction.Right: cells[index].Update(cur_rules.right_rule); break;
        }
        // Update lowest entropy list
        int ent = cells[index].options.Count;
        if (lowest_entropy.Count == 0 || ent == lowest_entropy[0].options.Count)
        {
            if (!lowest_entropy.Contains(cells[index]))
                lowest_entropy.Add(cells[index]);
        }
        else if (ent < lowest_entropy[0].options.Count)
        {
            lowest_entropy.Clear();
            lowest_entropy.Add(cells[index]);
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
    public static Ruleset GenerateRuleset(Biome[] inp_map, Vector2Int size, bool output_stats)
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
        if (output_stats)
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

    private static Cell LowestEntropy(Cell[] cells, List<Cell> lowest_ent)
    {
        if (lowest_ent.Count == 0)
        {
            int selected_cell;
            do
                selected_cell = Random.Range(0, cells.Length);
            while (cells[selected_cell].collapsed);
            return cells[selected_cell];
        }
        else
        {
            return lowest_ent[Random.Range(0, lowest_ent.Count)];
        }
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

    public bool Looping()
    {
        return generating && (frame_by_frame || loop_generating);
    }
    #endregion
}