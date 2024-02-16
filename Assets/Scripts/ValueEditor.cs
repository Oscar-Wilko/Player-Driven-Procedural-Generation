using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ValueEditor : MonoBehaviour
{
    [Header("Constraints")]
    [SerializeField] private int min_val;
    [SerializeField] private int max_val;
    [SerializeField] private int default_value;
    private int value = -1;
    [Header("References")]
    [SerializeField] private Slider val_slider;
    [SerializeField] private TMP_InputField val_input;
    [Space]
    public UnityEvent<int> ValueChanged;

    private void Start()
    {
        if (val_slider)
        {
            val_slider.minValue= min_val;
            val_slider.maxValue= max_val;
        }

        UpdateValue(default_value);
    }

    public void RefreshValue()
    {
        int prev_val = value;
        if ((int)val_slider.value != value)
            UpdateValue((int)val_slider.value);
        else if (int.Parse(val_input.text) != value)
            UpdateValue(int.Parse(val_input.text));
        if (prev_val != value && prev_val > 0)
            ValueChanged.Invoke(value);
    }

    public void UpdateValue(int new_val) 
    {
        value = Mathf.Clamp(new_val, min_val, max_val);
        if (val_slider)
            val_slider.SetValueWithoutNotify(value);
        if (val_input)
            val_input.SetTextWithoutNotify(value.ToString());
    }
}
