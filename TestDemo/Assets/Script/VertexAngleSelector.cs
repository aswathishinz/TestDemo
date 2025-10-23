using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class VertexAngleSelector : MonoBehaviour
{
    public float selectedSphereScale = 0.03f;
    public float lineWidth = 0.005f;
    public Material lineMaterial;
    public TextMeshPro textMeshProFont;  
    public float arcRadius = 0.05f;
    public int arcSegments = 30;

    [Header("UI Elements")]
    public TextMeshProUGUI Instruction, Instruction2, Instruction3;
    public Button button1;
    public Button button2;
    public Button button3;
    public Button doneButton;
    public Button Reset_second_terminal_point;

    private List<GameObject> selectedSpheres = new List<GameObject>();
    private List<Vector3> selectedPositions = new List<Vector3>();

    private GameObject angleTextObj;
    private GameObject arcObj;
    private GameObject arcOutlineObj;
    private List<GameObject> lineObjects = new List<GameObject>();

    private enum SelectionMode
    {
        None,
        SelectFirstVertex,
        SelectSecondVertex,
        SelectThirdVertex
    }

    private SelectionMode currentMode = SelectionMode.None;

    void Start()
    {
        if (button1) button1.onClick.AddListener(OnButton1Clicked);
        if (button2) button2.onClick.AddListener(OnButton2Clicked);
        if (button3) button3.onClick.AddListener(OnButton3Clicked);
        if (Reset_second_terminal_point != null)
        {
            Reset_second_terminal_point.onClick.AddListener(OnResetSecondTerminalPointClicked);
            Reset_second_terminal_point.gameObject.SetActive(false);
        }
        if (doneButton != null)
        {
            doneButton.gameObject.SetActive(false);
            doneButton.interactable = false;
        }

        if (button1) button1.interactable = true;
        if (button2) button2.interactable = false;
        if (button3) button3.interactable = false;
    }

    void OnButton1Clicked()
    {
        currentMode = SelectionMode.SelectFirstVertex;
        button1.interactable = false;
    }

    void OnButton2Clicked()
    {
        currentMode = SelectionMode.SelectSecondVertex;
        button2.interactable = false;
    }

    void OnButton3Clicked()
    {
        currentMode = SelectionMode.SelectThirdVertex;
        button3.interactable = false;
    }

    public void RegisterSelection(GameObject sphere, Vector3 position)
    {
        if (selectedSpheres.Contains(sphere)) return;

        switch (currentMode)
        {
            case SelectionMode.SelectFirstVertex:
                if (selectedPositions.Count == 0)
                {
                    AddSelection(sphere, position);
                    currentMode = SelectionMode.None;

                    if (button1) button1.GetComponent<Image>().color = Color.green;
                    if (Instruction) Instruction.gameObject.SetActive(false);

                    if (button2)
                    {
                        button2.gameObject.SetActive(true);
                        button2.interactable = true;
                    }
                }
                break;

            case SelectionMode.SelectSecondVertex:
                if (selectedPositions.Count == 1)
                {
                    AddSelection(sphere, position);
                    currentMode = SelectionMode.None;

                    if (Instruction2) Instruction2.gameObject.SetActive(false);
                    if (button2) button2.gameObject.SetActive(false);
                    if (button3)
                    {
                        button3.gameObject.SetActive(true);
                        button3.interactable = true;
                    }
                }
                break;

            case SelectionMode.SelectThirdVertex:
                if (selectedPositions.Count == 2)
                {
                    AddSelection(sphere, position);
                    currentMode = SelectionMode.None;
                    OnThirdVertexSelected();
                }
                break;
        }
    }

    private void AddSelection(GameObject sphere, Vector3 position)
    {
        selectedSpheres.Add(sphere);
        selectedPositions.Add(position);

        sphere.transform.localScale = Vector3.one * selectedSphereScale;
        sphere.GetComponent<Renderer>().material.color = Color.green;
    }

    private void OnThirdVertexSelected()
    {
        ClearPreviousVisuals();
        DrawAngleLines();
       DrawFilledArc();
       // DrawArcOutline();
        CalculateAndDisplayAngle();

        if (button1) button1.interactable = true;

        if (doneButton)
        {
            doneButton.gameObject.SetActive(true);
            button3.gameObject.SetActive(false);
            button1.gameObject.SetActive(false);
            if (Instruction3) Instruction3.gameObject.SetActive(false);
            doneButton.interactable = true;
        }

        if (Reset_second_terminal_point)
        {
            Reset_second_terminal_point.gameObject.SetActive(true);
            Reset_second_terminal_point.interactable = true;
        }
    }

    private void OnResetSecondTerminalPointClicked()
    {
        if (selectedPositions.Count < 3) return;

        var lastSphere = selectedSpheres[selectedSpheres.Count - 1];
        if (lastSphere != null)
        {
            lastSphere.transform.localScale = Vector3.one * 0.02f;
            lastSphere.GetComponent<Renderer>().material.color = Color.white;
        }

        selectedSpheres.RemoveAt(selectedSpheres.Count - 1);
        selectedPositions.RemoveAt(selectedPositions.Count - 1);

        ClearPreviousVisuals();

        if (button3)
        {
            button3.gameObject.SetActive(true);
            button3.interactable = true;
        }

        if (Reset_second_terminal_point)
            Reset_second_terminal_point.gameObject.SetActive(false);

        if (doneButton)
        {
            doneButton.gameObject.SetActive(false);
            doneButton.interactable = false;
        }
    }

    private void CalculateAndDisplayAngle()
    {
        Vector3 A = selectedPositions[0];
        Vector3 B = selectedPositions[1];
        Vector3 C = selectedPositions[2];

        Vector3 AB = (A - B).normalized;
        Vector3 CB = (C - B).normalized;

        float angle = Vector3.Angle(AB, CB);
         Vector3 bisector = (AB + CB).normalized;
        float textDistance = 0.06f; 
        Vector3 textPos = B + bisector * textDistance + Vector3.up * 0.1f; 
        textPos.z = -0.232f;
        if (angleTextObj != null) Destroy(angleTextObj);
        angleTextObj = CreateTextMeshPro($"{angle:F1}°", textPos);
        angleTextObj.transform.localScale = new Vector3(.1f, .1f, .1f);
    }

    private void DrawAngleLines()
    {
        Vector3 A = selectedPositions[0];
        Vector3 B = selectedPositions[1];
        Vector3 C = selectedPositions[2];

        lineObjects.Add(CreateLine(B, A, Color.red, "Line_BA"));
        lineObjects.Add(CreateLine(B, C, Color.red, "Line_BC"));
    }
            private void DrawFilledArc()
    {
        Vector3 A = selectedPositions[0];
        Vector3 B = selectedPositions[1];
        Vector3 C = selectedPositions[2];

        Vector3 dirA = (A - B).normalized;
        Vector3 dirC = (C - B).normalized;
        Vector3 normal = Vector3.Cross(dirA, dirC).normalized;
        float angleBetween = Vector3.Angle(dirA, dirC);

        if (arcObj != null) Destroy(arcObj);

        arcObj = new GameObject("FilledAngleArc");
        arcObj.transform.position = B;
        arcObj.transform.rotation = Quaternion.LookRotation(normal);

        MeshFilter mf = arcObj.AddComponent<MeshFilter>();
        MeshRenderer mr = arcObj.AddComponent<MeshRenderer>();

        Material fillMat = new Material(Shader.Find("Unlit/Color"));
        fillMat.color = new Color(1f, 0.9f, 0f, 1f); 
        fillMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mr.material = fillMat;

        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        verts.Add(Vector3.zero);

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float currentAngle = t * angleBetween;
            Vector3 pointDir = Quaternion.AngleAxis(currentAngle, normal) * dirA;
            verts.Add(pointDir * arcRadius);
        }

        for (int i = 1; i <= arcSegments; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

               if (Camera.main != null)
        {
            arcObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
    }

       private void DrawArcOutline()
    {
        Vector3 A = selectedPositions[0];
        Vector3 B = selectedPositions[1];
        Vector3 C = selectedPositions[2];

        Vector3 dirA = (A - B).normalized;
        Vector3 dirC = (C - B).normalized;
        Vector3 normal = Vector3.Cross(dirA, dirC).normalized;
        float angleBetween = Vector3.Angle(dirA, dirC);

        if (arcOutlineObj != null) Destroy(arcOutlineObj);

        arcOutlineObj = new GameObject("ArcOutline");
        LineRenderer lr = arcOutlineObj.AddComponent<LineRenderer>();

        lr.positionCount = arcSegments + 1;
        lr.startWidth = lineWidth * 2f;
        lr.endWidth = lineWidth * 2f;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.red;
        lr.useWorldSpace = true;
        lr.numCapVertices = 5;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float currentAngle = t * angleBetween;
            Vector3 pointDir = Quaternion.AngleAxis(currentAngle, normal) * dirA;
            lr.SetPosition(i, B + pointDir * arcRadius);
        }
    }

    private GameObject CreateLine(Vector3 start, Vector3 end, Color color, string name)
    {
        GameObject lineObj = new GameObject(name);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { start, end });
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        return lineObj;
    }

    private GameObject CreateTextMeshPro(string text, Vector3 position)
    {
        GameObject textObj = new GameObject("AngleLabel");
        textObj.transform.position = position;

        var textMeshPro = textObj.AddComponent<TextMeshPro>();

        textMeshPro.text = text;
        textMeshPro.fontSize = 5;
        textMeshPro.color = Color.yellow;
        textMeshPro.alignment = TextAlignmentOptions.Center;
      
        if (Camera.main != null)
        {
            textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - Camera.main.transform.position);
        }

        return textObj;
    }

    private void ResetSelection()
    {
        foreach (var sphere in selectedSpheres)
        {
            sphere.transform.localScale = Vector3.one * 0.02f;
            sphere.GetComponent<Renderer>().material.color = Color.white;
        }

        selectedSpheres.Clear();
        selectedPositions.Clear();
        currentMode = SelectionMode.None;

        if (button2) button2.interactable = false;
        if (button3) button3.interactable = false;
    }

    private void ClearPreviousVisuals()
    {
        foreach (var obj in lineObjects)
        {
            if (obj != null) Destroy(obj);
        }
        lineObjects.Clear();

        if (arcObj) Destroy(arcObj);
        arcObj = null;

        if (arcOutlineObj) Destroy(arcOutlineObj);
        arcOutlineObj = null;

        if (angleTextObj) Destroy(angleTextObj);
        angleTextObj = null;
    }
}
