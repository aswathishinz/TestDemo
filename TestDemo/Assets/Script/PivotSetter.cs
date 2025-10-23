using UnityEngine;


public class PivotSetterWithMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    public GameObject pivotMarkerPrefab;
    public float defaultMarkerScale = 0.05f;
    public bool keepMarkerWorldRotation = true;
    public bool scaleMarkerWithModel = false;

    [Header("Raycast / Layers")]
    public LayerMask interactableLayers = -1;
    public float maxRayDistance = 1000f;

    [Header("Rotation Feel")]
    public float yawSensitivity = 0.6f;
    public float pitchSensitivity = 0.6f;
    [Range(0f, 1f)]
    public float smoothing = 0.12f;
    public bool inertiaEnabled = true;
    [Range(1f, 10f)]
    public float inertiaDamping = 4f;
    public float deadZone = 1.5f;
    public float maxInertiaSpeed = 720f;
    public bool naturalDragDirection = true;
    public bool rotateOnlyY = true;

    [Header("Pitch Limits")]
    public bool clampPitch = true;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool invertY = false;

    private Camera mainCamera;
    private GameObject currentMarker;
    private GameObject pivotHolder;
    private Transform originalParent;

   
    private bool pointerDownOnModel = false; 
    private bool isDragging = false;
    private Vector2 lastPointerPos;

   
    private float targetYaw;
    private float currentYaw;
    private float targetPitch;
    private float currentPitch;
    private Vector2 angularVelocity = Vector2.zero; 

   
    private bool pivotJustSet = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cam = FindObjectOfType<Camera>();
            if (cam != null) mainCamera = cam;
        }
    }

    void OnDisable()
    {
        ResetPivot(false);
    }

    void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        HandlePointerInput();

        
        pivotJustSet = false;

        ApplySmoothingAndInertia();
    }

    void HandlePointerInput()
    {
       
        if (Input.mousePresent)
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastPointerPos = Input.mousePosition;
                
                pointerDownOnModel = HandlePressAt(Input.mousePosition);
                
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - lastPointerPos;
                               
                if (!isDragging && pointerDownOnModel && delta.magnitude >= deadZone)
                {
                    isDragging = true;
                    angularVelocity = Vector2.zero;
                    lastPointerPos = cur;
                }

                if (isDragging)
                {
                    
                    if (!pivotJustSet)
                    {
                        OnDrag(delta);
                    }
                    lastPointerPos = cur;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndTouchOrClick();
            }
        }

        
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                lastPointerPos = t.position;
                pointerDownOnModel = HandlePressAt(t.position);
                
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                Vector2 cur = t.position;
                Vector2 delta = cur - lastPointerPos;

                if (!isDragging && pointerDownOnModel && delta.magnitude >= deadZone)
                {
                    isDragging = true;
                    angularVelocity = Vector2.zero;
                    lastPointerPos = cur;
                }

                if (isDragging)
                {
                    if (!pivotJustSet)
                    {
                        OnDrag(delta);
                    }
                    lastPointerPos = cur;
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                EndTouchOrClick();
            }
        }
    }

   
    private bool HandlePressAt(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
               
                SetPivotPoint(hit.point, screenPos);
               
                angularVelocity = Vector2.zero;
                return true;
            }
            else
            {
                
                StopRotation();
                return false;
            }
        }

       
        StopRotation();
        return false;
    }

  
    void SetPivotPoint(Vector3 newPivot, Vector2? inputScreenPos = null)
    {
       
        if (pivotHolder != null)
        {
            transform.SetParent(originalParent, worldPositionStays: true);
            Destroy(pivotHolder);
            pivotHolder = null;
        }

         pivotHolder = new GameObject(gameObject.name + "_PivotHolder");
        pivotHolder.transform.position = newPivot;
        var modelWorldRot = transform.rotation.eulerAngles;
        pivotHolder.transform.rotation = Quaternion.Euler(modelWorldRot.x, modelWorldRot.y, 0f);

        originalParent = transform.parent;
        if (originalParent != null)
            pivotHolder.transform.SetParent(originalParent, worldPositionStays: true);

      
        transform.SetParent(pivotHolder.transform, worldPositionStays: true);

        
        currentYaw = targetYaw = pivotHolder.transform.eulerAngles.y;
        currentPitch = targetPitch = pivotHolder.transform.eulerAngles.x;
        angularVelocity = Vector2.zero;

        isDragging = false;
        pointerDownOnModel = false;

        if (inputScreenPos.HasValue)
        {
            lastPointerPos = inputScreenPos.Value;
        }

         pivotJustSet = true;

       
        if (currentMarker == null)
        {
            if (pivotMarkerPrefab != null)
                currentMarker = Instantiate(pivotMarkerPrefab, newPivot, Quaternion.identity);
            else
            {
                currentMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentMarker.transform.localScale = Vector3.one * defaultMarkerScale;
                var r = currentMarker.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(r.sharedMaterial);
                    r.material.color = Color.yellow;
                }
                var col = currentMarker.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }

        currentMarker.transform.position = newPivot;
        if (keepMarkerWorldRotation)
            currentMarker.transform.rotation = Quaternion.identity;
        else
            currentMarker.transform.rotation = pivotHolder.transform.rotation;

        if (scaleMarkerWithModel)
            currentMarker.transform.localScale = Vector3.one * defaultMarkerScale * transform.lossyScale.magnitude;

        currentMarker.transform.SetParent(pivotHolder.transform, worldPositionStays: true);

        Debug.Log($"New pivot set at: {newPivot}");
    }

    void OnDrag(Vector2 pointerDelta)
    {
        if (pivotHolder == null) return;

       
        if (pointerDelta.magnitude < Mathf.Epsilon) return;

        float dir = naturalDragDirection ? 1f : -1f;

        float deltaYaw = pointerDelta.x * yawSensitivity * dir;
        float invert = invertY ? -1f : 1f;
        float deltaPitch = pointerDelta.y * pitchSensitivity * dir * invert * -1f;

        if (rotateOnlyY) deltaPitch = 0f;

        targetYaw += deltaYaw;
        targetPitch += deltaPitch;

        if (clampPitch)
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        if (Time.deltaTime > 0f)
        {
            float estYaw = deltaYaw / Time.deltaTime;
            float estPitch = deltaPitch / Time.deltaTime;
            angularVelocity.x = Mathf.Clamp(estYaw, -maxInertiaSpeed, maxInertiaSpeed);
            angularVelocity.y = Mathf.Clamp(estPitch, -maxInertiaSpeed, maxInertiaSpeed);
        }
    }

    void EndTouchOrClick()
    {
        pointerDownOnModel = false;
        isDragging = false;

        if (!inertiaEnabled)
            angularVelocity = Vector2.zero;
    }

    void ApplySmoothingAndInertia()
    {
        if (pivotHolder == null) return;

        
        if (!isDragging && inertiaEnabled)
        {
            if (angularVelocity.sqrMagnitude > 0.0001f)
            {
                targetYaw += angularVelocity.x * Time.deltaTime;
                targetPitch += angularVelocity.y * Time.deltaTime;

                float decay = 1f - Mathf.Clamp01(inertiaDamping * Time.deltaTime * 0.5f);
                angularVelocity *= decay;

                if (angularVelocity.magnitude < 0.5f) angularVelocity = Vector2.zero;
            }
        }

        if (clampPitch)
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float t = 1f - Mathf.Pow(smoothing, Time.deltaTime * 60f);
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, t);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, t);

        if (rotateOnlyY)
        {
            Vector3 e = pivotHolder.transform.eulerAngles;
            e.y = currentYaw;
            pivotHolder.transform.eulerAngles = e;
        }
        else
        {
            pivotHolder.transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }
    }

    public void StopRotation()
    {
        pointerDownOnModel = false;
        isDragging = false;
        angularVelocity = Vector2.zero;
        targetYaw = currentYaw;
        targetPitch = currentPitch;
    }

    public void ResetPivot(bool removeMarker = false)
    {
        pointerDownOnModel = false;
        isDragging = false;
        angularVelocity = Vector2.zero;

        if (pivotHolder != null)
        {
           
           transform.SetParent(originalParent, worldPositionStays: true);
            Destroy(pivotHolder);
            pivotHolder = null;
        }

        if (removeMarker && currentMarker != null)
        {
            Destroy(currentMarker);
            currentMarker = null;
        }
    }

    void OnValidate()
    {
        yawSensitivity = Mathf.Max(0.0001f, yawSensitivity);
        pitchSensitivity = Mathf.Max(0.0001f, pitchSensitivity);
        smoothing = Mathf.Clamp01(smoothing);
        inertiaDamping = Mathf.Max(1f, inertiaDamping);
        deadZone = Mathf.Max(0f, deadZone);
        maxInertiaSpeed = Mathf.Max(1f, maxInertiaSpeed);

        if (clampPitch && minPitch > maxPitch)
            minPitch = maxPitch - 1f;
    }
}
