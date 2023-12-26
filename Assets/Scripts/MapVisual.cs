using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapVisual : MonoBehaviour
{
    // References
    [Header("References")]
    public WaveFunctionCollapse wfc;
    public MapGenerator map_gen;
    public Tilemap output_tilemap;
    public List<TileDictionaryInstance> tile_dict_list;
    Dictionary<TileID, Tile> tile_dict = new Dictionary<TileID, Tile>();
    [Header("Tweaking Values")]
    public int tiles_per_step;
    [Header("Tracking Values")]
    public TileID[,] prev_map = new TileID[0,0];
    private bool generating = false;

    private void Awake()
    {
        GenerateDictionary();
    }

    private void GenerateDictionary()
    {
        foreach(TileDictionaryInstance inst in tile_dict_list)
            tile_dict.Add(inst.id, inst.tile);
    }

    public void Update()
    {
        if (ValidMap())
            if (!IdenticalMaps(map_gen.cur_output, prev_map))
                StartCoroutine(ShowMap(map_gen.cur_output));
    }

    private IEnumerator ShowMap(TileID[,] map)
    {
        if (generating)
            yield break;
        generating = true;
        float t = Time.realtimeSinceStartup;
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        output_tilemap.ClearAllTiles();
        prev_map = new TileID[width, height];
        Vector2Int offset = new Vector2Int(0, -height/2);
        List<Vector3Int> pos_array = new List<Vector3Int>();
        List<TileBase> tile_array = new List<TileBase>();
        for(int x = 0; x < map.GetLength(0); x ++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                int index = x + y * width;
                pos_array.Add(new Vector3Int(x + offset.x, y + offset.y, 0));
                tile_array.Add(tile_dict[map[x, y]]);
                prev_map[x, y] = map[x, y];
            }
        }
        int passes = 0;
        while(passes <= 10000 && passes * tiles_per_step <= pos_array.Count())
        {
            yield return new WaitForEndOfFrame();
            Vector3Int[] split_pos_array = pos_array.Skip(tiles_per_step*passes).Take(tiles_per_step).ToArray();
            TileBase[] split_tile_array = tile_array.Skip(tiles_per_step * passes).Take(tiles_per_step).ToArray();
            output_tilemap.SetTiles(split_pos_array, split_tile_array);
            passes++;
        }
        Debug.Log("It took " + (Time.realtimeSinceStartup - t) + " seconds to display the tilemap.");
        generating = false;
    }

    private bool IdenticalMaps(TileID[,] map1, TileID[,] map2)
    {
        if (map1.GetLength(0) != map2.GetLength(0) ||
            map1.GetLength(1) != map2.GetLength(1))
            return false;
        for(int x = 0; x < map1.GetLength(0); x++)
            for (int y = 0; y < map1.GetLength(1); y++)
                if (map1[x, y] != map2[x, y])
                    return false;

        return true;
    }

    /// <summary>
    /// Checks if current map output is valid to display
    /// </summary>
    /// <returns>Bool if map is valid</returns>
    private bool ValidMap()
    {
        if (map_gen.cur_output.GetLength(0) == 0 ||
            map_gen.cur_output.GetLength(1) == 0)
            return false;
        return true;
    }

    /// <summary>
    /// UI reference to generate WFC
    /// </summary>
    public void GenMap()
    {
        if (wfc.cur_output.width == 0 || wfc.cur_output.height == 0)
        {
            Debug.LogError("Invalid WFC Output Size");
            return;
        }
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
