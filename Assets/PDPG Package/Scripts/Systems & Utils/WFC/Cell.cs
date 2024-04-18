using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public Biome biomeInvalid;
    public bool collapsed = false;
    public Biome biome;
    public List<BiomeWeight> options;
    public int index;
    public float perc_height;
    public float perc_length;
    public float perc_diag_tr;
    public float perc_diag_br;
    private int maxCheckRange = 16;

    /// <summary>
    /// Collapse self by checking current options with ruleset
    /// </summary>
    /// <param name="ruleset">Ruleset dictating biomes interactions</param>
    /// <returns>Biome of selected biome to collapse to</returns>
    public Biome Collapse(WFCInput input)
    {
        if (collapsed || options.Count <= 0)
            return biome;
        // Generate biased weights based on height and length
        List<BiomeWeight> weights = new List<BiomeWeight>();
        float total_weight = 0;
        for (int i = 0; i < options.Count; i++)
        {
            BiomeWeight weight = options[i];
            weight.impact *= WaveFunctionCollapse.WeightAtVariance(input.ruleset.height, weight.biome, perc_height);
            weight.impact *= WaveFunctionCollapse.WeightAtVariance(input.ruleset.length, weight.biome, perc_length);
            weight.impact *= Mathf.Clamp(WaveFunctionCollapse.WeightAtVariance(input.ruleset.diagonal_br, weight.biome, perc_diag_br),0.05f,5f);
            weight.impact *= Mathf.Clamp(WaveFunctionCollapse.WeightAtVariance(input.ruleset.diagonal_tr, weight.biome, perc_diag_tr),0.05f,5f);
            weight.impact *= Mathf.Clamp(1 - (DistFromClosest(input, options[i].biome) / maxCheckRange), 0, 1);
            total_weight += weight.impact;
            if (weight.impact != 0)
                weights.Add(weight);
        }
        // Select biome randomly with weight
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

        index = Mathf.Clamp(index, 0, weights.Count - 1);
        biome = weights.Count == 0 ? biomeInvalid : weights[index].biome;
        collapsed = true;
        return biome;
    }

    private float DistFromClosest(WFCInput inp, Biome biome)
    {
        float closestRange = maxCheckRange;
        Vector2Int indexPos = new Vector2Int(
            (int)(inp.biome_map.width * (index % inp.width / (float)inp.width)), 
            (int)(inp.biome_map.height * (index / inp.width) / (float)inp.width));
        for(int x = Mathf.Max(indexPos.x - maxCheckRange,0); x < Mathf.Min(indexPos.x +maxCheckRange, inp.biome_map.width); x++) 
        {
            for (int y = Mathf.Max(indexPos.y - maxCheckRange, 0); y < Mathf.Min(indexPos.y + maxCheckRange, inp.biome_map.height); y++)
            {
                if (inp.biome_map.map[x + y * inp.biome_map.width] == biome)
                {
                    closestRange = Mathf.Min(closestRange, Vector2Int.Distance(indexPos, new Vector2Int(x,y)));
                }
            }
        }
        return closestRange;
    }

    /// <summary>
    /// Refresh self with new surrounding cells
    /// </summary>
    /// <param name="rule">Rule of specific rule of neighbouring cell</param>
    public void Update(Rule rule)
    {
        if (collapsed || options.Count <= 0 || rule.wl_all)
            return;

        List<BiomeWeight> new_options = new List<BiomeWeight>();
        foreach (BiomeWeight option in options)
        {
            foreach (BiomeWeight w in rule.wl)
            {
                if (w.biome == option.biome)
                {
                    if (w.impact == 0)
                        break;
                    BiomeWeight new_w = option;
                    new_w.impact *= w.impact;
                    new_options.Add(new_w);
                    break;
                }
            }
        }
        if (new_options.Count != 0)
            options = new_options;
    }
}
