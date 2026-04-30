using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrateur de scénario pour le combat contre l'araignée.
/// Conserve les positions existantes des objets dans la scène.
/// </summary>
public class CombatManager : MonoBehaviour
{
    private enum ScenarioState
    {
        Intro,
        WaitLantern,
        WaitReturnForQTE1,
        QTE1Running,
        WaitSpeaker,
        WaitReturnForQTE2,
        QTE2Running,
        FadingSpider,
        Completed
    }

    [Header("Références scène")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject spiderRoot;
    [SerializeField] private GameObject spiderAura;
    [SerializeField] private GameObject lanternObject;
    [SerializeField] private GameObject speakerObject;

    [Header("UI message")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Vector2 messagePanelSize = new Vector2(1050f, 180f);
    [SerializeField] private Vector2 messagePanelAnchoredPosition = new Vector2(0f, 750f);

    [Header("QTE")]
    [SerializeField] private GameObject qtePrefab;
    [SerializeField] private Transform qteSpawnPoint;

    [Header("Tuning")]
    [SerializeField] private float introMessageDuration = 6f;
    [SerializeField] private float returnToSpiderDistance = 8f;
    [SerializeField] private float spiderFadeDuration = 4f;
    [SerializeField] private Color spiderAuraColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private bool resetSpiderTintOnStart = true;
    [SerializeField] private bool useCustomQTETriggerPosition = true;
    [SerializeField] private Vector2 customQTETriggerPosition = new Vector2(-1f, -2f);

    private ScenarioState state;
    private bool lanternCollected;
    private bool speakerCollected;
    private bool qte1Succeeded;
    private bool qte2Succeeded;

    private QTECombatBridge activeBridge;
    private Coroutine runningFlow;
    private SpriteRenderer[] spiderRenderers;
    private Color[] spiderOriginalColors;

    // Message gating: panel is hidden until both intro and boss apparition are closed
    private bool introClosed = false;
    private bool bossApparitionClosed = false;
    private bool allowMessages = false;
    private string pendingMessage = null;

    private const string IntroSentence = "Trouve un objet pour la faire taire.";
    private const string QTE1ReturnSentence = "Retrourne devant l’araignée pour utiliser la lampe, maintiens appuyer.";
    private const string HypnosisSentence = "La mère maléfique se met en colère et utilise un pouvoir hypnotique, trouve un objet pour ne plus l’entendre !";
    private const string QTE2ReturnSentence = "Retourne devant elle et monte le son en blayant ton écran vers le haut";
    private const string FinalEscapeSentence = "Elle est destabilisée mais s'échappe avec ta meilleure amie !";

    private void Awake()
    {
        messagePanelAnchoredPosition = new Vector2(0f, 750f);
        messagePanelSize = new Vector2(1050f, 180f);
        CacheSpiderRenderers();
    }

    private void Start()
    {
        Debug.Log("CombatManager: Start() appelé", this);
        state = ScenarioState.Intro;
        lanternCollected = false;
        speakerCollected = false;
        qte1Succeeded = false;
        qte2Succeeded = false;

        if (lanternObject != null)
        {
            lanternObject.SetActive(false);
            Debug.Log($"CombatManager: lanternObject désactivée: {lanternObject.name}", this);
        }

        if (speakerObject != null)
        {
            speakerObject.SetActive(false);
            Debug.Log($"CombatManager: speakerObject désactivée: {speakerObject.name}", this);
        }
        else
        {
            Debug.LogWarning("CombatManager: speakerObject NOT assignée dans l'inspector!", this);
        }

        if (spiderAura != null)
        {
            spiderAura.SetActive(false);
        }

        EnsureMessageUI();

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
                Debug.Log($"CombatManager: Joueur trouvé par tag: {player.name}", this);
            }
        }

        // Re-cache les renderers après que les références aient pu être assignées en inspector
        CacheSpiderRenderers();
        Debug.Log($"CombatManager: {spiderRenderers?.Length ?? 0} SpriteRenderers de l'araignée trouvés", this);

        if (resetSpiderTintOnStart)
        {
            ResetSpiderToNeutralColor();
            Debug.Log("CombatManager: Araignée réinitialisée en couleur neutre", this);
        }
        else
        {
            SetSpiderAura(false);
        }

        ShowMessage(IntroSentence);
        runningFlow = StartCoroutine(BeginScenario());
        Debug.Log("CombatManager: BeginScenario coroutine lancée", this);
    }

