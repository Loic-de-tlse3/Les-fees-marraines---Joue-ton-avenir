using UnityEngine;

public class ScreenOrientationManager : MonoBehaviour
{
    void Start()
    {
        // Vérifie le nom de la scène actuelle (case-sensitive)
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Si la scène est "Connexion" ou "Inscription", force le mode portrait
        if (currentSceneName == "Connexion" || currentSceneName == "Inscription")
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
        else
        {
            // Sinon, force le mode paysage
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }
}
