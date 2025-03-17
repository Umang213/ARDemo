using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoomPlanMeshGenerator : MonoBehaviour
{
    public ObjectSpawner m_ObjectSpawner;
    public List<Vector3> roomPoints = new List<Vector3>(); // Detected points list
    private Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        GenerateMesh();
    }

    void OnDestroy()
    {
        m_ObjectSpawner.objectSpawned -= OnObjectSpawned;
    }

    void OnObjectSpawned(GameObject spawnedObject)
    {
        roomPoints.Add(spawnedObject.transform.position);
    }

    public void GenerateMesh()
    {
        if (roomPoints.Count < 4)  // Minimum 4 points for 3D mesh
        {
            Debug.LogWarning("Not enough points to generate a 3D mesh.");
            return;
        }

        // Generate 3D Convex Hull
        List<int> triangles = QuickHull.GenerateConvexHull(roomPoints);

        // Assign data to mesh
        mesh.Clear();
        mesh.vertices = roomPoints.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}
