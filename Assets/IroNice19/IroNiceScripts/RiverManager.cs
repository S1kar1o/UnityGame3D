using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mesh;

public class RiverManager : MonoBehaviour
{

    
    private float[,] originalHeights; // �������� ������ ��������

    public GameObject CreateRiver(Terrain terrain, MeshData meshData, int id)
    {
        

        string idString = id.ToString();
        // ������ ����� ����
        GameObject riverMeshObject = new GameObject("RiverMesh" + idString);
        riverMeshObject.transform.parent = terrain.transform; // ������ �������� � ��������
        riverMeshObject.transform.localPosition = Vector3.zero;

        // ��������� ����������
        MeshFilter meshFilter = riverMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = riverMeshObject.AddComponent<MeshRenderer>();

        // ������������� ��������
        Material meshMaterial = new Material(Shader.Find("Standard"));
        meshMaterial.color = Color.blue;
        meshRenderer.material = meshMaterial;

        // ������ ����� Mesh � ��������� ��� MeshFilter
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh = meshData.CreateMesh();
        meshFilter.mesh = mesh;
        return riverMeshObject;
    }

    private void UpdateRiver(MeshData meshData,GameObject riverMeshObject)
    {
        Debug.Log("Updating existing river...");

        MeshFilter meshFilter = riverMeshObject.GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            // ��������� mesh ������
            Mesh mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh = meshData.CreateMesh();
            meshFilter.mesh = mesh;
        }
        else
        {
            Debug.LogError("Failed to find MeshFilter on existing river.");
        }
    }
}
