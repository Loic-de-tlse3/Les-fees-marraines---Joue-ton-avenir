using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;


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
    [SerializeField] private bool usePlayerApproachAnimation = true; // Si vrai, le perso se rapproche et joue l'animation
    
    private CombatManager combatManager;
    private PlayerMovement2D playerMovement;
    private bool isCollected = false;
    private Collider2D cachedCollider;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        // Cherche le CombatManager dans la scène
        combatManager = FindObjectOfType<CombatManager>();
        
        if (combatManager == null)
        {
            Debug.LogWarning($"CombatManager non trouvé dans la scène pour l'item {itemType}");
        }

        // Cherche le PlayerMovement2D pour le déplacement et l'animation
        playerMovement = FindObjectOfType<PlayerMovement2D>();
        if (playerMovement == null && usePlayerApproachAnimation)
        {
            Debug.LogWarning($"PlayerMovement2D non trouvé dans la scène pour l'item {itemType}. Fallback sans approche du personnage.");
        }

        Collider2D itemCollider = GetComponent<Collider2D>();
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        itemCollider.isTrigger = true;
        cachedCollider = itemCollider;
    }

    private void Update()
    {
        if (isCollected) return;

        // New Input System fallback: check touches
        #if ENABLE_INPUT_SYSTEM
        try
        {
            var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            foreach (var t in activeTouches)
            {
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector2 screenPos = t.screenPosition;
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                    RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                    if (hit.collider == cachedCollider)
                    {
                        CollectItem();
                        return;
                    }
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider == cachedCollider)
                {
                    CollectItem();
                    return;
                }
            }
        }
        catch { }
        #endif
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
        if (isCollected) return;

        if (usePlayerApproachAnimation && playerMovement != null)
        {
            // Lance la seq d'approche du personnage + animation + collection
            StartCoroutine(ApproachAndCollect());
        }
        else
        {
            // Immédiate collection (sans approche)
            FinishCollecting();
        }
    }

    private IEnumerator ApproachAndCollect()
    {
        isCollected = true;

        // Étape 0 : Tourner le personnage vers l'item
        Vector3 itemPosition = transform.position;
        Vector3 playerPos = playerMovement.transform.position;
        
        if (itemPosition.x < playerPos.x)
        {
            // L'item est à gauche : tourner le modèle à gauche
            playerMovement.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (itemPosition.x > playerPos.x)
        {
            // L'item est à droite : tourner le modèle à droite
            playerMovement.transform.localScale = new Vector3(1, 1, 1);
        }
        // Attendre un petit instant pour que la rotation soit visible
        yield return new WaitForSeconds(0.1f);

        // Étape 1 : Le personnage marche vers l'item
        playerMovement.MoveToPosition(itemPosition, () =>
        {
            // Callback lorsque le personnage atteint l'item
        });

        // Attendre que le mouvement soit fini (approximativement)
        // On peut estimer: distance / speed + un petit buffer
        float distance = Vector3.Distance(playerMovement.transform.position, itemPosition);
        float estimatedTime = distance / playerMovement.speed + 0.2f;
        yield return new WaitForSeconds(Mathf.Max(estimatedTime, 0.5f));

        // Étape 2 : Jouer l'animation d'interaction
        playerMovement.PlayInteractAnimation();
        yield return new WaitForSeconds(0.5f); // Durée estimée de l'animation Interact

        // Étape 3 : Finir la collection
        FinishCollecting();
    }

    private void FinishCollecting()
    {
        Debug.Log($"Item collecté: {itemType}");
        
        // Notifie le CombatManager
        if (combatManager != null)
        {
            combatManager.CollectItem(itemType);
        }
        
        // Enlève le visuel de l'item
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Désactiver le collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Détruire l'objet après un léger délai
        Destroy(gameObject, 0.1f);
    }

    // Accesseur pour vérifier si l'item est collecté
    public bool IsCollected()
    {
        return isCollected;
    }

    // Expose le type d'item pour que d'autres composants puissent le repérer
    public string ItemType => itemType;
}
