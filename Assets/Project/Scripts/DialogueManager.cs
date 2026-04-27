using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // C'est l'objet qui contient ton texte de tuto et ton bouton
    public GameObject tutoWelcomePanel; 

    // Cette fonction sera activée quand tu cliqueras sur le bouton "Suivant"
    public void CloseTuto()
    {
        if (tutoWelcomePanel != null)
        {
            tutoWelcomePanel.SetActive(false); // Cache le panneau
            Debug.Log("Tuto fermé !");
        }
    }
}
