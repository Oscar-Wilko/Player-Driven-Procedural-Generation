using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DrawCanvas : MonoBehaviour
{
    [Header("References")]
    public RectTransform[] palette_transforms;
    public GameObject palette_instance;
    public Texture2D referenced_texture;
    private SpriteRenderer sprite;

    [Header("Tweaking Variables")]
    public float scale_factor;
    public float reference_scaling;
    public Vector2Int texture_size;
    public BiomePixel[] biome_palette;

    [Header("Trackers")]
    public BiomePixel selected_pixel;
    private Biome[] current_map;

    [Header("Events")]
    public UnityEvent<int> SizeChangedX;
    public UnityEvent<int> SizeChangedY;

    [Header("Constants")]
    private Vector2Int[] fill_directions = { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(0,-1) };

    public void Awake()
    {
        GenerateSprite();
        GeneratePalette();
        if (biome_palette.Length > 0)
            selected_pixel = biome_palette[0];
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
            DrawAttempt(selected_pixel);
        else if (Input.GetMouseButtonDown(1))
            FillAttempt(selected_pixel);
    }
    #region Sprite Generation
    /// <summary>
    /// Create sprite of pixels to be drawn upon
    /// </summary>
    private void GenerateSprite()
    {
        current_map = new Biome[texture_size.x * texture_size.y];
        Texture2D blank_texture = BlankTexture(texture_size, biome_palette[0]);
        sprite = GetComponent<SpriteRenderer>();
        sprite.sprite = Sprite.Create(blank_texture, 
            new Rect(0, 0, texture_size.x, texture_size.y), 
            new Vector2(0.5f, 0.5f), PPU());
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
        for (int i = 0; i < palette_transforms.Length; i++)
        {
            palette_transforms[i].sizeDelta = new Vector2(palette_transforms[i].sizeDelta.x,
                biome_palette.Length * palette_instance.GetComponent<RectTransform>().rect.height + (biome_palette.Length + 1) * 8);
            foreach (BiomePixel pixel in biome_palette)
            {
                GameObject instance = Instantiate(palette_instance, palette_transforms[i]);
                instance.GetComponent<BiomePaletteInstance>().GenerateInstance(pixel, this);
            }
        }
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
            current_map[index] = biome.biome;
        }

        // Update sprite to new pixels
        base_texture.SetPixels(pixels);
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture, 
            new Rect(0, 0, texture_size.x, texture_size.y), 
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
            new Rect(0, 0, texture_size.x, texture_size.y), 
            new Vector2(0.5f, 0.5f), PPU());
    }
    #endregion
    #region Biome Conversion    
    /// <summary>
    /// Get biome pixel of certain name
    /// </summary>
    /// <param name="name">String of input name</param>
    /// <returns>BiomePixel of pixel data</returns>
    private BiomePixel PixelFromBiome(Biome biome)
    {
        if (biome_palette.Length == 0)
            return new BiomePixel();
        foreach(BiomePixel pixel in biome_palette) 
            if (pixel.biome == biome) 
                return pixel;
        return biome_palette[0];
    }

    public Dictionary<Biome, Color> BiomeDict()
    {
        Dictionary<Biome, Color> dict = new Dictionary<Biome, Color>();
        foreach (BiomePixel pixel in biome_palette)
            dict.Add(pixel.biome, pixel.colour);
        return dict;
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
    #endregion
    #region Calculations
    /// <summary>
    /// Checks if given position is within bounds of rect
    /// </summary>
    /// <param name="pos">Vector2 of input position</param>
    /// <returns>Bool if within bounds</returns>
    private bool VecInRect(Vector2 pos)
    {
        Vector2 rect_size = (Vector2)texture_size / PPU();
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
        Vector2 rect_size = (Vector2)texture_size / PPU();
        Vector2 input_pos = pos - (new Vector2(sprite.transform.position.x, sprite.transform.position.y) - rect_size * 0.5f);
        input_pos /= rect_size;
        Vector2Int pixel_pos = new Vector2Int((int)(input_pos.x * texture_size.x), (int)(input_pos.y * texture_size.y));
        return pixel_pos;
    }

    /// <summary>
    /// Calculate variance between two colours
    /// </summary>
    /// <param name="a">Color of first colour to check</param>
    /// <param name="b">Color of second colour to check</param>
    /// <returns>Float of colour variance</returns>
    private static float ColorVariance(Color a, Color b)
    {
        float variance = 0;
        variance += Mathf.Abs(a.r - b.r);
        variance += Mathf.Abs(a.g - b.g);
        variance += Mathf.Abs(a.b - b.b);
        return variance;
    }

    /// <summary>
    /// Get texture pixels per unit value
    /// </summary>
    /// <returns>Float of pixels per unit</returns>
    private float PPU() => Mathf.Max(texture_size.x * 0.6f, texture_size.y) * scale_factor;

    /// <summary>
    /// Crop existing texture and map to new bounds size
    /// </summary>
    /// <param name="new_size">Vector2Int of new bounds size</param>
    private void CropTextureSize(Vector2Int new_size)
    {
        Biome[] new_map = new Biome[new_size.x * new_size.y];
        for(int x = 0; x < new_size.x; x++)
        {
            for (int y = 0; y < new_size.y; y++)
            {
                if (x >= texture_size.x || y >= texture_size.y)
                {
                    new_map[x + y * new_size.x] = Biome.Standard;
                }
                else
                {
                    new_map[x + y * new_size.x] = current_map[x + y * texture_size.x];
                }
            }
        }
        texture_size = new_size;
        current_map = new_map;

        // Generate texture with new pixel map
        Texture2D base_texture = new Texture2D(new_size.x, new_size.y);
        base_texture.filterMode = FilterMode.Point;
        base_texture.SetPixels(MapToColours(new_map));
        base_texture.Apply();

        sprite.sprite = Sprite.Create(base_texture,
            new Rect(0, 0, texture_size.x, texture_size.y),
            new Vector2(0.5f, 0.5f), PPU());
    }
    #endregion
    #region Map Conversion
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
    #endregion
    #region Exports & Imports
    /// <summary>
    /// Export image information on current canvas
    /// </summary>
    /// <returns>ImageInformation of image</returns>
    private MapInfo ExportImage()
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
    private void ImportImage(MapInfo info)
    {
        current_map = info.map;
        SizeChangedX.Invoke(info.width);
        SizeChangedY.Invoke(info.height);
        texture_size = new Vector2Int(info.width, info.height);
        Texture2D texture = new Texture2D(info.width, info.height);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(MapToColours(info.map));
        texture.Apply();
        sprite.sprite = Sprite.Create(texture,
            new Rect(0, 0, info.width, info.height),
            new Vector2(0.5f, 0.5f), PPU());
    }

    /// <summary>
    /// Import texture onto canvas
    /// </summary>
    /// <param name="texture">Texture2D of imported texture</param>
    private void ImportTexture(Texture2D texture)
    {
        current_map = TextureToBiomes(texture);
        SizeChangedX.Invoke(texture.width);
        SizeChangedY.Invoke(texture.height);
        texture_size = new Vector2Int(texture.width, texture.height);
        Texture2D n_texture = new Texture2D(texture.width, texture.height);
        n_texture.filterMode = FilterMode.Point;
        n_texture.SetPixels(MapToColours(current_map));
        n_texture.Apply();
        sprite.sprite = Sprite.Create(n_texture,
            new Rect(0, 0, n_texture.width, n_texture.height),
            new Vector2(0.5f, 0.5f), PPU());
    }
    #endregion
    #region Quick Functions
    public void SetPalettePixel(BiomePixel pixel) => selected_pixel = pixel;
    public Biome[] BiomeMap() => current_map;
    public void Reset() => GenerateSprite();
    public void ChangeSizeX(int new_size) => CropTextureSize(new Vector2Int(new_size, texture_size.y));
    public void ChangeSizeY(int new_size) => CropTextureSize(new Vector2Int(texture_size.x, new_size));
    public void SaveCanvas() => SaveSystem.SaveImageInfo(ExportImage(), "canvas");
    public void LoadTexture() => ImportTexture(referenced_texture);
    public void LoadCanvas()
    {
        SavedImage data = SaveSystem.LoadImageInfo("canvas");
        if (data == null)
            return;
        ImportImage(data.info);
    }
    #endregion
}