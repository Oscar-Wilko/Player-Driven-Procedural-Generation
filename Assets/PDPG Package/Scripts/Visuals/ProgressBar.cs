using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    private Slider progress_slider;
    private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI prev_text;
    [SerializeField] private TextMeshProUGUI next_text;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        progress_slider = GetComponent<Slider>();
        SetVisible(false);
    }

    public void SetProgressAmount(float perc_completion, string just_accomplished, string next_process)
    {
        SetVisible(true);
        progress_slider.value = perc_completion;
        prev_text.text = (just_accomplished != "" ? "Just Done:" : "") + just_accomplished;
        next_text.text = (next_process != "" ? "Next Task:" : "") + next_process;
    }

    public void SetVisible(bool visibility) => group.alpha = visibility ? 1.0f : 0.0f;
}
