using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI messageText;

    private FirebaseAuth auth;

    void Start()
    {
        // Initialise Firebase
        FirebaseApp app = FirebaseApp.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    public async void Register()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        try
        {
            var user = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            messageText.text = "Inscription réussie !";
        }
        catch (System.Exception e)
        {
            messageText.text = "Erreur: " + e.Message;
        }
    }

    public async void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        try
        {
            var user = await auth.SignInWithEmailAndPasswordAsync(email, password);
            messageText.text = "Connexion réussie !";
        }
        catch (System.Exception e)
        {
            messageText.text = "Erreur: " + e.Message;
        }
    }
}
