using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFCVisual : MonoBehaviour
{
    private SpriteRenderer sprite;
    public WaveFunctionCollapse wfc;
    public DrawCanvas canvas;
    public Vector2Int size;
    public Ruleset default_ruleset;

    public void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        if (wfc.cur_output.width != 0 && wfc.cur_output.height != 0)
            sprite.sprite = Sprite.Create(canvas.WFCToTexture(wfc.cur_output),
                new Rect(0, 0, size.x, size.y),
                new Vector2(0.5f, 0.5f), PPU());
    }

    public void GenWFC()
    {
        if (size.x <= 0 || size.y <= 0)
        {
            Debug.LogError("Invalid WFC Output Size");
            return;
        }
        wfc.GenerateWFC(default_ruleset, size);
    }

    private float PPU()
    {
        if (size.x > size.y)
            return size.x * 6 / 50f;
        else
            return size.y * 6 / 50f;
    }
}
