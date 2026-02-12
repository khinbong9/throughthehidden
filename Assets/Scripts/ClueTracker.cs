using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class ClueTrackerRealtimeDB : MonoBehaviour
{
    public static ClueTrackerRealtimeDB Instance;

    public event Action<int> OnTotalChanged;

    private FirebaseAuth auth;
    private DatabaseReference dbRoot;

    private DatabaseReference totalRef;
    private EventHandler<ValueChangedEventArgs> totalHandler;

    private bool firebaseReady;

    private async void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError("Firebase dependency error: " + status);
            firebaseReady = false;
            return;
        }

        auth = FirebaseAuth.DefaultInstance;
        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
        firebaseReady = true;

        // If user already logged in (eg: auto session), start listening immediately
        TryStartListeningTotal();
    }

    private void OnDestroy()
    {
        StopListeningTotal();
    }

    private string GetUserId()
    {
        var user = auth?.CurrentUser;
        return user != null ? user.UserId : null;
    }

    private DatabaseReference UserCluePath(string uid) =>
        dbRoot.Child("users").Child(uid).Child("clue_found");

    /// Call this after login success (recommended)
    public void NotifyUserLoggedIn()
    {
        TryStartListeningTotal();
    }

    /// Call this after logout (optional)
    public void NotifyUserLoggedOut()
    {
        StopListeningTotal();
        OnTotalChanged?.Invoke(0);
    }

    private void TryStartListeningTotal()
    {
        if (!firebaseReady) return;

        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid)) return;

        StopListeningTotal(); // prevent duplicate listeners

        totalRef = UserCluePath(uid).Child("total");

        totalHandler = (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Total listener error: " + args.DatabaseError.Message);
                return;
            }

            int total = SnapshotToInt(args.Snapshot);
            OnTotalChanged?.Invoke(total);
        };

        totalRef.ValueChanged += totalHandler;

        // Fetch once right away so UI shows current value immediately
        _ = totalRef.GetValueAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                int total = SnapshotToInt(t.Result);
                OnTotalChanged?.Invoke(total);
            }
            else
            {
                OnTotalChanged?.Invoke(0);
            }
        });
    }

    private void StopListeningTotal()
    {
        if (totalRef != null && totalHandler != null)
            totalRef.ValueChanged -= totalHandler;

        totalRef = null;
        totalHandler = null;
    }

    private int SnapshotToInt(DataSnapshot snap)
    {
        if (snap == null || snap.Value == null) return 0;

        try
        {
            // Realtime DB sometimes returns long/double
            if (snap.Value is long l) return (int)l;
            if (snap.Value is double d) return (int)d;
            return Convert.ToInt32(snap.Value);
        }
        catch
        {
            return 0;
        }
    }

    /// Call this when clue is grabbed.
    /// Returns true if a new clue was saved, false if it already existed or failed.
    public async Task<bool> RegisterClueAsync(string itemId, string itemName)
    {
        if (!firebaseReady)
        {
            Debug.LogError("Firebase not ready yet.");
            return false;
        }

        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("No logged-in user. Login first before saving clues.");
            return false;
        }

        var clueRef = UserCluePath(uid);
        var itemNameRef = clueRef.Child("items").Child(itemId).Child("itemName");
        var totalRefLocal = clueRef.Child("total");

        try
        {
            // 1) Check if item already exists (avoid double counting)
            var snap = await itemNameRef.GetValueAsync();
            if (snap.Exists)
            {
                Debug.Log($"Clue already saved: {itemId}. Not incrementing total.");
                return false;
            }

            // 2) Save item name under itemId
            await itemNameRef.SetValueAsync(itemName);

            // 3) Increment total safely using transaction
            await totalRefLocal.RunTransaction(mutableData =>
            {
                long current = 0;
                if (mutableData.Value != null)
                {
                    try { current = Convert.ToInt64(mutableData.Value); }
                    catch { current = 0; }
                }

                mutableData.Value = current + 1;
                return TransactionResult.Success(mutableData);
            });

            Debug.Log($"Saved clue: {itemId} ({itemName}) to Realtime DB");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save clue: " + e);
            return false;
        }
    }
}
