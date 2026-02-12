using UnityEngine;

public class ClueHUDBinder : MonoBehaviour
{
    public VRCornerHUD hud;

    private void Awake()
    {
        if (!hud) hud = GetComponent<VRCornerHUD>();
    }

    private void OnEnable()
    {
        if (ClueTrackerRealtimeDB.Instance != null)
            ClueTrackerRealtimeDB.Instance.OnTotalChanged += HandleTotalChanged;
    }

    private void OnDisable()
    {
        if (ClueTrackerRealtimeDB.Instance != null)
            ClueTrackerRealtimeDB.Instance.OnTotalChanged -= HandleTotalChanged;
    }

    private void HandleTotalChanged(int total)
    {
        if (hud) hud.SetFound(total);
    }
}
