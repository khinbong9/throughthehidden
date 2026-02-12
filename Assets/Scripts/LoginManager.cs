using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Only if using TextMeshPro

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;   // Change to InputField if not using TMP
    public TMP_InputField passwordInput;
    public TMP_Text errorText;             // Change to Text if not using TMP

    [Header("Premade Credentials")]
    public string correctUsername = "abcdef";
    public string correctPassword = "12345";

    [Header("Scene to Load")]
    public string sceneToLoad = "wk06";  

    public void OnLoginButtonPressed()
    {
        Debug.Log("Login button pressed!");
        
        // Check if references are assigned
        if (usernameInput == null)
        {
            Debug.LogError("Username Input is not assigned!");
            return;
        }
        if (passwordInput == null)
        {
            Debug.LogError("Password Input is not assigned!");
            return;
        }
        if (errorText == null)
        {
            Debug.LogError("Error Text is not assigned!");
            return;
        }
        
        string enteredUser = usernameInput.text;
        string enteredPass = passwordInput.text;
        
        Debug.Log($"Entered Username: '{enteredUser}', Password: '{enteredPass}'");
        Debug.Log($"Expected Username: '{correctUsername}', Password: '{correctPassword}'");

        if (enteredUser == correctUsername && enteredPass == correctPassword)
        {
            // Correct login
            Debug.Log("Login successful! Loading scene: " + sceneToLoad);
            errorText.text = "";  // Clear error
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            // Wrong username or password
            Debug.Log("Login failed - incorrect credentials");
            errorText.text = "Incorrect username or password!";
        }
    }
}

