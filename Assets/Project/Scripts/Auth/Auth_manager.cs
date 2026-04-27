using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.SceneManagement; 

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

        if (string.IsNullOrEmpty(email))
        {
            messageText.text = "Veuillez entrer une adresse e-mail valide.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            messageText.text = "Veuillez entrer un mot de passe.";
            return;
        }

        try
        {
            var user = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            messageText.text = "Inscription réussie !";
        }
        catch (FirebaseException e)
        {
            messageText.text = GetFrenchErrorMessage(e);
        }
        catch (System.Exception e)
        {
            messageText.text = "Une erreur inattendue est survenue. (" + e.Message + ")";
            Debug.LogError("Erreur inattendue : " + e.Message);
        }
    }

    public async void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            messageText.text = "Veuillez entrer une adresse e-mail valide.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            messageText.text = "Veuillez entrer un mot de passe.";
            return;
        }

        try
        {
            var user = await auth.SignInWithEmailAndPasswordAsync(email, password);
            messageText.text = "Connexion réussie !";
            SceneManager.LoadScene("ScèneTest"); 
        }
        catch (FirebaseException e)
        {
            messageText.text = GetFrenchErrorMessage(e);
        }
        catch (System.Exception e)
        {
            messageText.text = "Une erreur inattendue est survenue. (" + e.Message + ")";
            Debug.LogError("Erreur inattendue : " + e.Message);
        }
    }

    private string GetFrenchErrorMessage(FirebaseException e)
    {
        switch (e.ErrorCode)
        {
            case (int)AuthError.InvalidEmail:
                return "L'adresse e-mail est invalide.";
            case (int)AuthError.EmailAlreadyInUse:
                return "Cette adresse e-mail est déjà utilisée.";
            case (int)AuthError.WeakPassword:
                return "Le mot de passe est trop faible (minimum 6 caractères).";
            case (int)AuthError.UserNotFound:
                return "Aucun utilisateur trouvé avec cette adresse e-mail.";
            case (int)AuthError.WrongPassword:
                return "Le mot de passe est incorrect.";
            case (int)AuthError.NetworkRequestFailed:
                return "Échec de la connexion : vérifiez votre connexion Internet.";
            case (int)AuthError.MissingEmail:
                return "Veuillez entrer une adresse e-mail valide.";
            case (int)AuthError.MissingPassword:
                return "Veuillez entrer un mot de passe.";
            case (int)AuthError.TooManyRequests:
                return "Trop de tentatives. Veuillez réessayer plus tard.";
            case (int)AuthError.OperationNotAllowed:
                return "L'opération n'est pas autorisée. Veuillez contacter le support.";
            default:
                Debug.LogError("Erreur Firebase non gérée : " + e.ErrorCode + " - " + e.Message);
                return "Une erreur est survenue. Veuillez réessayer.";
        }
    }
}
