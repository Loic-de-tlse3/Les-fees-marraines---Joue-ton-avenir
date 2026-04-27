using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public List<string> items = new List<string>();

    public void AddItem(string itemName)
    {
        if (!items.Contains(itemName))
        {
            items.Add(itemName);
            Debug.Log(itemName + " ajouté à l'inventaire");
        }
    }

    public bool HasItem(string itemName)
    {
        return items.Contains(itemName);
    }
}