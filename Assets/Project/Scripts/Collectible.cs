using UnityEngine;

/// <summary>
/// Script pour les items collectibles (Lanterne et Enceinte)
/// À placer sur les GameObjects collectibles
/// </summary>
public class Collectible : MonoBehaviour
{
    [SerializeField] private string itemType = ""; // "Lanterne" ou "Enceinte"
    [SerializeField] private bool allowClickCollect = true;
    [SerializeField] private bool requirePlayerTagForCollision = false;
    [SerializeField] private string playerTag = "Player";
    private CombatManager combatManager;
    private bool isCollected = false;

    private void Start()
    {
        // Cherche le CombatManager dans la scène
        combatManager = FindObjectOfType<CombatManager>();
        
        if (combatManager == null)
        {
            Debug.LogWarning($"CombatManager non trouvé dans la scène pour l'item {itemType}");
        }

        Collider2D itemCollider = GetComponent<Collider2D>();
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        itemCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected)
        {
            return;
        }

        if (!requirePlayerTagForCollision || collision.CompareTag(playerTag))
        {
            CollectItem();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCollected)
        {
            return;
        }

        if (!requirePlayerTagForCollision || collision.gameObject.CompareTag(playerTag))
        {
            CollectItem();
        }
    }

    private void OnMouseDown()
    {
        if (!allowClickCollect || isCollected)
        {
            return;
        }

        CollectItem();
    }

    private void CollectItem()
    {
        isCollected = true;
        
        Debug.Log($"Item collecté: {itemType}");
        
        // Notifie le CombatManager
        if (combatManager != null)
        {
            combatManager.CollectItem(itemType);
        }
        
        // Enlève le visuel de l'item
        // Option 1: Désactiver le SpriteRenderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Option 2: Désactiver le collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Option 3: Détruire l'objet après un léger délai (plus propre)
        Destroy(gameObject, 0.1f);
    }

    // Accesseur pour vérifier si l'item est collecté
    public bool IsCollected()
    {
        return isCollected;
    }
}
