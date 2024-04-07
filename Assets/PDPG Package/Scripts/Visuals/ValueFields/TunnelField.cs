using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TunnelField : BaseField
{
    [SerializeField] private TunnelType type;

    [Header("Events")]
    public UnityEvent<int> SetSeed;
    public UnityEvent<int> SetMinTunnels;
    public UnityEvent<int> SetMaxTunnels;
    public UnityEvent<int> SetMinVertexCount;
    public UnityEvent<int> SetMaxVertexCount;
    public UnityEvent<float> SetMinVertexDistance;
    public UnityEvent<float> SetMaxVertexDistance;
    public UnityEvent<float> SetMinRatio;
    public UnityEvent<float> SetMaxRatio;
    public UnityEvent<int> SetThickness;

    private MapGenerator generator;

    protected override void Awake()
    {
        generator = FindObjectOfType<MapGenerator>();
        base.Awake();
    }

    public void RefreshSeed(int val) => generator.UpdateTunnelValue(type, TunnelVariable.Seed, val);
    public void RefreshMinTunnels(int val) => generator.UpdateTunnelValue(type, TunnelVariable.MinTunnels, val);
    public void RefreshMaxTunnels(int val) => generator.UpdateTunnelValue(type, TunnelVariable.MaxTunnels, val);
    public void RefreshMinVertexCount(int val) => generator.UpdateTunnelValue(type, TunnelVariable.MinVertexCount, val);
    public void RefreshMaxVertexCount(int val) => generator.UpdateTunnelValue(type, TunnelVariable.MaxVertexCount, val);
    public void RefreshMinVertexDist(float val) => generator.UpdateTunnelValue(type, TunnelVariable.MinVertexDist, val);
    public void RefreshMaxVertexDist(float val) => generator.UpdateTunnelValue(type, TunnelVariable.MaxVertexDist, val);
    public void RefreshMinRatio(float val) => generator.UpdateTunnelValue(type, TunnelVariable.MinRatio, val);
    public void RefreshMaxRatio(float val) => generator.UpdateTunnelValue(type, TunnelVariable.MaxRatio, val);
    public void RefreshThickness(int val) => generator.UpdateTunnelValue(type, TunnelVariable.Thickness, val);
    public void SetNewSeed(int val) => SetSeed.Invoke(val);
    public void Initialize(TunnelVariables tunnel_vars)
    {
        SetSeed.Invoke(tunnel_vars.seed);
        SetMinTunnels.Invoke(tunnel_vars.minTunnels);
        SetMaxTunnels.Invoke(tunnel_vars.maxTunnels);
        SetMinVertexCount.Invoke(tunnel_vars.minVertexCount);
        SetMaxVertexCount.Invoke(tunnel_vars.maxVertexCount);
        SetMinVertexDistance.Invoke(tunnel_vars.minVertexDist);
        SetMaxVertexDistance.Invoke(tunnel_vars.maxVertexDist);
        SetMinRatio.Invoke(tunnel_vars.minRatio);
        SetMaxRatio.Invoke(tunnel_vars.maxRatio);
        SetThickness.Invoke(tunnel_vars.thickness);
    }
}
