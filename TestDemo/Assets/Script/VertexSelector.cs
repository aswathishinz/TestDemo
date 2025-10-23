using UnityEngine;
using System.Collections.Generic;

public class VertexSelector : MonoBehaviour
{
    [Tooltip("Prefab of the small sphere to place on vertices")]
    public GameObject spherePrefab;

    [Tooltip("Scale of each vertex sphere")]
    public float sphereScale = 0.02f;

    private List<Vector3> selectedPoints = new List<Vector3>();

    void Start()
    {
        if (spherePrefab == null)
        {
            spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.transform.localScale = Vector3.one * sphereScale;
            spherePrefab.GetComponent<Collider>().enabled = false;
        }

        // Get all child MeshFilters
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = mf.transform.TransformPoint(vertices[i]);
                GameObject sphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                sphere.transform.localScale = Vector3.one * sphereScale;
                sphere.transform.SetParent(mf.transform);
                sphere.name = $"VertexSphere_{i}";
                              
                SphereClickHandler handler = sphere.AddComponent<SphereClickHandler>();
                handler.Setup(this, worldPos);
            }
        }
    }

    // Called by SphereClickHandler
    public void RegisterVertex(Vector3 position)
    {
        selectedPoints.Add(position);

        if (selectedPoints.Count == 3)
        {
            CalculateAngle();
            selectedPoints.Clear(); 
        }
    }

    private void CalculateAngle()
    {
        Vector3 A = selectedPoints[0];
        Vector3 B = selectedPoints[1];
        Vector3 C = selectedPoints[2];

        Vector3 AB = (A - B).normalized;
        Vector3 CB = (C - B).normalized;

        float angle = Vector3.Angle(AB, CB);

        Debug.Log($"Angle between points at B: {angle} degrees");
    }
}
