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
    public Biome[] wl;
}

public class WaveFunctionCollapse : MonoBehaviour
{
    public class Cell
    {
        public bool collapsed = false;
        public Biome biome;
        public List<Biome> options;
        public int index;

        public Biome Collapse()
        {
            if (collapsed || options.Count <= 0)
                return biome;
            collapsed = true;
            biome = options[Random.Range(0, options.Count)];
            return biome;
        }

        public void Update(Rule rule)
        {
            if (collapsed || options.Count <= 0 || rule.wl_all)
                return;
            List<Biome> new_options = new List<Biome>();
            for (int i = options.Count - 1; i >= 0; i--)
                if (rule.wl.Contains(options[i]))
                    new_options.Add(options[i]);
            options = new_options;
        }
    }

    public WFCOutput cur_output;
    private bool generating;

    public IEnumerator GenerateWFC(Biome[] inp_map, Vector2Int size)
    {
        WFCInput input = new WFCInput();
        input.ruleset = GenerateRuleset(inp_map);
        input.width = size.x;
        input.height = size.y;
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
        generating = true;
        WFCOutput wfc = new WFCOutput();
        wfc.map = new Biome[inp.width * inp.height];
        wfc.width = inp.width;
        wfc.height = inp.height;

        List<Biome> init_options = new List<Biome>();
        for (int i = 0; i < System.Enum.GetValues(typeof(Biome)).Length; i++)
            init_options.Add((Biome)i);
        Cell[] cells = new Cell[inp.width * inp.height];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new Cell();
            cells[i].options = init_options;
            cells[i].index = i;
        }

        bool finished_generation = false;
        int max_passes = inp.width * inp.height;
        int pass = 0;
        while (!finished_generation && pass < max_passes)
        {
            // Find Cell of lowest entropy
            Cell low_e = LowestEntropy(cells);
            // Collapse Cell
            low_e.Collapse();
            // Update neighbouring cells
            Vector2Int g_pos = new Vector2Int(low_e.index % inp.width, low_e.index / inp.width);
            Rules cur_rules = RulesOfBiome(inp.ruleset, low_e.biome);
            if (g_pos.x > 0)            cells[low_e.index - 1].Update(cur_rules.left_rule);
            if (g_pos.x < inp.width-1)  cells[low_e.index + 1].Update(cur_rules.right_rule);
            if (g_pos.y < 0)            cells[low_e.index - inp.width].Update(cur_rules.up_rule);
            if (g_pos.y < inp.height-1) cells[low_e.index + inp.width].Update(cur_rules.down_rule);
            // Check finish condition
            finished_generation = FinishCheck(cells);

            pass++;
            for (int i = 0; i < cells.Length; i++)
                wfc.map[i] = cells[i].biome;
            cur_output = wfc;
            yield return new WaitForEndOfFrame();
        }

        for(int i = 0; i < cells.Length; i++)
            wfc.map[i] = cells[i].biome;
        cur_output = wfc;
        generating = false;
    }

    private bool FinishCheck(Cell[] cells)
    {
        foreach(Cell cell in cells)
            if (!cell.collapsed)
                return false;
        return true;
    }

    public Ruleset GenerateRuleset(Biome[] inp_map)
    {
        Ruleset set = new Ruleset();
        // Make Ruleset based off inp_map
        return set;
    }

    private Cell LowestEntropy(Cell[] cells)
    {
        List<Cell> joint_lowest = new List<Cell>();
        int lowest_entropy = int.MaxValue;
        foreach (Cell cell in cells)
        {
            if (cell.collapsed)
                continue;
            if (cell.options.Count < lowest_entropy)
            {
                joint_lowest.Clear();
                lowest_entropy = cell.options.Count;
                joint_lowest.Add(cell);
            }
            else if (cell.options.Count == lowest_entropy)
                joint_lowest.Add(cell);
        }
        return joint_lowest[Random.Range(0,joint_lowest.Count)];
    }

    private Rules RulesOfBiome(Ruleset set, Biome biome)
    {
        foreach(Rules rules in set.rules)
            if (rules.biome == biome)
                return rules;
        return set.rules[0];
    }
}
