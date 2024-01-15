using UnityEngine;

public class WFCVisual : MonoBehaviour
{
    [Header("References")]
    public WaveFunctionCollapse wfc;
    private SpriteRenderer sprite;
    public DrawCanvas canvas;

    [Header("Tweaking Variables")]
    public Vector2Int size;

    [Header("Rulesets")]
    public Ruleset default_ruleset;
    public Ruleset test_ruleset;

    private bool new_gen = false;

    public void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        // Valid Output
        if (ValidMap() && new_gen)
        {
            sprite.sprite = Sprite.Create(canvas.MapToTexture(wfc.cur_output),
                new Rect(0, 0, wfc.cur_output.width, wfc.cur_output.height),
                new Vector2(0.5f, 0.5f), PPU());
            if (!wfc.Looping())
                new_gen = false;
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
        new_gen = true;
        wfc.GenerateWFC(canvas.BiomeMap(), canvas.texture_size, size);
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
    /// UI reference to load WFC map
    /// </summary>
    public void Load()
    {
        SavedImage data = SaveSystem.LoadImageInfo("map");
        if (data == null)
            return;
        new_gen = true;
        wfc.cur_output = data.info;
    }

    /// <summary>
    /// UI reference to save WFC map
    /// </summary>
    public void Save()
    {
        if (wfc.cur_output.width <= 0 || wfc.cur_output.height <= 0)
            return;
        SaveSystem.SaveImageInfo(wfc.cur_output, "map");
    }
}