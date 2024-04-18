using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WFCVisual : MonoBehaviour
{
    [Header("References")]
    public WaveFunctionCollapse wfc;
    public DrawCanvas canvas;
    private SpriteRenderer sprite;

    [Header("Tweaking Variables")]
    public float scale_factor;
    public Vector2Int size;

    [Header("Events")]
    public UnityEvent<int> SizeChangedX;
    public UnityEvent<int> SizeChangedY;

    // Consts and Trackers
    private bool new_gen = false;
    private Vector2Int[] fill_directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };

    public void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        if (ValidMap())
        {
            if (Input.GetMouseButton(0))
                DrawAttempt(canvas.selected_pixel);
            else if (Input.GetMouseButtonDown(1))
                FillAttempt(canvas.selected_pixel);

            if (new_gen)
            {
                sprite.sprite = Sprite.Create(canvas.MapToTexture(wfc.cur_output),
                    new Rect(0, 0, wfc.cur_output.width, wfc.cur_output.height),
                    new Vector2(0.5f, 0.5f), PPU());
                if (!wfc.Looping())
                    new_gen = false;
            }
        }
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
    }
    #region Calculations
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
    /// Checks if given position is within bounds of rect
    /// </summary>
    /// <param name="pos">Vector2 of input position</param>
    /// <returns>Bool if within bounds</returns>
    private bool VecInRect(Vector2 pos)
    {
        Vector2 rect_size = (Vector2)size / PPU();
        if (pos.x > sprite.transform.position.x + rect_size.x * 0.5f
            || pos.x < sprite.transform.position.x - rect_size.x * 0.5f
            || pos.y > sprite.transform.position.y + rect_size.y * 0.5f
            || pos.y < sprite.transform.position.y - rect_size.y * 0.5f)
            return false;
        return true;
    }

    /// <summary>
    /// Converts world position to pixel position on texture
    /// </summary>
    /// <param name="pos">Vector2 of input position</param>
    /// <returns>Vector2Int of pixel position</returns>
    private Vector2Int GetPixelPosition(Vector2 pos)
    {
        Vector2 rect_size = (Vector2)size / PPU();
        Vector2 input_pos = pos - (new Vector2(sprite.transform.position.x, sprite.transform.position.y) - rect_size * 0.5f);
        input_pos /= rect_size;
        Vector2Int pixel_pos = new Vector2Int((int)(input_pos.x * size.x), (int)(input_pos.y * size.y));
        return pixel_pos;
    }

    /// <summary>
    /// Calculate pixels per unit (PPU)
    /// </summary>
    /// <returns>Float of pixels per unit</returns>
    private float PPU() => Mathf.Max(size.x*0.6f, size.y) * scale_factor;

    /// <summary>
    /// Crop the texture to a new size
    /// </summary>
    /// <param name="new_size">Vector2Int of new texture size</param>
    private void CropTextureSize(Vector2Int new_size)
    {
        if (!ValidMap())
        {
            size = new_size;
            return;
        }
        Biome[] new_map = new Biome[new_size.x * new_size.y];
        for (int x = 0; x < new_size.x; x++)
        {
            for (int y = 0; y < new_size.y; y++)
            {
                if (x >= size.x || y >= size.y)
                {
                    new_map[x + y * new_size.x] = Biome.Standard;
                }
                else
                {
                    new_map[x + y * new_size.x] = wfc.cur_output.map[x + y * size.x];
                }
            }
        }
        size = new_size;
        wfc.cur_output.width = size.x;
        wfc.cur_output.height = size.y;
        wfc.cur_output.map = new_map;

        // Get Texture of current raw image
        Texture2D base_texture = new Texture2D(new_size.x, new_size.y);
        base_texture.filterMode = FilterMode.Point;
        Color[] pixels = canvas.MapToColours(new_map);

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture,
            new Rect(0, 0, size.x, size.y),
            new Vector2(0.5f, 0.5f), PPU());
    }
    #endregion
    #region Tools
    /// <summary>
    /// Attempt to draw on canvas with given colour
    /// </summary>
    /// <param name="biome">BiomePixel of chosen 'pen'</param>
    private void DrawAttempt(BiomePixel biome)
    {
        Vector2 m_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!VecInRect(m_pos)) return;

        // Get Texture of current raw image
        Texture2D base_texture = sprite.sprite.texture;
        base_texture.filterMode = FilterMode.Point;
        Color[] pixels = base_texture.GetPixels();

        // Get mouse position on texture
        Vector2Int m_grid = GetPixelPosition(m_pos);
        int index = m_grid.x + m_grid.y * base_texture.width;
        // Alter colour
        if (pixels[index].a != 0)
        {
            pixels[index] = biome.colour;
            wfc.cur_output.map[index] = biome.biome;
        }

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture,
            new Rect(0, 0, size.x, size.y),
            new Vector2(0.5f, 0.5f), PPU());
    }

    /// <summary>
    /// Attempt to fill on canvas with given colour
    /// </summary>
    /// <param name="biome">BiomePixel of chosen 'pen'</param>
    private void FillAttempt(BiomePixel biome)
    {
        Vector2 m_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!VecInRect(m_pos)) return;

        // Get Texture of current raw image
        Texture2D base_texture = sprite.sprite.texture;
        base_texture.filterMode = FilterMode.Point;
        Color[] pixels = base_texture.GetPixels();

        // Get mouse position on texture
        Vector2Int m_grid = GetPixelPosition(m_pos);
        int index = m_grid.x + m_grid.y * base_texture.width;
        // Alter colour
        if (pixels[index].a == 0)
            return;
        Color target_colour = pixels[index];

        // Generate queue of current map, new spots and temporary spots
        Queue<Vector2Int> new_spots = new Queue<Vector2Int>(), temp_spots = new Queue<Vector2Int>();
        HashSet<Vector2Int> hash_set = new HashSet<Vector2Int>() { m_grid };
        new_spots.Enqueue(m_grid);
        // Repeat until there are no more spots to check
        do
        {
            // For every new found spot
            foreach (Vector2Int spot in new_spots)
            {
                // Set to chosen colour
                pixels[spot.x + spot.y * base_texture.width] = biome.colour;
                wfc.cur_output.map[spot.x + spot.y * base_texture.width] = biome.biome;
                // Calculate and add neighbours
                foreach (Vector2Int dir in fill_directions)
                {
                    Vector2Int target_vec = spot + dir;
                    if (target_vec.x < 0 || target_vec.x >= base_texture.width) continue;
                    if (target_vec.y < 0 || target_vec.y >= base_texture.height) continue;
                    Color t_col = pixels[target_vec.x + target_vec.y * base_texture.width];

                    // Condition checking
                    if (t_col.a == 0) continue;
                    if (t_col != target_colour) continue;
                    if (hash_set.Contains(target_vec)) continue;
                    temp_spots.Enqueue(target_vec);
                    hash_set.Add(target_vec);
                }
            }
            // Transfer temporary spots onto new spots
            new_spots.Clear();
            foreach (Vector2Int spot in temp_spots) 
                new_spots.Enqueue(spot);
            temp_spots.Clear();
        } while (new_spots.Count != 0);

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture,
            new Rect(0, 0, size.x, size.y),
            new Vector2(0.5f, 0.5f), PPU());
    }
    #endregion
    #region Save&Load
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
        size.x = wfc.cur_output.width;
        size.y = wfc.cur_output.height;
        SizeChangedX.Invoke(wfc.cur_output.width);
        SizeChangedY.Invoke(wfc.cur_output.height);
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
    #endregion
    public void ChangeSizeX(int new_size)
    {
        if (!wfc.Looping()) 
            CropTextureSize(new Vector2Int(new_size, size.y));
        else
            SizeChangedX.Invoke(size.x);
    }
    public void ChangeSizeY(int new_size)
    {
        if (!wfc.Looping())
            CropTextureSize(new Vector2Int(size.x, new_size));
        else
            SizeChangedY.Invoke(size.y);
    }
}