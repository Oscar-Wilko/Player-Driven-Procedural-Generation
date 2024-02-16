using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    private Slider progress_slider;
    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        progress_slider = GetComponent<Slider>();
        SetVisible(false);
    }

    public void SetProgressAmount(float perc_completion)
    {
        SetVisible(true);
        progress_slider.value = perc_completion;
    }

    public void SetVisible(bool visibility) => group.alpha = visibility ? 1.0f : 0.0f;
}
