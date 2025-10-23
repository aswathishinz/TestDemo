using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MiniMapClickMoverRobust : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Camera miniMapCamera;
    public Camera mainCamera;
    public RectTransform miniMapRect;

    [Header("Settings")]
    public bool smoothMove = true;
    public float moveSpeed = 10f;
    public float cameraHeight = -4f;
    public Vector3 cameraRotation = new Vector3(90f, 0f, 0f);

    [Header("Model Filter")]
    public LayerMask modelLayerMask;   
    public string modelTag = "";       

    [Header("Debug")]
    public bool verbose = true;

    private Vector3 targetPos;
    private bool moving;

    void Start()
    {
        if (miniMapRect == null) miniMapRect = GetComponent<RectTransform>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (smoothMove && moving)
        {
            mainCamera.transform.position = Vector3.MoveTowards(
                mainCamera.transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(mainCamera.transform.position, targetPos) < 0.05f)
                moving = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (miniMapCamera == null || miniMapRect == null) return;

       
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            miniMapRect, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        Vector2 rectSize = miniMapRect.rect.size;
        Vector2 normalized = new Vector2((localPoint.x / rectSize.x) + 0.5f, (localPoint.y / rectSize.y) + 0.5f);

       
        if (normalized.x < 0 || normalized.x > 1 || normalized.y < 0 || normalized.y > 1)
        {
            if (verbose) Debug.Log("[MiniMapClickMover] Clicked outside minimap.");
            return;
        }

        
        Ray ray = miniMapCamera.ViewportPointToRay(new Vector3(normalized.x, normalized.y, 0f));

       
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, modelLayerMask))
        {
            if (!string.IsNullOrEmpty(modelTag) && !hit.collider.CompareTag(modelTag))
            {
                if (verbose) Debug.Log("[MiniMapClickMover] Clicked non-matching tag.");
                return;
            }

            if (verbose) Debug.Log($"[MiniMapClickMover] ✅ Clicked model: {hit.collider.name} at {hit.point}");

            Vector3 moveTo = new Vector3(hit.point.x, cameraHeight, hit.point.z);

            if (!smoothMove)
            {
                mainCamera.transform.position = moveTo;
                moving = false;
            }
            else
            {
                targetPos = moveTo;
                moving = true;
            }

            mainCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        }
        else
        {
            if (verbose) Debug.Log("[MiniMapClickMover] Clicked on empty area, no model hit.");
        }
    }
}