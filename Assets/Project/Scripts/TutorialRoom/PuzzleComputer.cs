using System.Collections;
using UnityEngine;

public class PuzzleComputer : InteractableObject
{
    public GameObject puzzlePanel;
    public Transform interactionPoint;
    public PlayerMovement2D player;
    public float interactionDelay = 0.6f;

    public override void Interact()
    {
        if (player == null || interactionPoint == null)
            return;

        // Toujours regarder vers le PC (à droite)
        player.transform.localScale = new Vector3(1, 1, 1);

        // Toujours aller au point d'interaction placé à gauche du PC
        player.MoveToPosition(interactionPoint.position, () =>
        {
            // Reforce encore la direction juste avant l'anim
            player.transform.localScale = new Vector3(1, 1, 1);

            player.PlayInteractAnimation();
            StartCoroutine(OpenPuzzleAfterDelay());
        });
    }

    private IEnumerator OpenPuzzleAfterDelay()
    {
        yield return new WaitForSeconds(interactionDelay);

        if (puzzlePanel != null)
            puzzlePanel.SetActive(true);
    }
}