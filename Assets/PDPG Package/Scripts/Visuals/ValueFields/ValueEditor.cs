using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ValueEditor : MonoBehaviour
{
    public bool is_float;
    [Header("Int Constraints")]
    [SerializeField] private int imin_val;
    [SerializeField] private int imax_val;
    [SerializeField] private int idefault_value;
    private int ivalue = -1;
    [Header("Float Constraints")]
    [SerializeField] private float fmin_val;
    [SerializeField] private float fmax_val;
    [SerializeField] private float fdefault_value;
    private float fvalue = -1;
    [Header("References")]
    [SerializeField] private Slider val_slider;
    [SerializeField] private TMP_InputField val_input;
    [Space]
    public UnityEvent<int> IntValueChanged;
    public UnityEvent<float> FloatValueChanged;
    private bool initCheck = false;
    public bool assign_default_on_startup = false;

    private void Awake()
    {
        if (val_slider)
        {
            val_slider.minValue = is_float ? fmin_val : imin_val;
            val_slider.maxValue = is_float ? fmax_val : imax_val;
        }
        if (is_float)
        {
            UpdateFloatValue(fdefault_value);
            if (assign_default_on_startup)
            {
                FloatValueChanged.Invoke(fdefault_value);
            }
        }
        else
        {
            UpdateIntValue(idefault_value);
            if (assign_default_on_startup)
            {
                IntValueChanged.Invoke(idefault_value);
            }
        }
        initCheck = true;
    }

    public void RefreshValue()
    {
        if (!initCheck) return;
        if (is_float)
        {
            float prev_val = fvalue;
            if (val_slider.value != fvalue)
                UpdateFloatValue(Mathf.Round(val_slider.value*100)*0.01f);
            else if (float.Parse(val_input.text) != fvalue)
                UpdateFloatValue(Mathf.Round(float.Parse(val_input.text) * 100) * 0.01f);
            if (prev_val != fvalue && prev_val >= 0)
                FloatValueChanged.Invoke(fvalue);
        }
        else
        {
            int prev_val = ivalue;
            if ((int)val_slider.value != ivalue)
                UpdateIntValue((int)val_slider.value);
            else if (int.Parse(val_input.text) != ivalue)
                UpdateIntValue(int.Parse(val_input.text));
            if (prev_val != ivalue && prev_val >= 0)
            {
                IntValueChanged.Invoke(ivalue);
            }
        }
    }

    public void UpdateIntValue(int new_val) 
    {
        ivalue = Mathf.Clamp(new_val, imin_val, imax_val);
        if (val_slider)
            val_slider.SetValueWithoutNotify(ivalue);
        if (val_input)
            val_input.SetTextWithoutNotify(ivalue.ToString());
    }

    public void UpdateFloatValue(float new_val) 
    {
        fvalue = Mathf.Clamp(new_val, fmin_val, fmax_val);
        if (val_slider)
            val_slider.SetValueWithoutNotify(fvalue);
        if (val_input)
            val_input.SetTextWithoutNotify(fvalue.ToString());
    }
}
