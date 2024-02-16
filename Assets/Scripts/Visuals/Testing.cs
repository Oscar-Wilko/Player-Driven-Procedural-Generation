using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Testing : MonoBehaviour
{
    public RawImage img;
    private int seed = 13241234;
    public void StartTest()
    {
        Texture2D texture = FloatMapToImg(Noise.GenerateWorley(new Vector2Int(500, 500), 64, seed, 0, 128));
        //Texture2D texture = FloatMapToImg(Noise.GenerateVoronoi(new Vector2Int(500, 500), 64, seed));
        img.texture = texture;
        seed = (seed * 19272376) % 10000000;
    }

    public static Texture2D FloatMapToImg(float[,] map)
    {
        Vector2Int scale = new Vector2Int(map.GetLength(0), map.GetLength(1));
        Texture2D texture = new Texture2D(scale.x, scale.y);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[scale.x * scale.y];
        for (int x = 0; x < scale.x; x++)
            for (int y = 0; y < scale.y; y++)
                pixels[x + y * scale.x] = new Color(map[x, y], map[x, y], map[x, y], 1);

        texture.SetPixels(pixels);
        return texture;
    }

    public static Texture2D FloatMapToImg(int[,] map)
    {
        Vector2Int scale = new Vector2Int(map.GetLength(0), map.GetLength(1));

        int max_region_number = 0;
        for (int x = 0; x < scale.x; x++)
            for (int y = 0; y < scale.y; y++)
                if (map[x, y] > max_region_number)
                    max_region_number = map[x, y];
        Debug.Log(max_region_number);
        Color[] region_colours = new Color[max_region_number+1];
        for (int i = 0; i < region_colours.Length; i++)
            region_colours[i] = new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1),1);

        Texture2D texture = new Texture2D(scale.x, scale.y);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[scale.x * scale.y];
        for (int x = 0; x < scale.x; x++)
            for (int y = 0; y < scale.y; y++)
                pixels[x + y * scale.x] = region_colours[map[x, y]];

        texture.SetPixels(pixels);
        return texture;
    }
}
