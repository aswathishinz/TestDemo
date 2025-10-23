
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class SurfacePointSelector : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    [Tooltip("Which layers are treated as marker colliders (for clicking/dragging markers).")]
    public LayerMask markerLayerMask = ~0;

    [Tooltip("Which layers are valid surfaces for placing points (ONLY these layers allow placement).")]
    public LayerMask placementLayerMask = ~0;

    public GameObject markerPrefab;
    public Text infoText;

    [Header("Options")]
    public bool snapToNearestVertex = false;

    [Header("Line Settings")]
    public float lineWidth = 0.01f;
    public Material lineMaterial;
    public int lineSampleSegments = 40;
    public float raycastSearchDistance = 0.15f;
    public float surfaceOffset = 0.002f;

    [Header("Arc Settings")]
    public float arcRadius = 0.12f;
    [Range(4, 128)]
    public int arcSegments = 24;
    public Material arcMaterial;
    public Color arcColor = new Color(1f, 1f, 0f, 0.45f);

    [Header("Force Arc World Y (set to 0 to disable)")]
    [Tooltip("If non-zero, forces the arc's world Y position to this value.")]
    public float forcedArcWorldY = 0.51f;

    [Header("Angle Text Settings")]
    public bool showAngleText = true;
    public Color angleTextColor = Color.black;
    public int angleFontSize = 48;
    public float angleTextCharacterSize = 0.03f;
    public float angleLabelRadiusFactor = 0.55f;
    public Vector3 angleTextLocalOffset = Vector3.zero;
    [Tooltip("Small offset toward the camera so text sits in front of arc (world units)")]
    public float angleTextCameraOffset = 0.015f;
    [Tooltip("Vertical upward offset applied to the angle text (world units)")]
    public float angleTextVerticalOffset = 0.02f;

    [Header("Marker Settings")]
    public float markerScale = 0.03f;
    public float markerColliderRadius = 0.025f;

    private List<SelectedPoint> points = new List<SelectedPoint>(3);

    private LineRenderer lineBA;
    private LineRenderer lineBC;

    private GameObject arcGO;
    private MeshFilter arcMF;
    private MeshRenderer arcMR;

    private GameObject angleTextObj;
    private TextMesh angleTextMesh;
    private bool isDragging = false;
    private int draggingIndex = -1; 
    private Plane dragPlane; 
    private Vector3 dragOffsetLocal = Vector3.zero; 
    private float dragFixedY = 0f; 

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        CreateOrResetLines();
        CreateOrResetArc();
    }

    private void Update()
    {
        HandleInput();

        UpdateInfoText();
        UpdateLines();
        UpdateArc();
    }

    private void HandleInput()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
           
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

           
            if (Physics.Raycast(ray, out RaycastHit hitMarker, Mathf.Infinity, markerLayerMask))
            {
              
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].marker == null) continue;

                    Transform t = hitMarker.collider.transform;
                    while (t != null)
                    {
                        if (t.gameObject == points[i].marker)
                        {
                            StartDragging(i, hitMarker);
                            return; 
                        }
                        t = t.parent;
                    }
                }
            }

           
            if (points.Count < 3)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayerMask))
                {
                    Vector3 chosenPoint = hit.point;
                    Transform hitTransform = hit.transform;

                    if (snapToNearestVertex)
                    {
                        Mesh mesh = null;
                        MeshCollider meshCollider = hit.collider as MeshCollider;
                        if (meshCollider != null && meshCollider.sharedMesh != null)
                            mesh = meshCollider.sharedMesh;
                        else
                        {
                            var mf = hitTransform.GetComponent<MeshFilter>();
                            if (mf != null) mesh = mf.sharedMesh;
                        }

                        if (mesh != null)
                            chosenPoint = FindNearestVertexWorld(mesh, hitTransform, hit.point);
                    }

                    AddPoint(chosenPoint, hit.transform);
                }
                else
                {
                    
                }
            }
        }

       
        if (isDragging && draggingIndex >= 0 && draggingIndex < points.Count)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var marker = points[draggingIndex].marker;
            var parent = points[draggingIndex].parent;
            Vector3 newPos = marker.transform.position;

           
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayerMask))
            {
                newPos = hit.point;
            }
            else
            {
               
                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPointOnPlane = ray.GetPoint(enter);
                    newPos = hitPointOnPlane + marker.transform.TransformVector(dragOffsetLocal);
                }
            }

           
            newPos.y = dragFixedY;

           
            if (parent != null)
            {
                
                Bounds localBounds = GetLocalBounds(parent.gameObject);
                if (localBounds.size != Vector3.zero)
                {
                  
                    Vector3 localPos = parent.InverseTransformPoint(newPos);
                                       
                    localPos.x = Mathf.Clamp(localPos.x, localBounds.min.x, localBounds.max.x);
                    localPos.z = Mathf.Clamp(localPos.z, localBounds.min.z, localBounds.max.z);
                    localPos.y = Mathf.Clamp(localPos.y, localBounds.min.y, localBounds.max.y);
                                        
                    newPos = parent.TransformPoint(localPos);
                                       
                    newPos.y = dragFixedY;
                }
            }

            marker.transform.position = newPos;
        }

      
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDragging();
        }

      
        if (Input.GetKeyDown(KeyCode.C)) ClearPoints();
        if (Input.GetKeyDown(KeyCode.U)) UndoPoint();
        if (Input.GetKeyDown(KeyCode.V)) snapToNearestVertex = !snapToNearestVertex;
    }

    private void StartDragging(int index, RaycastHit hit)
    {
        isDragging = true;
        draggingIndex = index;

        Vector3 markerPos = points[index].marker.transform.position;

       
        dragFixedY = markerPos.y;

       
        dragPlane = new Plane(-cam.transform.forward, markerPos);

       
        Vector3 localHit = points[index].marker.transform.InverseTransformPoint(hit.point);
        dragOffsetLocal = localHit;
    }

    private void EndDragging()
    {
        isDragging = false;
        draggingIndex = -1;
    }

    private void AddPoint(Vector3 worldPos, Transform parent)
    {
        if (points.Count >= 3) return;

        GameObject marker;
        if (markerPrefab != null)
        {
            marker = Instantiate(markerPrefab, worldPos, Quaternion.identity, parent);
        }
        else
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.SetParent(parent, true);
            marker.transform.localScale = Vector3.one * markerScale;
        }

       
        Collider col = marker.GetComponent<Collider>();
        if (col == null)
        {
            var sc = marker.AddComponent<SphereCollider>();
            sc.radius = markerColliderRadius;
            col = sc;
        }
        col.isTrigger = false;
               
        var rb = marker.GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);

               int layer = GetFirstLayerFromMask(markerLayerMask);
        if (layer >= 0) SetLayerRecursively(marker, layer);

        marker.transform.position = worldPos;

        points.Add(new SelectedPoint
        {
            marker = marker,
            parent = parent
        });

        if (points.Count == 3)
        {
            EnableLines(true);
            if (arcGO != null) arcGO.SetActive(true);
            UpdateLines();
            UpdateArc();
        }
    }

       private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

   
    private int GetFirstLayerFromMask(LayerMask mask)
    {
        int maskVal = mask.value;
        for (int i = 0; i < 32; i++)
            if ((maskVal & (1 << i)) != 0) return i;
        return -1;
    }

    private void UndoPoint()
    {
        if (points.Count == 0) return;
        var last = points[points.Count - 1];
        if (last.marker != null) Destroy(last.marker);
        points.RemoveAt(points.Count - 1);

        if (points.Count < 3)
        {
            EnableLines(false);
            if (arcGO != null) arcGO.SetActive(false);
            if (angleTextObj != null) angleTextObj.SetActive(false);
        }
    }

    private void ClearPoints()
    {
        foreach (var p in points)
            if (p.marker != null) Destroy(p.marker);
        points.Clear();
        EnableLines(false);
        if (arcGO != null) arcGO.SetActive(false);
        if (angleTextObj != null) angleTextObj.SetActive(false);
    }

    private void UpdateInfoText()
    {
        if (infoText == null) return;
        if (points.Count == 0) { infoText.text = "Click 3 points (U=Undo, C=Clear, V=Snap)"; return; }

        string s = $"Points: {points.Count}/3\n";
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 wp = points[i].marker != null ? points[i].marker.transform.position : Vector3.zero;
            s += $"{i + 1}: {wp:F3}\n";
        }
        s += $"Snap: {(snapToNearestVertex ? "ON" : "OFF")}";
        infoText.text = s;
    }

    private Vector3 FindNearestVertexWorld(Mesh mesh, Transform meshTransform, Vector3 worldHitPoint)
    {
        Vector3 nearest = worldHitPoint;
        float bestDist = float.MaxValue;
        foreach (var v in mesh.vertices)
        {
            Vector3 vWorld = meshTransform.TransformPoint(v);
            float d = Vector3.Distance(vWorld, worldHitPoint);
            if (d < bestDist) { bestDist = d; nearest = vWorld; }
        }
        return nearest;
    }

    #region Lines
    private void CreateOrResetLines()
    {
        if (lineBA == null)
        {
            GameObject go = new GameObject("Line_B_to_A"); go.transform.SetParent(transform, false);
            lineBA = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineBA);
        }
        if (lineBC == null)
        {
            GameObject go = new GameObject("Line_B_to_C"); go.transform.SetParent(transform, false);
            lineBC = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineBC);
        }
        EnableLines(false);
    }

    private void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.loop = false;
        lr.widthCurve = AnimationCurve.Constant(0, 1, lineWidth);
        lr.useWorldSpace = true;
        lr.material = lineMaterial ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.numCapVertices = 2;
    }

    private void EnableLines(bool enabled)
    {
        if (lineBA != null) lineBA.enabled = enabled;
        if (lineBC != null) lineBC.enabled = enabled;
    }

    private void UpdateLines()
    {
        if (points.Count < 3) { EnableLines(false); return; }

        Vector3 posA = points[0].marker.transform.position;
        Vector3 posB = points[1].marker.transform.position;
        Vector3 posC = points[2].marker.transform.position;

        EnableLines(true);
        DrawSurfaceLine(lineBA, posB, posA);
        DrawSurfaceLine(lineBC, posB, posC);
    }

    private void DrawSurfaceLine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        if (lr == null) return;
        int segments = Mathf.Max(2, lineSampleSegments);
        Vector3[] pts = new Vector3[segments];

        Vector3 dir = (end - start);
        float fullDist = dir.magnitude;
        if (fullDist <= Mathf.Epsilon)
        {
            for (int i = 0; i < segments; i++) pts[i] = start;
            lr.positionCount = segments; lr.SetPositions(pts); return;
        }

        Vector3 step = dir / (segments - 1);
        Vector3 segDirNorm = dir.normalized;

        for (int i = 0; i < segments; i++)
        {
            Vector3 sample = start + step * i;
            RaycastHit hit; bool found = false;

            if (Physics.Raycast(sample + segDirNorm * 0.001f, -segDirNorm, out hit, raycastSearchDistance, placementLayerMask))
            { pts[i] = hit.point + hit.normal * surfaceOffset; found = true; }
            else if (Physics.Raycast(sample - segDirNorm * 0.001f, segDirNorm, out hit, raycastSearchDistance, placementLayerMask))
            { pts[i] = hit.point + hit.normal * surfaceOffset; found = true; }
            else
            {
                if (Physics.SphereCast(sample, raycastSearchDistance * 0.25f, segDirNorm, out hit, raycastSearchDistance * 0.5f, placementLayerMask))
                { pts[i] = hit.point + hit.normal * surfaceOffset; found = true; }
            }

            if (!found) pts[i] = sample;
        }

        lr.positionCount = segments;
        lr.SetPositions(pts);
    }
    #endregion

    #region Arc + Angle text (WORLD-space verts -> convert to local)
    private void CreateOrResetArc()
    {
        if (arcGO == null)
        {
            arcGO = new GameObject("AngleArc_B");
            arcMF = arcGO.AddComponent<MeshFilter>();
            arcMR = arcGO.AddComponent<MeshRenderer>();
            arcMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arcMR.receiveShadows = false;
        }

        if (arcMaterial != null)
            arcMR.sharedMaterial = new Material(arcMaterial);
        else
        {
            var mat = new Material(Shader.Find("Unlit/Color")); mat.color = arcColor;
            arcMR.sharedMaterial = mat;
        }

#if UNITY_2019_1_OR_NEWER
        arcMR.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
#else
        arcMR.sharedMaterial.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);
