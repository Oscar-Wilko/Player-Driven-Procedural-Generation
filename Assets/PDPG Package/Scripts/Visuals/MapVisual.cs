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
    public RectTransform layers;
    public SpriteRenderer primary_sprite;
    public SpriteRenderer secondary_sprite;
    public List<TileDictionaryInstance> tile_dict_list;
    Dictionary<TileID, Tile> tile_dict = new Dictionary<TileID, Tile>();

    [Header("Tweaking Values")]
    public Color biome_wall_mult;

    [Header("Tracking Values")]
    public bool output_info;
    private bool generating = false;
    private bool new_gen = false;
    private Dictionary<Biome, Color> biome_dict;
    private Layer active_layer = Layer.FullGen;

    private void Awake()
    {
        GenerateDictionary();
        biome_dict = FindObjectOfType<DrawCanvas>().BiomeDict(); 
        foreach(LayerViewerInstance instance in layers.GetComponentsInChildren<LayerViewerInstance>())
        {
            instance.Init(this);
            instance.SetState(active_layer);
        }
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
                ShowMap(map_gen.Output());
    }

    private void ShowMap(TileInfo map)
    {
        if (generating || !map_gen.ShowVisual())
            return;
        if (!map_gen.Looping())
            new_gen = false;
        generating = true;
        float t = Time.realtimeSinceStartup;
        
        primary_sprite.sprite = Sprite.Create(MapToTexture(map),
            new Rect(0, 0, map.width, map.height),
            new Vector2(0.5f, 0.5f), PPU(map));
        SetLayer(active_layer);

        if (output_info)
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

    private float PPU(int width, int height)
    {
        return Mathf.Max(width,height) * (0.0035f);
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

    /// <summary>
    /// Convert tile map into texture
    /// </summary>
    /// <param name="map">TileInfo of map</param>
    /// <returns>Texture2D of converted texture</returns>
    private Texture2D LayerToTexture(Layers layers, bool[] chosen_layer)
    {
        Vector2Int size = new Vector2Int(layers.width, layers.height);
        Texture2D texture = new Texture2D(size.x,size.y);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size.x * size.y];
        for (int i = 0; i < size.x * size.y; i++)
        {
            if (chosen_layer[i])
                pixels[i] = Color.white;
            else
                pixels[i] = Color.black;

            pixels[i].a = 0.5f;
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
        if (map_gen.Output().width <= 0 ||
            map_gen.Output().height <= 0)
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

    public void SetLayer(Layer layer)
    {
        Layers all_layers = map_gen.GetLayers();
        active_layer = layer;
        foreach (LayerViewerInstance instance in layers.GetComponentsInChildren<LayerViewerInstance>())
        {
            instance.SetState(layer);
        }
        secondary_sprite.color = new Color(1, 1, 1, layer == Layer.FullGen ? 0 : 1);
        bool[] chosen_layer;
        switch (layer)
        {
            case Layer.FullGen:
                return;
            case Layer.CaveWith:
                chosen_layer = all_layers.layer_cave_with;
                break;
            case Layer.CaveWithout:
                chosen_layer = all_layers.layer_cave_without;
                break;
            case Layer.LargeClump:
                chosen_layer = all_layers.layer_large_clump;
                break;
            case Layer.SmallClump:
                chosen_layer = all_layers.layer_small_clump;
                break;
            case Layer.Dots:
                chosen_layer = all_layers.layer_dots;
                break;
            case Layer.WaterBefore:
                chosen_layer = all_layers.layer_water_before;
                break;
            case Layer.WaterAfter:
                chosen_layer = all_layers.layer_water_after;
                break;
            default:
                return;
        }
        if (chosen_layer == null)
            return;
        if (chosen_layer.Length == 0)
            return;
        secondary_sprite.sprite = Sprite.Create(LayerToTexture(all_layers, chosen_layer),
            new Rect(0, 0, all_layers.width, all_layers.height),
            new Vector2(0.5f, 0.5f), PPU(all_layers.width,all_layers.height));
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
        map_gen.LoadTiles(data.tiles);
        map_gen.LoadLayers(data.layers);
    }

    /// <summary>
    /// UI reference to save map
    /// </summary>
    public void Save()
    {
        if (wfc.cur_output.width <= 0 || wfc.cur_output.height <= 0)
            return;
        SaveSystem.SaveTileInfo(map_gen.Output(), map_gen.GetLayers(), "large_map");
    }
}
