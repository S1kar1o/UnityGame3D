using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiverMeshCreate : MonoBehaviour
{
    public static MeshData CreateMeshData(Vector2[] riverLineDown, Vector2[] riverLineTop, Vector2[] riverVertices, int riverLength, int riverWidth, float riverHeight)
    {
        
        int meshSimpleficationIncrument = 8;
        int verticesPerLineWidth = (riverWidth - 1) / meshSimpleficationIncrument + 1;
        int verticesPerLineLength = (riverLength - 1) / meshSimpleficationIncrument + 1;

        MeshData meshData = new MeshData(verticesPerLineLength, verticesPerLineWidth);
        int vertexIndex = 0;
        


        for (int x = 0; x < riverLength; x += meshSimpleficationIncrument)
        {
           

            for (int y = 0; y < riverWidth; y += meshSimpleficationIncrument)
            {
                float testH = riverHeight;
                


                meshData.vertices[vertexIndex] = new Vector3(riverVertices[x* riverWidth + y].x, riverHeight, riverVertices[x * riverWidth + y].y);

               
                meshData.uvs[vertexIndex] = new Vector2(x / (float)riverLength, y / (float)riverWidth);
                if (x < riverLength - 1 && y < riverWidth - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLineWidth + 1, vertexIndex + verticesPerLineWidth);
                    meshData.AddTriangle(vertexIndex + verticesPerLineWidth + 1, vertexIndex, vertexIndex + 1);
                }
                
                vertexIndex++;
            }
        }

        Debug.Log(meshData.vertices.Length);


        return meshData;
    }

}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    int triangleIndex;
    public Vector2[] uvs;

    public MeshData(int mesWidth, int meshHeight)
    {
        vertices = new Vector3[mesWidth * meshHeight];
        uvs = new Vector2[mesWidth * meshHeight];
        triangles = new int[(mesWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;




        triangleIndex += 3;

    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        Debug.Log(mesh.vertices.Length);
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
