using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public bool collapsed = false;
    public Biome biome;
    public List<BiomeWeight> options;
    public int index;
    public float perc_height;
    public float perc_length;

    /// <summary>
    /// Collapse self by checking current options with ruleset
    /// </summary>
    /// <param name="ruleset">Ruleset dictating biomes interactions</param>
    /// <returns>Biome of selected biome to collapse to</returns>
    public Biome Collapse(Ruleset ruleset)
    {
        if (collapsed || options.Count <= 0)
            return biome;
        // Generate biased weights based on height and length
        List<BiomeWeight> weights = new List<BiomeWeight>();
        float total_weight = 0;
        for (int i = 0; i < options.Count; i++)
        {
            BiomeWeight weight = options[i];
            weight.impact *= WaveFunctionCollapse.WeightAtHeight(ruleset, weight.biome, perc_height);
            weight.impact *= WaveFunctionCollapse.WeightAtLength(ruleset, weight.biome, perc_length);
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
            foreach (BiomeWeight w in rule.wl)
                if (w.biome == option.biome)
                {
                    if (w.impact == 0)
                        break;
                    BiomeWeight new_w = option;
                    new_w.impact *= w.impact;
                    new_options.Add(new_w);
                    break;
                }
        if (new_options.Count == 0)
            Debug.LogError("No more options");
        else
            options = new_options;
    }
}
