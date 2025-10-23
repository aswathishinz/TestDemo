using UnityEngine;

public class MouseZoomOnObject : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float smoothTime = 0.06f;
    public float minDistance = 0.5f;
    public float maxDistance = 30f;

    [Header("Collision Settings")]
    public LayerMask planeLayer;      
    public LayerMask obstacleLayers;  

    [Header("Camera Settings")]
    public float fixedDepth = 10f;

    [Header("Pan Settings")]
    public float panSpeed = 0.1f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    private Vector3 lastMousePosition;
    private bool isPanning = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandlePanning();
    }

    void LateUpdate()
    {
        HandleZoom();
    }

    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(1)) 
        {
            lastMousePosition = Input.mousePosition;
            isPanning = true;
        }

        if (Input.GetMouseButtonUp(1)) 
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

           
            Vector3 move = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0);

            
            transform.Translate(move, Space.Self);

            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        Vector3 camPos = transform.position;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = fixedDepth;

        Vector3 targetPoint = cam.ScreenToWorldPoint(mousePos);

        float currentDistance = Vector3.Distance(camPos, targetPoint);
        float desiredDistance = currentDistance - scroll * zoomSpeed;
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

        Vector3 dir = (camPos - targetPoint).normalized;
        Vector3 candidatePos = targetPoint + dir * desiredDistance;

        float zoomStepDistance = Mathf.Abs(scroll) * zoomSpeed;

        Vector3 zoomDirection = (targetPoint - camPos).normalized;

        if (scroll > 0)
        {
            Ray forwardRay = new Ray(camPos, zoomDirection);
            if (Physics.Raycast(forwardRay, out RaycastHit hitInfo, zoomStepDistance, planeLayer))
            {
                
                return;
            }
        }
        else if (scroll < 0)
        {
            Ray backwardRay = new Ray(camPos, -zoomDirection);
            if (Physics.Raycast(backwardRay, out RaycastHit hitInfo, zoomStepDistance, obstacleLayers))
            {
                
                return;
            }
        }

        if (smoothTime > 0f)
            transform.position = Vector3.SmoothDamp(camPos, candidatePos, ref velocity, smoothTime);
        else
            transform.position = candidatePos;
    }
}
