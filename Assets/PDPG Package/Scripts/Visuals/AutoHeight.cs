using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoHeight : MonoBehaviour
{
    List<RectTransform> rects = new List<RectTransform>();
    RectTransform selfRect;
    public float padding;
    public float spacing;

    private void Awake()
    {
        selfRect = GetComponent<RectTransform>();
        for(int i = 0; i < transform.childCount; i ++)
        {
            RectTransform rect;
            if (transform.GetChild(i).TryGetComponent(out rect))
                rects.Add(rect);
        }
    }

    private void LateUpdate()
    {
        float height = 2 * padding - spacing;
        foreach(RectTransform rect in rects)
        {
            height += padding + rect.sizeDelta.y;
        }
        selfRect.sizeDelta = new Vector2(0, height);
    }
}
