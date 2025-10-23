using UnityEngine;

public class SphereClickHandler : MonoBehaviour
{
    private VertexSelector selector;
    private Vector3 position;

    public void Setup(VertexSelector selector, Vector3 position)
    {
        this.selector = selector;
        this.position = position;
        Collider col = GetComponent<Collider>();
        if (col == null)
            gameObject.AddComponent<SphereCollider>();
        else
            col.enabled = true;
    }

    void OnMouseDown()
    {
        selector.RegisterVertex(position);
    }
}
