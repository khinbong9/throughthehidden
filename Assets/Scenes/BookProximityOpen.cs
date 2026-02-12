using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BookProximityOpen : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Drag the VR camera here: XR Origin > Camera Offset > Main Camera")]
    public Transform head;

    [Tooltip("XR Grab Interactable on the BOOK PARENT object")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    [Header("Model Swap")]
    [Tooltip("Closed book model GameObject (child of this parent)")]
    public GameObject closedModel;

    [Tooltip("Open book model GameObject (child of this parent). Disable it at start.")]
    public GameObject openModel;

    [Header("Open Trigger Settings")]
    [Tooltip("Distance (meters) from book to head to trigger open. Try 0.35–0.60")]
    public float openDistance = 0.45f;

    [Tooltip("How long the book must stay within distance to open (seconds)")]
    public float holdTime = 0.20f;

    [Tooltip("If true: open immediately when grabbed (no proximity needed). Useful for testing.")]
    public bool openOnGrab = false;

    [Header("Optional: Clue UI")]
    [Tooltip("Canvas or UI object to show only when book is open (optional)")]
    public GameObject clueCanvas;

    private bool isHeld;
    private bool isOpen;
    private float nearTimer;

    void Reset()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (Camera.main != null) head = Camera.main.transform;
    }

    void Awake()
    {
        // Auto-fill if possible
        if (grab == null) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (head == null && Camera.main != null) head = Camera.main.transform;

        // Start CLOSED
        SetOpen(false);

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogError("[BookProximityOpen] Missing XRGrabInteractable on the same GameObject.");
        }
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }

    void Update()
    {
        if (!isHeld) return;
        if (isOpen) return;

        if (openOnGrab)
        {
            SetOpen(true);
            return;
        }

        if (head == null) return;

        // Distance from book PARENT to camera
        float d = Vector3.Distance(transform.position, head.position);

        if (d <= openDistance)
        {
            nearTimer += Time.deltaTime;
            if (nearTimer >= holdTime)
            {
                SetOpen(true);
            }
        }
        else
        {
            nearTimer = 0f;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        nearTimer = 0f;

        // If you want: open instantly on grab for your checkpoint video, set openOnGrab=true.
        if (openOnGrab)
            SetOpen(true);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        nearTimer = 0f;

        // Close again when released (feels natural + prevents stuck state)
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (closedModel != null) closedModel.SetActive(!open);
        if (openModel != null) openModel.SetActive(open);

        if (clueCanvas != null) clueCanvas.SetActive(open);
    }
}
