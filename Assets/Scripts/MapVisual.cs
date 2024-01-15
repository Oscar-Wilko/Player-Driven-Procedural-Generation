using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapVisual : MonoBehaviour
{
    [Header("References")]
    public WaveFunctionCollapse wfc;
    public MapGenerator map_gen;
    private SpriteRenderer sprite;
    public List<TileDictionaryInstance> tile_dict_list;
    Dictionary<TileID, Tile> tile_dict = new Dictionary<TileID, Tile>();

    [Header("Tweaking Values")]
    public int tiles_per_step;
    public Color biome_wall_mult;

    [Header("Tracking Values")]
    public TileInfo prev_map = new TileInfo(0,0);
    private bool generating = false;
    private bool new_gen = false;
    private Dictionary<Biome, Color> biome_dict;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        GenerateDictionary();
        biome_dict = FindObjectOfType<DrawCanvas>().BiomeDict();        
    }

    /// <summary>
    /// Generate tile dictionary from tile list
    /// </summary>
    private void GenerateDictionary()
    {
        foreach(TileDictionaryInstance inst in tile_dict_list)
            tile_dict.Add(inst.id, inst.tile);
    }

    public void Update()
    {
        if (ValidMap())
            if (!generating && new_gen)
                ShowMap(map_gen.cur_output);
    }

    private void ShowMap(TileInfo map)
    {
        if (generating)
            return;
        if (!map_gen.Looping())
            new_gen = false;
        generating = true;
        float t = Time.realtimeSinceStartup;
        prev_map.map = new TileID[map.width * map.height];
        for (int i = 0; i < map.map.Length; i++)
            prev_map.map[i] = map.map[i];
        prev_map.width = map.width;
        prev_map.height = map.height;
        
        sprite.sprite = Sprite.Create(MapToTexture(map),
            new Rect(0, 0, map.width, map.height),
            new Vector2(0.5f, 0.5f), PPU(map));

        Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to display the tilemap.");
        generating = false;
    }

    /// <summary>
    /// Calculate pixels per unit (PPU)
    /// </summary>
    /// <returns>Float of pixels per unit</returns>
    private float PPU(TileInfo map)
    {
        return Mathf.Max(map.width, map.height) * (0.0035f);
    }

    /// <summary>
    /// Convert tile map into texture
    /// </summary>
    /// <param name="map">TileInfo of map</param>
    /// <returns>Texture2D of converted texture</returns>
    private Texture2D MapToTexture(TileInfo map)
    {
        Texture2D texture = new Texture2D(map.width, map.height);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[map.map.Length];
        for (int i = 0; i < map.map.Length; i++)
        {
            // Get pixel colour of tile
            if (map.map[i] == TileID.Wall)
                pixels[i] = BiomeCol(map.biome_map[i]);
            else
                pixels[i] = tile_dict[map.map[i]].color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private Color BiomeCol(Biome biome) 
    {
        if (biome_dict.ContainsKey(biome))
            return biome_dict[biome] * biome_wall_mult;
        else
            return Color.clear;
    }

    /// <summary>
    /// Checks if current map output is valid to display
    /// </summary>
    /// <returns>Bool if map is valid</returns>
    private bool ValidMap()
    {
        if (map_gen.cur_output.width <= 0 ||
            map_gen.cur_output.height <= 0)
            return false;
        return true;
    }

    /// <summary>
    /// UI reference to generate WFC
    /// </summary>
    public void GenMap()
    {
        if (wfc.cur_output.width <= 0 ||
            wfc.cur_output.height <= 0)
            return;
        new_gen = true;
        StartCoroutine(map_gen.GenerateMap(wfc.cur_output));
    }

    /// <summary>
    /// UI reference to load map
    /// </summary>
    public void Load()
    {
        SavedTiles data = SaveSystem.LoadTileInfo("large_map");
        if (data == null)
            return;
        new_gen = true;
        map_gen.cur_output = data.tiles;
    }

    /// <summary>
    /// UI reference to save map
    /// </summary>
    public void Save()
    {
        if (wfc.cur_output.width <= 0 || wfc.cur_output.height <= 0)
            return;
        SaveSystem.SaveTileInfo(map_gen.cur_output, "large_map");
    }
}