#endif
        arcMR.sharedMaterial.SetInt("_ZWrite", 0);
        arcMR.sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (angleTextObj == null)
        {
            angleTextObj = new GameObject("AngleText");
            angleTextMesh = angleTextObj.AddComponent<TextMesh>();
            angleTextMesh.anchor = TextAnchor.MiddleCenter;
            angleTextMesh.alignment = TextAlignment.Center;
            angleTextMesh.fontSize = angleFontSize;
            angleTextMesh.characterSize = angleTextCharacterSize;
            angleTextMesh.color = angleTextColor;
            var billboard = angleTextObj.AddComponent<FaceCameraBillboardFull>();
            billboard.cam = cam;
        }

        arcGO.SetActive(false);
        angleTextObj.SetActive(false);
    }

    private void UpdateArc()
    {
        if (arcGO == null || points.Count < 3)
        {
            if (arcGO != null) arcGO.SetActive(false);
            if (angleTextObj != null) angleTextObj.SetActive(false);
            return;
        }

        Vector3 posA = points[0].marker.transform.position;
        Vector3 posB = points[1].marker.transform.position;
        Vector3 posC = points[2].marker.transform.position;

        Vector3 vBA = posA - posB;
        Vector3 vBC = posC - posB;

        if (vBA.sqrMagnitude < 1e-6f || vBC.sqrMagnitude < 1e-6f)
        {
            arcGO.SetActive(false); angleTextObj.SetActive(false); return;
        }

      
        Vector3 planeNormal = Vector3.Cross(vBA, vBC);
        if (planeNormal.sqrMagnitude < 1e-6f) { arcGO.SetActive(false); angleTextObj.SetActive(false); return; }
        planeNormal.Normalize();

        Vector3 xAxis = vBA.normalized;
        xAxis = (xAxis - Vector3.Dot(xAxis, planeNormal) * planeNormal).normalized; // project to plane
        Vector3 yAxis = Vector3.Cross(planeNormal, xAxis).normalized;

       
        float angleA = Mathf.Atan2(Vector3.Dot(yAxis, vBA.normalized), Vector3.Dot(xAxis, vBA.normalized));
        float angleC = Mathf.Atan2(Vector3.Dot(yAxis, vBC.normalized), Vector3.Dot(xAxis, vBC.normalized));
        angleA = NormalizeAngleRad(angleA);
        angleC = NormalizeAngleRad(angleC);

        float delta = Mathf.DeltaAngle(angleA * Mathf.Rad2Deg, angleC * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float arcAngle = Mathf.Abs(delta);
        float startAngle = angleA;
        float sign = Mathf.Sign(delta);

        int segs = Mathf.Max(3, arcSegments);
        int vcount = segs + 2;

       
        Vector3[] vertsWorld = new Vector3[vcount];
        Vector3[] vertsLocal = new Vector3[vcount];
        Vector3[] normsLocal = new Vector3[vcount];
        Vector2[] uvs = new Vector2[vcount];
        int[] tris = new int[(vcount - 2) * 3];

       
        Vector3 centerWorld = posB + planeNormal * surfaceOffset;

        
        if (!Mathf.Approximately(forcedArcWorldY, 0f))
            centerWorld.y = forcedArcWorldY;

        vertsWorld[0] = centerWorld;
        uvs[0] = Vector2.zero;

        for (int i = 0; i <= segs; i++)
        {
            float t = (float)i / (float)segs;
            float ang = startAngle + sign * t * arcAngle;
            Vector3 dir = (Mathf.Cos(ang) * xAxis + Mathf.Sin(ang) * yAxis).normalized;
            Vector3 pWorld = centerWorld + dir * arcRadius;
            vertsWorld[i + 1] = pWorld;
            uvs[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Cos(ang), 0.5f + 0.5f * Mathf.Sin(ang));
        }

       
        Transform parentForArc = points[1].parent != null ? points[1].parent : null;
        if (parentForArc != null) arcGO.transform.SetParent(parentForArc, true);
        else arcGO.transform.SetParent(null, true);

      
        arcGO.transform.position = centerWorld;
        arcGO.transform.rotation = Quaternion.identity;

        Vector3 localNormal = arcGO.transform.InverseTransformDirection(planeNormal.normalized);

        for (int i = 0; i < vcount; i++)
        {
            vertsLocal[i] = vertsWorld[i] - centerWorld;
            normsLocal[i] = localNormal;
        }

        for (int i = 0; i < vcount - 2; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        Mesh m = arcMF.sharedMesh;
        if (m == null) m = new Mesh();
        else m.Clear();

        m.vertices = vertsLocal;
        m.triangles = tris;
        m.normals = normsLocal;
        m.uv = uvs;
        m.RecalculateBounds();
        arcMF.sharedMesh = m;
        arcGO.SetActive(true);

       
        if (showAngleText && angleTextObj != null)
        {
            float midAngle = startAngle + sign * (arcAngle * 0.5f);
            Vector3 bisectorDir = (Mathf.Cos(midAngle) * xAxis + Mathf.Sin(midAngle) * yAxis).normalized;
            Vector3 labelWorldPos = centerWorld + bisectorDir * (arcRadius * Mathf.Clamp01(angleLabelRadiusFactor)) + angleTextLocalOffset;

           
            labelWorldPos += Vector3.up * angleTextVerticalOffset;

         
            if (cam != null)
            {
                Vector3 toCam = (cam.transform.position - labelWorldPos);
                if (toCam.sqrMagnitude > 0.000001f)
                {
                    labelWorldPos += toCam.normalized * angleTextCameraOffset;
                }
            }

            
            angleTextObj.transform.SetParent(arcGO.transform, true);
            angleTextObj.transform.position = labelWorldPos;

            float angleDeg = arcAngle * Mathf.Rad2Deg;
            angleTextMesh.text = $"{angleDeg:F1}°";
            angleTextMesh.fontSize = angleFontSize;
            angleTextMesh.characterSize = angleTextCharacterSize;
            angleTextMesh.color = angleTextColor;

            angleTextObj.SetActive(true);
        }
        else if (angleTextObj != null)
        {
            angleTextObj.SetActive(false);
        }
    }
    #endregion

    
    private Bounds GetLocalBounds(GameObject obj)
    {
        Bounds worldBounds = new Bounds();
        bool found = false;

        var renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (!found) { worldBounds = r.bounds; found = true; }
            else worldBounds.Encapsulate(r.bounds);
        }

        var colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            if (!found) { worldBounds = c.bounds; found = true; }
            else worldBounds.Encapsulate(c.bounds);
        }

        if (!found) return new Bounds(Vector3.zero, Vector3.zero);

       
        Vector3 localMin = obj.transform.InverseTransformPoint(worldBounds.min);
        Vector3 localMax = obj.transform.InverseTransformPoint(worldBounds.max);

        Vector3 localCenter = (localMin + localMax) * 0.5f;
        Vector3 localSize = new Vector3(
            Mathf.Abs(localMax.x - localMin.x),
            Mathf.Abs(localMax.y - localMin.y),
            Mathf.Abs(localMax.z - localMin.z)
        );

        return new Bounds(localCenter, localSize);
    }

    private static float NormalizeAngleRad(float a)
    {
        a %= (Mathf.PI * 2f);
        if (a < 0) a += Mathf.PI * 2f;
        return a;
    }

    private struct SelectedPoint
    {
        public GameObject marker;
        public Transform parent;
    }



}

public class FaceCameraBillboardFull : MonoBehaviour
{
    public Camera cam;
    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }





}
