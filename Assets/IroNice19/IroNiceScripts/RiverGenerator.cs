using System.Collections;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Mesh;

public class RiverGenerator : MonoBehaviour
{
    public Terrain terrain;

    public int posRiverX;
    public int posRiverY;
    public int riverWidth;
    public int riverLength;
    public float riverHeight;

    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public float sizeRiver;

    public bool autoUpdate;

    [SerializeField]
    private float[,] originalHeights;


    

    public AnimationCurve riverCurveByLength;
    public AnimationCurve riverCurve;

    public int riverId;
    Dictionary<int, River> rivers = new Dictionary<int, River>();

    
   

    [ContextMenu("River")]
    public void riverGenerator()
    {
        
        TerrainData terrainData = terrain.terrainData;
        RiverManager river = FindAnyObjectByType<RiverManager>(); // Проставляем высоты для вершин реки
        Debug.Log("fd");
        int riverLength;
        int riverWidth;
        riverWidth = (int) ((Mathf.Round((this.riverWidth-1) / 8f) * 8) + 1);
        riverLength = (int)((Mathf.Round((this.riverLength - 1) / 8f) * 8) + 1);

        Vector2[] riverLineTop = new Vector2[riverLength];
        Vector2[] riverLineDown = new Vector2[riverLength];


        Vector2[] riverVertices = LineRiverGenerator.riverVertices(
            ref riverLineTop,
            ref riverLineDown,
            sizeRiver,
            posRiverX,
            posRiverY,
            riverWidth,
            riverLength,
            noiseScale,
            octaves,
            persistance,
            lacunarity,
            riverCurveByLength,
            riverCurve
        );

        if (rivers.ContainsKey(riverId))
        {
            MeshData meshData = RiverMeshCreate.CreateMeshData(riverLineDown,riverLineTop, riverVertices, riverLength, riverWidth, riverHeight);
            MeshFilter meshFilter = rivers[riverId].meshObject.GetComponent<MeshFilter>();
            Mesh mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh = meshData.CreateMesh();
            meshFilter.mesh = mesh;
            
           
        } else
        {
            rivers.Add(riverId, new River());
            rivers[riverId].meshObject = river.CreateRiver(terrain, RiverMeshCreate.CreateMeshData(riverLineDown, riverLineTop, riverVertices, riverLength, riverWidth, riverHeight), riverId);
        }
        rivers[riverId].heightsTerrainPos = riverVertices;
        rivers[riverId].riverLineTop = riverLineTop;
        rivers[riverId].riverLineDown = riverLineDown;
        SetRiverValues();





    }

    [ContextMenu("GetRiverValues")]
    private void GetRiverValues()
    {
        MeshFilter meshFilter = rivers[riverId].meshObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Mesh mesh = meshFilter.mesh;
            Debug.Log($"Река {riverId} содержит {mesh.vertexCount} вершин и {mesh.triangles.Length / 3} треугольников.");
        }
        
        this.riverHeight = rivers[riverId].riverHeight;
        this.riverCurve = new AnimationCurve(rivers[riverId].riverCurve.keys);
        this.riverCurveByLength = new AnimationCurve(rivers[riverId].riverCurveByLength.keys);
        this.lacunarity = rivers[riverId].lacunarity;
        this.persistance = rivers[riverId].persistance;
        this.octaves = rivers[riverId].octaves;
        this.noiseScale = rivers[riverId].noiseScale;
        this.riverLength = rivers[riverId].riverLength;
        this.riverWidth = rivers[riverId].riverWidth;
        this.posRiverY = rivers[riverId].posRiverY;
        this.posRiverX = rivers[riverId].posRiverX;
        this.sizeRiver = rivers[riverId].sizeRiver;
    }

    
    private void SetRiverValues()
    {
        MeshFilter meshFilter = rivers[riverId].meshObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            Mesh mesh = meshFilter.mesh;
            Debug.Log($"Река {riverId} содержит {mesh.vertexCount} вершин и {mesh.triangles.Length / 3} треугольников.");
        }
        rivers[riverId].sizeRiver = this.sizeRiver;
        rivers[riverId].posRiverX = this.posRiverX;
        rivers[riverId].posRiverY = this.posRiverY;
        rivers[riverId].riverWidth = this.riverWidth;
        rivers[riverId].riverLength = this.riverLength;
        rivers[riverId].noiseScale = this.noiseScale;
        rivers[riverId].octaves = this.octaves;
        rivers[riverId].persistance = this.persistance;
        rivers[riverId].lacunarity = this.lacunarity;
        rivers[riverId].riverCurveByLength = new AnimationCurve(this.riverCurveByLength.keys);
        rivers[riverId].riverCurve = new AnimationCurve(this.riverCurve.keys);
        rivers[riverId].riverHeight = this.riverHeight;


    }


    [ContextMenu("Terr")]
    private void UpdateTerrain()
    {
        OriginalTerrainHeight originalTerrainHeight = FindObjectOfType<OriginalTerrainHeight>();

        if (originalTerrainHeight == null)
        {
            Debug.LogError("OriginalTerrainHeight component not found in the scene!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        float[,] heights = originalTerrainHeight.LoadHeightsFromBinary();

        Debug.Log(terrainData.name);
        if (heights == null)
        {
            Debug.LogError("Failed to load heights.");
            return;
        }
        Debug.Log($"Heightmap Resolution: {terrainData.heightmapResolution}");
        Debug.Log($"Heightmap Scale: {terrainData.heightmapScale}");

        Debug.Log(rivers.Count);
        foreach (int key in rivers.Keys)
        {
            Debug.Log(1);

            if (rivers[key].heightsTerrainPos == null || rivers[key].heightsTerrainPos.Length == 0)
            {
                Debug.LogWarning($"rivers[{key}].heightsTerrainPos порожній, пропускаємо.");
                continue;
            }
            foreach (var vertex in rivers[key].heightsTerrainPos)
            {
                int x = Mathf.Clamp((int)vertex.x, 0, terrainData.heightmapResolution - 1);
                int y = Mathf.Clamp((int)vertex.y, 0, terrainData.heightmapResolution - 1);

                heights[y, x] = -0.1f;



            }
        }


        terrainData.SetHeights(0, 0, heights);
        terrain.Flush();


    }
    [ContextMenu("TERRAINUPDATE")]
    public void Uper()
    {
        terrain.Flush();

    }
    [ContextMenu("SaveDataRivers")]
    public void SaveDataRivers()
    {
        OriginalTerrainHeight originalTerrainHeight = FindObjectOfType<OriginalTerrainHeight>();

        originalTerrainHeight.SaveRiversToBinary(rivers);
    }

    [ContextMenu("LoadDataRivers")]
    public void LoadMyDataRivers()
    {
        rivers = LoadDataRivers(rivers);
    }
    public Dictionary<int, River> LoadDataRivers(Dictionary<int, River> rivers)
    {
        OriginalTerrainHeight originalTerrainHeight = FindObjectOfType<OriginalTerrainHeight>();

        rivers = originalTerrainHeight.LoadRiversFromBinary();
        return rivers;
    }




}




public class River
{
    public GameObject meshObject;
    public Vector2[] heightsTerrainPos;
    public Vector2[] riverLineTop;
    public Vector2[] riverLineDown;

    public int posRiverX;
    public int posRiverY;
    public int riverWidth;
    public int riverLength;
    public float riverHeight;

    public float noiseScale; 
    public int octaves;
    public float persistance;
    public float lacunarity;
    public float sizeRiver;
    public AnimationCurve riverCurveByLength;
    public AnimationCurve riverCurve;

}
