using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BiomePaletteInstance : MonoBehaviour
{
    public Image colour_image;
    public TextMeshProUGUI biome_text;
    public Button select_button;

    public void GenerateInstance(BiomePixel pixel, DrawCanvas canvas)
    {
        colour_image.color = pixel.colour;
        biome_text.text = pixel.biome.ToString();
        select_button.onClick.AddListener(delegate { canvas.SetPalettePixel(pixel); });
    }
}
