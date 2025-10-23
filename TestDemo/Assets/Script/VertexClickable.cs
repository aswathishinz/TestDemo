using UnityEngine;

public class VertexClickable : MonoBehaviour
{
    private VertexAngleSelector selector;
    private Vector3 position;
      
    public void Setup(VertexAngleSelector selector, Vector3 position)
    {
        this.selector = selector;
        this.position = position;
    }

    void OnMouseDown()
    {
        selector.RegisterSelection(gameObject, position);
    }
}
