using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene_changer : MonoBehaviour
{
    // Nom de la scène vers laquelle tu veux aller
    public string sceneName;

    // Méthode appelée quand on clique sur le bouton
    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
