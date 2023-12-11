using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BiomePixel
{
    public int ID;
    public Biome biome;
    public Color colour;
}

[System.Serializable]
public struct MapInfo
{
    public Biome[] map;
    public int width;   
    public int height;
}

public enum Biome
{
    Stone,
    Surface,
    Ice,
    Desert,
    Fire,
    Wind
}

public class DrawCanvas : MonoBehaviour
{
    // Refs
    [Header("References")]
    public RectTransform palette_transform;
    public GameObject palette_instance;
    private SpriteRenderer sprite;
    
    // Consts
    private Vector2Int[] fill_directions = { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(0,-1) };

    // Balancing Variables
    [Header("Tweak Variables")]
    public float texture_pixels_per_unit;
    public Vector2Int texture_size;
    public BiomePixel[] biome_palette;

    // Trackers
    private BiomePixel selected_pixel;
    private Biome[] current_map;

    public void Awake()
    {
        GenerateSprite();
        GeneratePalette();
        SetPalettePixel(0);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
            DrawAttempt(selected_pixel);
        else if (Input.GetMouseButtonDown(1))
            FillAttempt(selected_pixel);
    }

    /// <summary>
    /// Create sprite of pixels to be drawn upon
    /// </summary>
    private void GenerateSprite()
    {
        current_map = new Biome[texture_size.x * texture_size.y];
        Texture2D blank_texture = BlankTexture(texture_size, PixelFromID(0));
        sprite = GetComponent<SpriteRenderer>();
        sprite.sprite = Sprite.Create(blank_texture, 
            new Rect(0, 0, texture_size.x, texture_size.y), 
            new Vector2(0.5f, 0.5f), texture_pixels_per_unit);
    }

    /// <summary>
    /// Generate a blank texture of a certain size and tile
    /// </summary>
    /// <returns></returns>
    private Texture2D BlankTexture(Vector2Int _size, BiomePixel _biome)
    {
        Texture2D _texture = new Texture2D(_size.x, _size.y);
        Color[] colours = _texture.GetPixels();
        for (int i = 0; i < colours.LongLength; i++)
        {
            colours[i] = _biome.colour;
            current_map[i] = _biome.biome;
        }
        _texture.SetPixels(colours);
        _texture.Apply();

        return _texture;
    }

