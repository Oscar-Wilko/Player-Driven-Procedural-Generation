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

public struct ImageInformation
{
    public Color[] image;
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
    private BiomePixel selected_pixel;
    private SpriteRenderer sprite;

    public Vector2Int texture_size;
    public float texture_pixels_per_unit;
    public RectTransform palette_transform;
    public GameObject palette_instance;
    public BiomePixel[] biome_palette;

    public void Awake()
    {
        GenerateSprite();
        GeneratePalette();
        SetPalettePixel(0);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0)) 
            DrawAttempt(selected_pixel.colour);
    }

    /// <summary>
    /// Create sprite of pixels to be drawn upon
    /// </summary>
    private void GenerateSprite()
    {
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
            colours[i] = _biome.colour;
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
    /// <param name="colour">Color of chosen 'pen' colour</param>
    private void DrawAttempt(Color colour)
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
        if (pixels[index].a != 0) 
            pixels[index] = colour;

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
    /// Checks if given position is within bounds of rect
    /// </summary>
    /// <param name="pos">Vector2 of input position</param>
    /// <returns>Bool if within bounds</returns>
    private bool VecInRect(Vector2 pos)
    {
        Vector2 rect_size = (Vector2)texture_size / texture_pixels_per_unit;
        if (pos.x > sprite.transform.position.x + rect_size.x * 0.5f) return false;
        if (pos.x < sprite.transform.position.x - rect_size.x * 0.5f) return false;
        if (pos.y > sprite.transform.position.y + rect_size.y * 0.5f) return false;
        if (pos.y < sprite.transform.position.y - rect_size.y * 0.5f) return false;
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
    public ImageInformation ExportImage()
    {
        Texture2D texture = sprite.sprite.texture;
        ImageInformation info = new ImageInformation();
        info.image = texture.GetPixels();
        info.width = texture.width;
        info.height = texture.height;
        return info;
    }

    public Texture2D WFCToTexture(WFCOutput map)
    {
        Texture2D texture = new Texture2D(map.width, map.height);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[map.width * map.height];
        for(int i = 0; i < pixels.Length; i ++)
        {
            pixels[i] = PixelFromBiome(map.map[i]).colour;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
