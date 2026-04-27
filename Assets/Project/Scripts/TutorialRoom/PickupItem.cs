using UnityEngine;

public class PickupItem : InteractableObject
{
    public string itemName = "Objet";

    public override void Interact()
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
        if (inventory != null)
        {
            inventory.AddItem(itemName);
        }

        TutorialManager tutorial = FindFirstObjectByType<TutorialManager>();
        if (tutorial != null)
        {
            tutorial.ValidatePickupStep();
        }

        gameObject.SetActive(false);
    }
}