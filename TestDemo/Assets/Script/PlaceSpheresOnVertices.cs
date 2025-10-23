using UnityEngine;

public class PlaceSpheresOnVertices : MonoBehaviour
{
    [Tooltip("Prefab of the small sphere to place on vertices")]
    public GameObject spherePrefab;

    [Tooltip("Scale of each vertex sphere")]
    public float sphereScale = 0.02f;

    private VertexAngleSelector selector;

    void Start()
    {
        selector = FindObjectOfType<VertexAngleSelector>();
        if (selector == null)
        {
            Debug.LogError("VertexAngleSelector not found in the scene.");
            return;
        }

        if (spherePrefab == null)
        {
            spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.transform.localScale = Vector3.one * sphereScale;
            spherePrefab.GetComponent<Collider>().enabled = false;
        }

               MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;

            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldPos = mf.transform.TransformPoint(vertex);

                GameObject sphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                sphere.transform.localScale = Vector3.one * sphereScale;
                sphere.transform.SetParent(mf.transform);

                
                if (sphere.GetComponent<Collider>() == null)
                    sphere.AddComponent<SphereCollider>();

               
                var clickHandler = sphere.AddComponent<VertexClickable>();
                clickHandler.Setup(selector, worldPos);

               
                if (sphere.GetComponent<Renderer>().material == null)
                    sphere.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
            }
        }
    }
}
