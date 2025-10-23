using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Target Positions")]
    public Vector3 upPosition;
    public Vector3 downPosition;
    public Vector3 leftPosition;
    public Vector3 rightPosition;
    public Vector3 middlePosition;

    [Header("Target Object")]
    public GameObject OBJ;

    [Header("UI RawImages for Directions (optional)")]
    public RawImage upImage;
    public RawImage downImage;
    public RawImage leftImage;
    public RawImage rightImage;
    public RawImage middleImage;

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 1f, 1f, 0.6f); // slightly bright
    private Color normalColor = Color.white;

    [Range(0.1f, 1f)]
    public float colorResetDelay = 0.2f;

    private void Start()
    {
       
        if (upImage != null)
            normalColor = upImage.color;
    }

    public void MoveUp()
    {
        MoveTo(upPosition);
        if (upImage != null) StartCoroutine(FlashRawImage(upImage));
    }

    public void MoveDown()
    {
        MoveTo(downPosition);
        if (downImage != null) StartCoroutine(FlashRawImage(downImage));
    }

    public void MoveLeft()
    {
        MoveTo(leftPosition);
        if (leftImage != null) StartCoroutine(FlashRawImage(leftImage));
    }

    public void MoveRight()
    {
        MoveTo(rightPosition);
        if (rightImage != null) StartCoroutine(FlashRawImage(rightImage));
    }

    public void MoveMiddle()
    {
        MoveTo(middlePosition);
        if (middleImage != null) StartCoroutine(FlashRawImage(middleImage));
    }

    private void MoveTo(Vector3 target)
    {
        if (OBJ != null)
            OBJ.transform.position = target;
    }

    private IEnumerator FlashRawImage(RawImage img)
    {
        img.color = highlightColor;
        yield return new WaitForSeconds(colorResetDelay);
        img.color = normalColor;
    }
}
