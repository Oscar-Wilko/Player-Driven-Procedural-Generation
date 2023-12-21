using UnityEngine;

public class WFCVisual : MonoBehaviour
{
    // References
    [Header("References")]
    public WaveFunctionCollapse wfc;
    private SpriteRenderer sprite;
    public DrawCanvas canvas;
    // Tweaks
    [Header("Tweaking Variables")]
    public Vector2Int size;
    // Tracking
    private Biome[] last_map;
    // Rules
    [Header("Rulesets")]
    public Ruleset default_ruleset;
    public Ruleset test_ruleset;

    public void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        // Valid Output
        if (ValidMap())
        {
            sprite.sprite = Sprite.Create(canvas.MapToTexture(wfc.cur_output),
                new Rect(0, 0, size.x, size.y),
                new Vector2(0.5f, 0.5f), PPU());
        }
    }

    /// <summary>
    /// Checks if current map output is valid to display
    /// </summary>
    /// <returns>Bool if map is valid</returns>
    private bool ValidMap()
    {
        if (wfc.cur_output.width == 0 ||
            wfc.cur_output.height == 0)
            return false;
        return true;
    }

    /// <summary>
    /// UI reference to generate WFC
    /// </summary>
    public void GenWFC()
    {
        if (size.x <= 0 || size.y <= 0)
        {
            Debug.LogError("Invalid WFC Output Size");
            return;
        }
        wfc.GenerateWFC(canvas.BiomeMap(), canvas.texture_size, size);
        //wfc.GenerateWFC(default_ruleset, size);
        test_ruleset = WaveFunctionCollapse.GenerateRuleset(canvas.BiomeMap(), canvas.texture_size, false);
    }

    /// <summary>
    /// Calculate pixels per unit (PPU)
    /// </summary>
    /// <returns>Float of pixels per unit</returns>
    private float PPU()
    {
        return Mathf.Max(size.x, size.y) * (0.12f);
    }

    /// <summary>
    /// UI reference to import WFC map
    /// </summary>
    public void Import()
    {
        SavedImage data = SaveSystem.LoadImageInfo("map");
        if (data == null)
            return;
        wfc.cur_output = data.info;
    }

    /// <summary>
    /// UI reference to export WFC map
    /// </summary>
    public void Export()
    {
        if (wfc.cur_output.width <= 0 || wfc.cur_output.height <= 0)
            return;
        SaveSystem.SaveImageInfo(wfc.cur_output, "map");
    }
}