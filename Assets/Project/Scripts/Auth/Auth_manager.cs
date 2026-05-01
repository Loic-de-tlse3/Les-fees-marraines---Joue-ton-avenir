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

    void Awake()
    {
        // Vérifications rapides des références UI (évite NullReference silencieuses)
        if (emailInput == null) Debug.LogError("AuthManager: emailInput n'est pas assigné dans l'Inspector");
        if (passwordInput == null) Debug.LogError("AuthManager: passwordInput n'est pas assigné dans l'Inspector");
        if (messageText == null) Debug.LogError("AuthManager: messageText n'est pas assigné dans l'Inspector");

        try
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            Debug.Log("Firebase Auth initialisé correctement.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Erreur d'initialisation Firebase: " + e.GetType().Name + " - " + e.Message + "\n" + e.StackTrace);
            if (messageText != null) messageText.text = "Erreur d'initialisation Firebase: " + e.Message;
        }
    }

    public async void Register()
    {
        string email = emailInput?.text ?? "";
        string password = passwordInput?.text ?? "";

        if (string.IsNullOrEmpty(email))
        {
            if (messageText != null) messageText.text = "Veuillez entrer une adresse e-mail valide.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            if (messageText != null) messageText.text = "Veuillez entrer un mot de passe.";
            return;
        }

        if (auth == null)
        {
            if (messageText != null) messageText.text = "Firebase n'est pas prêt. Regarde la Console Unity.";
            Debug.LogError("Register() appelé alors que FirebaseAuth n'est pas initialisé.");
            return;
        }

        try
        {
            var user = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            if (messageText != null) messageText.text = "Inscription réussie !";
        }
        catch (FirebaseException e)
        {
            if (messageText != null) messageText.text = GetFrenchErrorMessage(e);
            Debug.LogError("FirebaseException lors de l'inscription: " + e.Message);
        }
        catch (System.Exception e)
        {
            if (messageText != null) messageText.text = "Une erreur inattendue est survenue. (" + e.Message + ")";
            Debug.LogException(e);
        }
    }

    public async void Login()
    {
        string email = emailInput?.text ?? "";
        string password = passwordInput?.text ?? "";

        if (string.IsNullOrEmpty(email))
        {
            if (messageText != null) messageText.text = "Veuillez entrer une adresse e-mail valide.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            if (messageText != null) messageText.text = "Veuillez entrer un mot de passe.";
            return;
        }

        if (auth == null)
        {
            if (messageText != null) messageText.text = "Firebase n'est pas prêt. Regarde la Console Unity.";
            Debug.LogError("Login() appelé alors que FirebaseAuth n'est pas initialisé.");
            return;
        }

        try
        {
            var user = await auth.SignInWithEmailAndPasswordAsync(email, password);
            if (messageText != null) messageText.text = "Connexion réussie !";
            SceneManager.LoadScene("EnigmeScene");
        }
        catch (FirebaseException e)
        {
            if (messageText != null) messageText.text = GetFrenchErrorMessage(e);
            Debug.LogError("FirebaseException lors de la connexion: " + e.Message);
        }
        catch (System.Exception e)
        {
            if (messageText != null) messageText.text = "Une erreur inattendue est survenue. (" + e.Message + ")";
            Debug.LogException(e);
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
