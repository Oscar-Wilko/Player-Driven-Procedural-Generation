using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Testing : MonoBehaviour
{
    public RawImage img;
    public WaveVariables vars;
    public int size;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            StartTest();
    }

    public void StartTest()
    {
        float[,] noise = Noise.Generate2DLevels(new Vector2Int(size,size), vars);
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        for(int i = 0; i < size * size; i++)
        {
            colors[i] = noise[i % size, i / size] * Color.white;
            colors[i].a = 1;
        }
        texture.SetPixels(colors);
        texture.filterMode= FilterMode.Point;
        img.texture = texture;
    }
}
