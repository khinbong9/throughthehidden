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

    // Main-thread relay
    private bool hasPendingTotal;
    private int pendingTotal;

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

        TryStartListeningTotal();
    }

    private void Update()
    {
        // ✅ Unity main thread: safe to touch UI / invoke handlers here
        if (hasPendingTotal)
        {
            hasPendingTotal = false;
            OnTotalChanged?.Invoke(pendingTotal);
        }
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

    public void NotifyUserLoggedIn()
    {
        TryStartListeningTotal();
    }

    public void NotifyUserLoggedOut()
    {
        StopListeningTotal();
        QueueTotalForMainThread(0);
    }

    private void TryStartListeningTotal()
    {
        if (!firebaseReady) return;

        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("TryStartListeningTotal: No logged-in user yet.");
            return;
        }

        StopListeningTotal();

        totalRef = UserCluePath(uid).Child("total");

        totalHandler = (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Total listener error: " + args.DatabaseError.Message);
                return;
            }

            int total = SnapshotToInt(args.Snapshot);
            QueueTotalForMainThread(total);
        };

        totalRef.ValueChanged += totalHandler;

        // Fetch once immediately (also relay to main thread)
        _ = totalRef.GetValueAsync().ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
            {
                int total = SnapshotToInt(t.Result);
                QueueTotalForMainThread(total);
            }
            else
            {
                QueueTotalForMainThread(0);
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

    private void QueueTotalForMainThread(int total)
    {
        pendingTotal = total;
        hasPendingTotal = true;
    }

    private int SnapshotToInt(DataSnapshot snap)
    {
        if (snap == null || snap.Value == null) return 0;

        try
        {
            if (snap.Value is long l) return (int)l;
            if (snap.Value is double d) return (int)d;
            return Convert.ToInt32(snap.Value);
        }
        catch
        {
            return 0;
        }
    }

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
            var snap = await itemNameRef.GetValueAsync();
            if (snap.Exists)
            {
                Debug.Log($"Clue already saved: {itemId}. Not incrementing total.");
                return false;
            }

            await itemNameRef.SetValueAsync(itemName);

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

            Debug.Log($"Saved clue: {itemId} ({itemName})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save clue: " + e);
            return false;
        }
    }
}
