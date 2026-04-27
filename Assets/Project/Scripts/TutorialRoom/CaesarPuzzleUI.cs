using TMPro;
using UnityEngine;

public class CaesarPuzzleUI : MonoBehaviour
{
    [Header("UI Principale")]
    public TMP_Text titleText;
    public TMP_Text encryptedText;
    public TMP_Text shiftText;
    public TMP_Text feedbackText;
    public TMP_InputField answerInput;
    public GameObject puzzlePanel;
    public NotificationBar notificationBar;

    [Header("Aide & Joker")]
    [Tooltip("L'objet qui contient le personnage et sa boîte de dialogue")]
    public GameObject jokerCharacter; 
    [Tooltip("Le texte à l'intérieur de la bulle du Joker")]
    public TMP_Text helpHintText;     

    [Header("Configuration")]
    public string[] possibleWords = { "MAGIE", "SECRET", "JUNGLE", "PORTAIL", "CODE", "SORT", "FORET", "LUMIERE" };
    public int minShift = 1;
    public int maxShift = 5;

    private string originalMessage;
    private string codedMessage;
    private int currentShift;

    private void OnEnable()
    {
        GeneratePuzzle();
    }

    public void GeneratePuzzle()
    {
        if (possibleWords == null || possibleWords.Length == 0) return;

        originalMessage = possibleWords[Random.Range(0, possibleWords.Length)];
        currentShift = Random.Range(minShift, maxShift + 1);
        codedMessage = Encode(originalMessage, currentShift);

        if (titleText != null) titleText.text = "Énigme : Code de César";
        if (encryptedText != null) encryptedText.text = codedMessage;
        if (shiftText != null) shiftText.text = "Décalage : " + currentShift;
        if (feedbackText != null) feedbackText.text = "";
        if (answerInput != null) answerInput.text = "";
        
        if (jokerCharacter != null) jokerCharacter.SetActive(false);
    }
// FONCTION APPELÉE PAR LE BOUTON AIDE
    public void CallForHelp()
    {
        // 1. NETTOYAGE : On efface le texte de l'écran principal
        if (feedbackText != null) 
        {
            feedbackText.text = ""; 
        }

        if (jokerCharacter != null)
        {
            // 2. On affiche le personnage et sa bulle
            jokerCharacter.SetActive(true); 
            
            // 3. On change le texte du Joker pour donner l'indice
            if (helpHintText != null)
            {
                helpHintText.text = " Le code a été décalé de " + currentShift + ". Pour trouver la solution, tu dois reculer dans l'alphabet !";
            }

            // 4. Message discret jaune sur l'écran principal (Optionnel)
            // Si tu ne veux même pas ce texte jaune, tu peux supprimer ces 4 lignes
            if (feedbackText != null)
            {
                feedbackText.text = "Le Joker arrive...";
                feedbackText.color = Color.yellow;
            }

            // NOTE : J'ai supprimé la ligne notificationBar.ShowMessage ici.
            // Donc plus de carré rouge/vert en haut de l'écran !
        }
        else
        {
            Debug.LogWarning("Attention : Glisse l'objet Joker dans l'Inspector !");
        }
    }

    public void ValidateAnswer()
    {
        if (answerInput == null) return;

        string playerAnswer = answerInput.text.Trim().ToUpper();

        if (playerAnswer == originalMessage)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Bonne réponse !";
                feedbackText.color = Color.green;
            }

            if (notificationBar != null)
                notificationBar.ShowMessage("Félicitations !", true);

            Invoke("ClosePuzzle", 1.2f);
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Ce n'est pas ça...";
                feedbackText.color = Color.red;
            }

            if (notificationBar != null)
                notificationBar.ShowMessage("Réponse incorrecte", false);
        }
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);
    }

    private string Encode(string text, int shiftValue)
    {
        string result = "";
        foreach (char c in text.ToUpper())
        {
            if (c >= 'A' && c <= 'Z')
            {
                int offset = (c - 'A' + shiftValue) % 26;
                if (offset < 0) offset += 26; 
                char encoded = (char)(offset + 'A');
                result += encoded;
            }
            else { result += c; }
        }
        return result;
    }
}