    /// <summary>
    /// Generate palette of biome pixels to pick
    /// </summary>
    private void GeneratePalette()
    {
        palette_transform.sizeDelta = new Vector2(palette_transform.sizeDelta.x,
            biome_palette.Length * palette_instance.GetComponent<RectTransform>().rect.height + (biome_palette.Length + 1) * 8);
        foreach (BiomePixel pixel in biome_palette)
        {
            GameObject instance = Instantiate(palette_instance, palette_transform);
            instance.GetComponent<BiomePaletteInstance>().GenerateInstance(pixel, this);
        }
    }

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
            current_map[index] = biome.biome;
        }

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture, 
            new Rect(0, 0, texture_size.x, texture_size.y), 
            new Vector2(0.5f, 0.5f), texture_pixels_per_unit);
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
        Queue<Vector2Int> current_spots = new Queue<Vector2Int>(), new_spots = new Queue<Vector2Int>(), temp_spots = new Queue<Vector2Int>();
        HashSet<Vector2Int> hash_set = new HashSet<Vector2Int>() { m_grid };
        new_spots.Enqueue(m_grid);
        current_spots.Enqueue(m_grid);
        // Repeat until there are no more spots to check
        do
        {
            // For every new found spot
            foreach (Vector2Int spot in new_spots)
            {
                // Set to chosen colour
                pixels[spot.x + spot.y * base_texture.width] = biome.colour;
                current_map[spot.x + spot.y * base_texture.width] = biome.biome;
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
                    current_spots.Enqueue(target_vec);
                    hash_set.Add(target_vec);
                }
            }
            // Transfer temporary spots onto new spots
            new_spots.Clear();
            foreach (Vector2Int spot in temp_spots) new_spots.Enqueue(spot);
            temp_spots.Clear();
        } while (new_spots.Count != 0);

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture, 
            new Rect(0, 0, texture_size.x, texture_size.y), 
            new Vector2(0.5f, 0.5f), texture_pixels_per_unit);
    }

    public void SetPalettePixel(int ID) { selected_pixel = PixelFromID(ID); }
    public void SetPalettePixel(BiomePixel pixel) { selected_pixel = pixel; }

    /// <summary>
    /// Get biome pixel of certain ID
    /// </summary>
    /// <param name="ID">Int of input ID</param>
    /// <returns>BiomePixel of pixel data</returns>
    private BiomePixel PixelFromID(int ID)
    {
        foreach(BiomePixel pixel in biome_palette) 
            if (pixel.ID == ID) 
                return pixel;
        return biome_palette[0];
    }
    
    /// <summary>
    /// Get biome pixel of certain name
    /// </summary>
    /// <param name="name">String of input name</param>
    /// <returns>BiomePixel of pixel data</returns>
    private BiomePixel PixelFromBiome(Biome biome)
    {
        foreach(BiomePixel pixel in biome_palette) 
            if (pixel.biome == biome) 
                return pixel;
        return biome_palette[0];
    }

    /// <summary>
    /// Conver color into biome of that colour
    /// </summary>
    /// <param name="colour">Color of colour to check</param>
    /// <returns>Biome of converted biome</returns>
    private Biome BiomeFromPixel(Color colour)
    {
        float lowest_variance = float.MaxValue;
        Biome closest_biome = 0;
        foreach (BiomePixel pixel in biome_palette)
        {
            float variance = ColorVariance(colour, pixel.colour);
            if (variance < lowest_variance)
            {
                lowest_variance = variance;
                closest_biome = pixel.biome;
            }
        }
        return closest_biome;
    }

    /// <summary>
    /// Calculate variance between two colours
    /// </summary>
    /// <param name="a">Color of first colour to check</param>
    /// <param name="b">Color of second colour to check</param>
    /// <returns>Float of colour variance</returns>
    private float ColorVariance(Color a, Color b)
    {
        float variance = 0;
        variance += Mathf.Abs(a.r - b.r);
        variance += Mathf.Abs(a.g - b.g);
        variance += Mathf.Abs(a.b - b.b);
        return variance;
    }

    /// <summary>
    /// Checks if given position is within bounds of rect
    /// </summary>
    /// <param name="pos">Vector2 of input position</param>
    /// <returns>Bool if within bounds</returns>
    private bool VecInRect(Vector2 pos)
    {
        Vector2 rect_size = (Vector2)texture_size / texture_pixels_per_unit;
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
        Vector2 rect_size = (Vector2)texture_size / texture_pixels_per_unit;
        Vector2 input_pos = pos - (new Vector2(sprite.transform.position.x, sprite.transform.position.y) - rect_size * 0.5f);
        input_pos /= rect_size;
        Vector2Int pixel_pos = new Vector2Int((int)(input_pos.x * texture_size.x), (int)(input_pos.y * texture_size.y));
        return pixel_pos;
    }

    /// <summary>
    /// Export image information on current canvas
    /// </summary>
    /// <returns>ImageInformation of image</returns>
    public MapInfo ExportImage()
    {
        Texture2D texture = sprite.sprite.texture;
        MapInfo info = new MapInfo();
        info.map = current_map;
        info.width = texture.width;
        info.height = texture.height;
        return info;
    }

    /// <summary>
    /// Import map info onto canvas
    /// </summary>
    /// <param name="info">MapInfo of imported map information</param>
    public void ImportImage(MapInfo info)
    {
        current_map = info.map;
        Texture2D texture = new Texture2D(info.width, info.height);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(MapToColours(info.map));
        texture.Apply();
        sprite.sprite = Sprite.Create(texture,
            new Rect(0, 0, texture_size.x, texture_size.y),
            new Vector2(0.5f, 0.5f), texture_pixels_per_unit); ;
    }

    /// <summary>
    /// Convert texture into biome map
    /// </summary>
    /// <param name="texture">Texture2D of inputted texture</param>
    /// <returns>Biome[] of biome map</returns>
    public Biome[] TextureToBiomes(Texture2D texture)
    {
        texture.filterMode = FilterMode.Point;
        Color[] pixels = texture.GetPixels();
        Biome[] biome_map = new Biome[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            biome_map[i] = BiomeFromPixel(pixels[i]);
        return biome_map;
    }

    public Biome[] BiomeMap() { return current_map; }

    /// <summary>
    /// Convert biome map into colour map
    /// </summary>
    /// <param name="map">Biome[] of biome map</param>
    /// <returns>Color[] of colour map</returns>
    public Color[] MapToColours(Biome[] map)
    {
        Color[] colours = new Color[map.Length];
        for(int i = 0; i < map.Length; i++)
            colours[i] = PixelFromBiome(map[i]).colour;
        return colours;
    }

    /// <summary>
    /// Convert MapInfo into Texture2D
    /// </summary>
    /// <param name="map">MapInfo of requested map information</param>
    /// <returns>Texture2D of converted map</returns>
    public Texture2D MapToTexture(MapInfo map)
    {
        Texture2D texture = new Texture2D(map.width, map.height);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = MapToColours(map.map);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// Reset to default
    /// </summary>
    public void Reset()
    {
        GenerateSprite();
    }

    /// <summary>
    /// Save using save system
    /// </summary>
    public void SaveCanvas()
    {
        SaveSystem.SaveImageInfo(ExportImage(), "canvas");
    }

    /// <summary>
    /// Load using load system
    /// </summary>
    public void LoadCanvas()
    {
        SavedImage data = SaveSystem.LoadImageInfo("canvas");
        if (data == null)
            return;
        ImportImage(data.info);
    }
}