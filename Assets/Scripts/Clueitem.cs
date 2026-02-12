using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ClueItemGrab : MonoBehaviour
{
    [Header("Clue Data")]
    public string itemId = "clue_001";
    public string itemName = "Keycard";

    [Header("Optional")]
    public bool disableAfterSaved = true;

    private XRGrabInteractable grab;
    private bool alreadyTriggered;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        if (grab != null) grab.selectEntered.RemoveListener(OnGrabbed);
    }

    private async void OnGrabbed(SelectEnterEventArgs args)
    {
        if (alreadyTriggered) return;
        alreadyTriggered = true;

        if (ClueTrackerRealtimeDB.Instance == null)
        {
            Debug.LogError("ClueTrackerRealtimeDB.Instance is null. Put ClueTrackerRealtimeDB in your scene.");
            alreadyTriggered = false;
            return;
        }

        bool saved = await ClueTrackerRealtimeDB.Instance.RegisterClueAsync(itemId, itemName);

        // If it was already saved before, allow grabbing again (optional)
        if (!saved)
        {
            alreadyTriggered = false;
            return;
        }

        if (disableAfterSaved)
        {
            // Disable interactable + collider so player can't keep grabbing it
            grab.enabled = false;
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }
}
