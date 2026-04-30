using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // C'est l'objet qui contient ton texte de tuto et ton bouton
    public GameObject tutoWelcomePanel; 
    public GameObject bossApparitionTexte;

    private void Start()
    {
    }

    // Cette fonction sera activée quand tu cliqueras sur le bouton "Suivant"
    public void CloseTuto()
    {
        if (tutoWelcomePanel != null)
        {
            tutoWelcomePanel.SetActive(false); // Cache le panneau
            Debug.Log("Tuto fermé !");
        }
        // Notify CombatManager that intro has been closed
        var cm = FindObjectOfType<CombatManager>();
        if (cm != null)
        {
            cm.NotifyIntroClosed();
        }
    }

    public void ShowBossApparitionText()
    {
        if (bossApparitionTexte == null)
        {
            Transform found = FindInactiveInSceneByName("BossApparitionTexte");
            if (found != null)
            {
                bossApparitionTexte = found.gameObject;
            }
        }

        if (bossApparitionTexte != null)
        {
            bossApparitionTexte.SetActive(true);
        }
        else
        {
            Debug.LogWarning("DialogueManager: BossApparitionTexte introuvable dans la scène.", this);
        }
    }

    // Silent hide (used on Start), does NOT notify CombatManager
    public void HideBossApparitionText()
    {
        if (bossApparitionTexte != null)
        {
            bossApparitionTexte.SetActive(false);
        }
    }

    // Call this from the UI button when the player closes the boss apparition text.
    public void CloseBossApparition()
    {
        HideBossApparitionText();
        var cm = FindObjectOfType<CombatManager>();
        if (cm != null)
        {
            cm.NotifyBossApparitionClosed();
        }
    }

    // Use this when the boss button should also close the intro (tuto)
    // and enable the combat messages immediately. Bind the BossApparition
    // button to this method in the Inspector if you want that behaviour.
    public void CloseTutoFromBossButton()
    {
        CloseTuto();
        var cm = FindObjectOfType<CombatManager>();
        if (cm != null)
        {
            cm.EnableMessagesNow();
        }
    }

    private Transform FindInactiveInSceneByName(string targetName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current == null)
            {
                continue;
            }

            if (!current.gameObject.scene.IsValid())
            {
                continue;
            }

            if (current.name == targetName)
            {
                return current;
            }
        }

        return null;
    }
}
