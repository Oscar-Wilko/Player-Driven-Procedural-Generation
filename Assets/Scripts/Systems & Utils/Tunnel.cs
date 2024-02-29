using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tunnel
{
    private static readonly List<Vector2Int> directions = new List<Vector2Int>() { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(0,-1) };

    public static List<Vector2Int> GenerateTunnel(TunnelVariables vars, Direction dir)
    {
        List<Vector2Int> tunnelTiles = new List<Vector2Int>() { Vector2Int.zero };
        System.Random rng = new System.Random(vars.seed);
        int vertices = rng.Next(Mathf.Min(vars.minVertexCount, vars.maxVertexCount), 
            Mathf.Max(vars.minVertexCount, vars.maxVertexCount));
        bool flipAim = rng.Next(0, 2) == 0;
        Vector2Int vertex1 = new Vector2Int(0,0);
        Vector2Int vertex2;
        Vector2Int vertexTemp;

        for(int i = 0; i < vertices; i ++)
        {
            // Create vertex
            float dist = rng.Next((int)(100 * Mathf.Min(vars.minVertexDist, vars.maxVertexDist)), 
                (int)(100 * Mathf.Max(vars.minVertexDist, vars.maxVertexDist))) * 0.01f;
            float ratio = rng.Next((int)(100 * Mathf.Min(vars.minRatio, vars.maxRatio)), 
                (int)(100 * Mathf.Max(vars.minRatio, vars.maxRatio))) * 0.01f;
            vertex2 = vertex1 + new Vector2Int(
                (int)(dist * Mathf.Cos(ratio * Mathf.Deg2Rad * 90)), 
                (int)(dist * Mathf.Sin(ratio * Mathf.Deg2Rad * 90)) * (i == 0 ? 1 : 2) * (flipAim ? -1 : 1));
            tunnelTiles.Add(vertex2);
            flipAim = !flipAim;
            
            // Make line between vertices
            vertexTemp = vertex1;
            int count = Mathf.Abs(vertex2.x - vertex1.x) + Mathf.Abs(vertex2.y - vertex1.y);
            for(int l = 0; l < count; l ++)
            {               
                Vector2Int diff = vertex2 - vertexTemp;
                bool horStep = rng.Next(0, Mathf.Abs(diff.x) + Mathf.Abs(diff.y)) <= Mathf.Abs(diff.x);
                vertexTemp += horStep ? Vector2Int.right : (flipAim ? Vector2Int.up : Vector2Int.down);
                tunnelTiles.Add(vertexTemp);
            }
            vertex1 = vertex2;
        }

        if (dir == Direction.Up) 
            tunnelTiles = Mirror(tunnelTiles);
        if (dir == Direction.Left) 
            tunnelTiles = FlipX(tunnelTiles);
        if (dir == Direction.Down) 
            tunnelTiles = FlipY(Mirror(tunnelTiles));

        tunnelTiles = ExpandPositions(tunnelTiles, vars.thickness);
        return tunnelTiles;
    }

    public static List<Vector2Int> FlipX(List<Vector2Int> list)
    {
        List<Vector2Int> alteredList = new List<Vector2Int>(list);
        for (int i = 0; i < alteredList.Count; i ++)
            alteredList[i] = new Vector2Int(alteredList[i].x * -1, alteredList[i].y);
        return alteredList;
    }
    
    public static List<Vector2Int> FlipY(List<Vector2Int> list)
    {
        List<Vector2Int> alteredList = new List<Vector2Int>(list);
        for(int i = 0; i < alteredList.Count; i ++)
            alteredList[i] = new Vector2Int(alteredList[i].x, alteredList[i].y * -1);
        return alteredList;
    }

    public static List<Vector2Int> Mirror(List<Vector2Int> list)
    {
        List<Vector2Int> alteredList = new List<Vector2Int>(list);
        for (int i = 0; i < list.Count; i ++)
            alteredList[i] = new Vector2Int(alteredList[i].y, alteredList[i].x);
        return alteredList;
    }

    public static List<Vector2Int> ExpandPositions(List<Vector2Int> tiles, int thickness)
    {
        List<Vector2Int> allPositions = new List<Vector2Int>(tiles);
        List<Vector2Int> newPositions = new List<Vector2Int>(tiles);
        List<Vector2Int> temp = new List<Vector2Int>();
        HashSet<Vector2Int> hash = new HashSet<Vector2Int>(tiles);
        for(int i = 0; i < thickness; i ++)
        {
            foreach(Vector2Int pos in newPositions)
            {
                foreach(Vector2Int dir in directions)
                {
                    Vector2Int target = pos + dir;
                    if (hash.Contains(target)) continue;
                    temp.Add(target);
                    allPositions.Add(target);
                    hash.Add(target);
                }
            }
            newPositions.Clear();
            foreach(Vector2Int pos in temp)
                newPositions.Add(pos);
            temp.Clear();
        }
        return allPositions;
    }

    public static void OutputList(List<Vector2Int> list)
    {
        string outputMessage = "Tunnel Vec List: ";
        foreach (Vector2Int vec in list)
            outputMessage += vec.ToString() + ", ";
        Debug.Log(outputMessage);
    }
}
