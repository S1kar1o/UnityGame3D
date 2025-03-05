/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation; // ВАЖНО!

public class BridgeNavMeshUpdater : MonoBehaviour
{
    [SerializeField] private GameObject bridgePrefab;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private List<GameObject> bridges = new List<GameObject>();

    public void BuildBridge(Vector3 position)
    {
        GameObject newBridge = Instantiate(bridgePrefab, position, Quaternion.identity);
        bridges.Add(newBridge);

        UpdateNavMeshForBridge(newBridge);
    }

    private void UpdateNavMeshForBridge(GameObject bridge)
    {
        MeshFilter meshFilter = bridge.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Bridge mesh not found!");
            return;
        }

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        navMeshSurface.CollectSources(ref sources); // Теперь должно работать!

        NavMeshBuildSource bridgeSource = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = meshFilter.mesh,
            transform = bridge.transform.localToWorldMatrix,
            area = 0
        };
        sources.Add(bridgeSource);

        Bounds updateBounds = new Bounds(bridge.transform.position, Vector3.one * 15);
        NavMeshBuilder.UpdateNavMeshDataAsync(navMeshSurface.navMeshData, NavMesh.GetSettingsByID(0), sources, updateBounds);
    }
}
*/