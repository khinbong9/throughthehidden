using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;

public class SignUpManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;

    [Header("Scene after signup")]
    public string loginSceneName = "user_login";

    private FirebaseAuth auth;
    private DatabaseReference dbRoot;
    private bool isBusy = false;

    private async void Awake()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError("Firebase deps error: " + status);
            if (errorText) errorText.text = "Firebase error: " + status;
            return;
        }
        auth = FirebaseAuth.DefaultInstance;
        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // Call this from your Poke/Select event
    public void OnSignUpButtonPressed()
    {
        if (isBusy) return;
        _ = SignUp(emailInput.text.Trim(), passwordInput.text);
    }

    private async Task SignUp(string email, string password)
    {
        isBusy = true;
        if (errorText) errorText.text = "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (errorText) errorText.text = "Email/password cannot be empty.";
            isBusy = false;
            return;
        }

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            // Save email to database under user_data
            if (result != null && result.User != null)
            {
                string userId = result.User.UserId;
                await SaveUserEmail(userId, email);
            }

            // optional: store email to autofill login
            PlayerPrefs.SetString("LAST_EMAIL", email);

            // ONLY change scene after Firebase signup success
            SceneManager.LoadScene(loginSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            if (errorText) errorText.text = "Sign up failed: " + e.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task SaveUserEmail(string userId, string email)
    {
        try
        {
            var userDataRef = dbRoot.Child("users").Child(userId).Child("user_data");
            await userDataRef.Child("email").SetValueAsync(email);
            Debug.Log($"Saved email for user {userId} to database");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save email to database: {e.Message}");
        }
    }
}
