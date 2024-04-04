using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayerViewerInstance : MonoBehaviour
{
    public Layer layer;
    public Image view_icon;

    public void Init(MapVisual visual)
    {
        GetComponent<Button>().onClick.AddListener(delegate { visual.SetLayer(layer); });
    }

    public void SetState(Layer layer_comparison) => view_icon.enabled =  layer == layer_comparison;
}
