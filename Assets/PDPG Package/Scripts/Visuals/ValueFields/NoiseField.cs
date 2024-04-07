using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NoiseField : BaseField
{
    [SerializeField] private NoiseType type;

    [Header("Events")]
    public UnityEvent<int> SetSeed;
    public UnityEvent<int> SetOctaves;
    public UnityEvent<float> SetScaleX;
    public UnityEvent<float> SetScaleY;
    public UnityEvent<float> SetPersistance;
    public UnityEvent<float> SetLacunarity;
    public UnityEvent<float> SetThreshold;


    private MapGenerator generator;

    protected override void Awake()
    {
        generator = FindObjectOfType<MapGenerator>();
        base.Awake();
    }

    public void RefreshSeed(int val) => generator.UpdateNoiseValue(type, NoiseVariable.Seed, val);
    public void RefreshScaleX(float val) => generator.UpdateNoiseValue(type, NoiseVariable.ScaleX, val);
    public void RefreshScaleY(float val) => generator.UpdateNoiseValue(type, NoiseVariable.ScaleY, val);
    public void RefreshOctaves(int val) => generator.UpdateNoiseValue(type, NoiseVariable.Octaves, val);
    public void RefreshPersistance(float val) => generator.UpdateNoiseValue(type, NoiseVariable.Persistance, val);
    public void RefreshLacunarity(float val) => generator.UpdateNoiseValue(type, NoiseVariable.Lacunarity, val);
    public void RefreshThreshold(float val) => generator.UpdateNoiseValue(type, NoiseVariable.Threshold, val);
    public void SetNewSeed(int val) => SetSeed.Invoke(val);
    public void Initialize(WaveVariables wave_vars)
    {
        SetSeed.Invoke(wave_vars.seed);
        SetOctaves.Invoke(wave_vars.octaves);
        SetScaleX.Invoke(wave_vars.scale.x);
        SetScaleY.Invoke(wave_vars.scale.y);
        SetPersistance.Invoke(wave_vars.persistance);
        SetLacunarity.Invoke(wave_vars.lacunarity);
        SetThreshold.Invoke(wave_vars.threshold);
    }
}
