using TMPro;
using UnityEngine;

public class VRCornerHUD : MonoBehaviour
{
    [Header("References")]
    public Transform hmd;            // drag Main Camera here
    public TMP_Text clueText;        // drag TMP text here

    [Header("Clue Display")]
    public int totalClues = 5;

    [Header("Placement (relative to HMD)")]
    public Vector3 localOffset = new Vector3(0.25f, 0.15f, 0.6f); // right, up, forward

    [Header("Smoothing")]
    public float posSmooth = 12f;
    public float rotSmooth = 12f;

    private int currentFound;

    private void Start()
    {
        // Ensure it shows something on start
        SetFound(0);
    }

    private void LateUpdate()
    {
        if (!hmd) return;

        Vector3 targetPos = hmd.TransformPoint(localOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * posSmooth);

        // Face the camera but stay upright (no roll)
        Vector3 lookDir = (transform.position - hmd.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) lookDir = -hmd.forward;

        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSmooth);
    }

    public void SetFound(int found)
    {
        currentFound = Mathf.Clamp(found, 0, totalClues);
        if (clueText) clueText.text = $"{currentFound}/{totalClues}";
    }
}