    private void EnsureMessageUI()
    {
        if (messagePanel != null && messageText != null)
        {
            ApplyMessagePanelLayout();
            // keep hidden until both intro and boss panels are closed
            messagePanel.SetActive(false);
            return;
        }

        if (messagePanel == null)
        {
            Transform existingPanel = transform.Find("CombatMessagePanel");
            if (existingPanel != null)
            {
                messagePanel = existingPanel.gameObject;
            }
        }

        if (messageText == null)
        {
            TMP_Text existingText = GetComponentInChildren<TMP_Text>(true);
            if (existingText != null)
            {
                messageText = existingText;
                if (messagePanel == null)
                {
                    messagePanel = existingText.transform.parent != null ? existingText.transform.parent.gameObject : null;
                }
            }
        }

        if (messagePanel != null && messageText != null)
        {
            ApplyMessagePanelLayout();
            messagePanel.SetActive(false);
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject panelObject = new GameObject("CombatMessagePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = messagePanelSize;
        panelRect.anchoredPosition = messagePanelAnchoredPosition;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject textObject = new GameObject("CombatMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(28f, 18f);
        textRect.offsetMax = new Vector2(-28f, -18f);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = 38f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 24f;
        tmp.fontSizeMax = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;

        messagePanel = panelObject;
        messageText = tmp;
        ApplyMessagePanelLayout();
        messagePanel.SetActive(false);
    }

    private void EnsurePanelOnTop()
    {
        if (messagePanel == null)
        {
            return;
        }

        messagePanel.transform.SetAsLastSibling();
    }

    private void ApplyMessagePanelLayout()
    {
        if (messagePanel == null)
        {
            return;
        }

        RectTransform panelRect = messagePanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = messagePanelSize;
        panelRect.anchoredPosition = messagePanelAnchoredPosition;
    }

    private void Update()
    {
        if (state == ScenarioState.WaitReturnForQTE1 && lanternCollected && IsPlayerNearSpider())
        {
            Debug.Log($"CombatManager: Conditions QTE1 réunies! lanternCollected={lanternCollected}, nearSpider={IsPlayerNearSpider()}", this);
            StartQTEPhase(1);
        }

        if (state == ScenarioState.WaitReturnForQTE2 && speakerCollected && IsPlayerNearSpider())
        {
            Debug.Log($"CombatManager: Conditions QTE2 réunies! speakerCollected={speakerCollected}, nearSpider={IsPlayerNearSpider()}", this);
            StartQTEPhase(2);
        }
    }

    public void CollectItem(string itemType)
    {
        string normalizedType = (itemType ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizedType == "lanterne" && !lanternCollected)
        {
            lanternCollected = true;

            if (state == ScenarioState.Intro || state == ScenarioState.WaitLantern || state == ScenarioState.WaitReturnForQTE1)
            {
                state = ScenarioState.WaitReturnForQTE1;
                ShowMessage(QTE1ReturnSentence);
            }

            return;
        }

        if (normalizedType == "enceinte" && !speakerCollected && state == ScenarioState.WaitSpeaker)
        {
            speakerCollected = true;
            state = ScenarioState.WaitReturnForQTE2;
            ShowMessage(QTE2ReturnSentence);
        }
    }

    private IEnumerator BeginScenario()
    {
        yield return new WaitForSeconds(introMessageDuration);

        HideMessage();

        if (lanternObject != null)
        {
            lanternObject.SetActive(true);
        }

        state = ScenarioState.WaitLantern;
    }

    private void StartQTEPhase(int phase)
    {
        Debug.Log($"CombatManager: StartQTEPhase({phase}) appelé. État actuel: {state}", this);
        
        if (qtePrefab == null)
        {
            Debug.LogError("CombatManager: qtePrefab non assigné.", this);
            return;
        }

        if (activeBridge != null)
        {
            Debug.LogWarning("CombatManager: Un QTE est déjà actif, ignoré", this);
            return;
        }

        state = phase == 1 ? ScenarioState.QTE1Running : ScenarioState.QTE2Running;

        Vector3 spawnPosition = qteSpawnPoint != null ? qteSpawnPoint.position : transform.position;
        Debug.Log($"CombatManager: Instanciation QTE prefab à position {spawnPosition}", this);
        GameObject qteInstance = Instantiate(qtePrefab, spawnPosition, Quaternion.identity);

        MobileQTESequence sequence = FindRuntimeQTESequence(qteInstance);
        if (sequence == null)
        {
            Debug.LogError("CombatManager: Aucun MobileQTESequence trouvé dans le prefab QTE.", this);
            return;
        }

        activeBridge = sequence.GetComponent<QTECombatBridge>();
        if (activeBridge == null)
        {
            activeBridge = sequence.GetComponentInParent<QTECombatBridge>();
        }

        if (activeBridge == null)
        {
            Debug.LogWarning("CombatManager: QTECombatBridge non trouvé sur la séquence active, ajout dynamique.", this);
            activeBridge = sequence.gameObject.AddComponent<QTECombatBridge>();
        }

        if (sequence != null)
        {
            int phaseStepIndex = phase == 1 ? 0 : 1;
            Debug.Log($"CombatManager: Configuration QTE pour phase {phase}, étape {phaseStepIndex}", this);
            sequence.ConfigureSingleStep(phaseStepIndex, false);
            sequence.BindCombatBridge(activeBridge);
            sequence.StartSequence();
        }
        else
        {
            Debug.LogError("CombatManager: MobileQTESequence NOT trouvé sur le prefab QTE!", this);
        }

        if (activeBridge != null)
        {
            Debug.Log("CombatManager: QTECombatBridge trouvé et abonné aux événements", this);
            activeBridge.SequenceSucceeded += OnQTESucceeded;
            activeBridge.SequenceFailed += OnQTEFailed;
        }
    }

    private MobileQTESequence FindRuntimeQTESequence(GameObject qteInstance)
    {
        if (qteInstance == null)
        {
            return null;
        }

        MobileQTESequence[] sequences = qteInstance.GetComponentsInChildren<MobileQTESequence>(true);
        if (sequences == null || sequences.Length == 0)
        {
            return null;
        }

        foreach (MobileQTESequence candidate in sequences)
        {
            if (candidate == null)
            {
                continue;
            }

            if (candidate.GetComponent<QTEHudPresenter>() != null)
            {
                Debug.Log($"CombatManager: Séquence QTE sélectionnée via QTEHudPresenter sur {candidate.name}", this);
                return candidate;
            }
        }

        Debug.Log($"CombatManager: Séquence QTE sélectionnée par défaut sur {sequences[0].name}", this);
        return sequences[0];
    }

    public void OnQTESucceeded()
    {
        if (state == ScenarioState.QTE1Running)
        {
            qte1Succeeded = true;
            if (spiderAura != null)
            {
                spiderAura.SetActive(true);
            }
            SetSpiderAura(true);
            ShowMessage(HypnosisSentence);

            // Tentative d'activation de l'enceinte. Si la référence n'est pas assignée,
            // on recherche un Collectible nommé 'enceinte' dans la scène (y compris inactifs).
            if (speakerObject != null)
            {
                Debug.Log($"CombatManager: Activation speakerObject assigné: {speakerObject.name} (actif={speakerObject.activeSelf})", this);
                speakerObject.SetActive(true);
                Debug.Log($"CombatManager: Après SetActive(true): {speakerObject.name} (actif={speakerObject.activeSelf})", this);
            }
            else
            {
                Debug.LogWarning("CombatManager: speakerObject non-assigné. Recherche d'un Collectible 'enceinte' en fallback.", this);
                var allCollectibles = FindObjectsOfType<Collectible>(true);
                Debug.Log($"CombatManager: {allCollectibles.Length} Collectibles trouvés dans la scène.", this);

                // Fallback 1: Recherche par ItemType
                foreach (var c in allCollectibles)
                {
                    if (c == null) continue;
                    try
                    {
                        Debug.Log($"CombatManager: Vérification Collectible: {c.gameObject.name}, ItemType='{c.ItemType}'", this);
                        if (!string.IsNullOrEmpty(c.ItemType) && c.ItemType.Trim().ToLowerInvariant() == "enceinte")
                        {
                            speakerObject = c.gameObject;
                            speakerObject.SetActive(true);
                            Debug.Log($"CombatManager: enceinte trouvée par ItemType et activée: {speakerObject.name} (actif={speakerObject.activeSelf})", this);
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"CombatManager: Erreur lors de la vérification: {ex.Message}", this);
                    }
                }

                // Fallback 2: Recherche par nom si ItemType n'a rien trouvé
                if (speakerObject == null)
                {
                    foreach (var c in allCollectibles)
                    {
                        if (c == null) continue;
                        if (c.gameObject.name.ToLowerInvariant().Contains("enceinte") || 
                            c.gameObject.name.ToLowerInvariant().Contains("speaker") ||
                            c.gameObject.name.ToLowerInvariant().Contains("haut-parleur"))
                        {
                            speakerObject = c.gameObject;
                            speakerObject.SetActive(true);
                            Debug.Log($"CombatManager: enceinte trouvée par nom et activée: {speakerObject.name} (actif={speakerObject.activeSelf})", this);
                            break;
                        }
                    }
                }

                if (speakerObject == null)
                {
                    Debug.LogError("CombatManager: Impossible de trouver l'enceinte par ItemType ou par nom!", this);
                }
            }

            state = ScenarioState.WaitSpeaker;
            CleanupQTEBridge();
            return;
        }

        if (state == ScenarioState.QTE2Running)
        {
            qte2Succeeded = true;
            state = ScenarioState.FadingSpider;
            CleanupQTEBridge();
            ShowMessage(FinalEscapeSentence);
            StartCoroutine(FadeOutSpiderAndFinish());
        }
    }

    public void OnQTEFailed()
    {
        if (state == ScenarioState.QTE1Running)
        {
            state = ScenarioState.WaitReturnForQTE1;
            ShowMessage("QTE 1 raté. Reviens devant l’araignée pour réessayer.");
            CleanupQTEBridge();
            return;
        }

        if (state == ScenarioState.QTE2Running)
        {
            state = ScenarioState.WaitReturnForQTE2;
            ShowMessage("QTE 2 raté. Reviens devant l’araignée pour réessayer.");
            CleanupQTEBridge();
        }
    }

    private IEnumerator FadeOutSpiderAndFinish()
    {
        yield return new WaitForSeconds(2.5f);

        // Si aucun SpriteRenderer dispo, on se contente de désactiver le root.
        if (spiderRoot == null)
        {
            if (spiderAura != null)
            {
                spiderAura.SetActive(false);
            }
            state = ScenarioState.Completed;
            HideMessage();
            yield break;
        }

        if (spiderRenderers == null || spiderRenderers.Length == 0)
        {
            // Pas de renderers trouvés — désactive directement après un petit délai pour que le joueur voie la fin.
            yield return new WaitForSeconds(spiderFadeDuration);
            spiderRoot.SetActive(false);
            if (spiderAura != null)
            {
                spiderAura.SetActive(false);
            }
            HideMessage();
            state = ScenarioState.Completed;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < spiderFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spiderFadeDuration);

            for (int i = 0; i < spiderRenderers.Length; i++)
            {
                if (spiderRenderers[i] == null)
                {
                    continue;
                }

                Color baseColor = spiderOriginalColors[i];
                spiderRenderers[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            }

            yield return null;
        }

        spiderRoot.SetActive(false);
        if (spiderAura != null)
        {
            spiderAura.SetActive(false);
        }
        HideMessage();
        state = ScenarioState.Completed;
    }

    private void CacheSpiderRenderers()
    {
        if (spiderRoot == null)
        {
            spiderRenderers = new SpriteRenderer[0];
            spiderOriginalColors = new Color[0];
            return;
        }

        spiderRenderers = spiderRoot.GetComponentsInChildren<SpriteRenderer>(true);
        spiderOriginalColors = new Color[spiderRenderers.Length];

        for (int i = 0; i < spiderRenderers.Length; i++)
        {
            spiderOriginalColors[i] = spiderRenderers[i] != null ? spiderRenderers[i].color : Color.white;
        }
    }

    private void SetSpiderAura(bool enabled)
    {
        if (spiderRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spiderRenderers.Length; i++)
        {
            if (spiderRenderers[i] == null)
            {
                continue;
            }

            spiderRenderers[i].color = enabled ? spiderAuraColor : spiderOriginalColors[i];
        }
    }

    private void ResetSpiderToNeutralColor()
    {
        if (spiderRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spiderRenderers.Length; i++)
        {
            if (spiderRenderers[i] == null)
            {
                continue;
            }

            spiderOriginalColors[i] = Color.white;
            spiderRenderers[i].color = Color.white;
        }
    }

    private bool IsPlayerNearSpider()
    {
        if (player == null || spiderRoot == null)
        {
            return false;
        }

        Vector2 targetPos = useCustomQTETriggerPosition ? customQTETriggerPosition : (Vector2)spiderRoot.transform.position;
        float distance = Vector2.Distance(player.position, targetPos);
        return distance <= returnToSpiderDistance;
    }

    private void ShowMessage(string content)
    {
        // If messages are not yet allowed, queue the last message to show later
        if (!allowMessages)
        {
            pendingMessage = content;
            Debug.Log("CombatManager: message queued until intro and boss closed", this);
            return;
        }

        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
            EnsurePanelOnTop();
        }

        if (messageText != null)
        {
            messageText.text = content;
        }
    }

    private void HideMessage()
    {
        // Only hide if messages are allowed; otherwise keep it hidden
        if (!allowMessages)
        {
            return;
        }

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    public void NotifyIntroClosed()
    {
        introClosed = true;
        TryEnableMessages();
    }

    public void NotifyBossApparitionClosed()
    {
        bossApparitionClosed = true;
        TryEnableMessages();
    }

    // Force-enable messages immediately (used when CloseTuto is invoked from boss button)
    public void EnableMessagesNow()
    {
        if (allowMessages)
        {
            return;
        }

        allowMessages = true;
        Debug.Log("CombatManager: Messages enabled immediately via EnableMessagesNow()", this);

        if (!string.IsNullOrEmpty(pendingMessage))
        {
            ShowMessage(pendingMessage);
            pendingMessage = null;
        }
    }

    private void TryEnableMessages()
    {
        if (allowMessages)
        {
            return;
        }

        if (introClosed && bossApparitionClosed)
        {
            allowMessages = true;
            // show any pending message
            if (!string.IsNullOrEmpty(pendingMessage))
            {
                ShowMessage(pendingMessage);
                pendingMessage = null;
            }
        }
    }

    private void CleanupQTEBridge()
    {
        if (activeBridge == null)
        {
            return;
        }

        activeBridge.SequenceSucceeded -= OnQTESucceeded;
        activeBridge.SequenceFailed -= OnQTEFailed;
        activeBridge = null;
    }

    private void OnDestroy()
    {
        if (runningFlow != null)
        {
            StopCoroutine(runningFlow);
        }

        CleanupQTEBridge();
    }
}
