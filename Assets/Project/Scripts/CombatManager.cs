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
    [SerializeField] private GameObject lanternObject;
    [SerializeField] private GameObject speakerObject;

    [Header("UI message")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Vector2 messagePanelSize = new Vector2(1050f, 220f);
    [SerializeField] private Vector2 messagePanelAnchoredPosition = new Vector2(0f, -230f);

    [Header("QTE")]
    [SerializeField] private GameObject qtePrefab;
    [SerializeField] private Transform qteSpawnPoint;

    [Header("Tuning")]
    [SerializeField] private float introMessageDuration = 6f;
    [SerializeField] private float returnToSpiderDistance = 8f;
    [SerializeField] private float spiderFadeDuration = 1.2f;
    [SerializeField] private Color spiderAuraColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private bool resetSpiderTintOnStart = true;

    private ScenarioState state;
    private bool lanternCollected;
    private bool speakerCollected;
    private bool qte1Succeeded;
    private bool qte2Succeeded;

    private QTECombatBridge activeBridge;
    private Coroutine runningFlow;
    private SpriteRenderer[] spiderRenderers;
    private Color[] spiderOriginalColors;

    private const string IntroSentence = "Le numérique ? Trop difficile pour toi. De toute façon tu te marieras, tu auras des enfants, et puis ce n’est pas un métier assez élégant pour une fille…";
    private const string HypnosisSentence = "La mère Maléfique utilise un pouvoir hypnotique !";

    private void Awake()
    {
        CacheSpiderRenderers();
    }

    private void Start()
    {
        state = ScenarioState.Intro;
        lanternCollected = false;
        speakerCollected = false;
        qte1Succeeded = false;
        qte2Succeeded = false;

        if (lanternObject != null)
        {
            lanternObject.SetActive(false);
        }

        if (speakerObject != null)
        {
            speakerObject.SetActive(false);
        }

        EnsureMessageUI();

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        if (resetSpiderTintOnStart)
        {
            ResetSpiderToNeutralColor();
        }
        else
        {
            SetSpiderAura(false);
        }

        ShowMessage(IntroSentence);
        runningFlow = StartCoroutine(BeginScenario());
    }

    private void EnsureMessageUI()
    {
        if (messagePanel != null && messageText != null)
        {
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
        messagePanel.SetActive(false);
    }

    private void Update()
    {
        if (state == ScenarioState.WaitReturnForQTE1 && lanternCollected && IsPlayerNearSpider())
        {
            StartQTEPhase(1);
        }

        if (state == ScenarioState.WaitReturnForQTE2 && speakerCollected && IsPlayerNearSpider())
        {
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
                ShowMessage("Retourne devant l’araignée pour lancer le QTE 1.");
            }

            return;
        }

        if (normalizedType == "enceinte" && !speakerCollected && state == ScenarioState.WaitSpeaker)
        {
            speakerCollected = true;
            state = ScenarioState.WaitReturnForQTE2;
            ShowMessage("Retourne devant l’araignée pour lancer le QTE 2.");
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
        if (qtePrefab == null)
        {
            Debug.LogWarning("CombatManager: qtePrefab non assigné.", this);
            return;
        }

        if (activeBridge != null)
        {
            return;
        }

        state = phase == 1 ? ScenarioState.QTE1Running : ScenarioState.QTE2Running;

        Vector3 spawnPosition = qteSpawnPoint != null ? qteSpawnPoint.position : transform.position;
        GameObject qteInstance = Instantiate(qtePrefab, spawnPosition, Quaternion.identity);

        MobileQTESequence sequence = qteInstance.GetComponent<MobileQTESequence>();
        if (sequence != null)
        {
            int phaseStepIndex = phase == 1 ? 0 : 1;
            sequence.ConfigureSingleStep(phaseStepIndex, true);
        }

        activeBridge = qteInstance.GetComponent<QTECombatBridge>();
        if (activeBridge != null)
        {
            activeBridge.SequenceSucceeded += OnQTESucceeded;
            activeBridge.SequenceFailed += OnQTEFailed;
        }
    }

    private void OnQTESucceeded()
    {
        if (state == ScenarioState.QTE1Running)
        {
            qte1Succeeded = true;
            SetSpiderAura(true);
            ShowMessage(HypnosisSentence);

            if (speakerObject != null)
            {
                speakerObject.SetActive(true);
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
            StartCoroutine(FadeOutSpiderAndFinish());
        }
    }

    private void OnQTEFailed()
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
        if (spiderRoot == null || spiderRenderers == null || spiderRenderers.Length == 0)
        {
            state = ScenarioState.Completed;
            HideMessage();
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

        float distance = Vector2.Distance(player.position, spiderRoot.transform.position);
        return distance <= returnToSpiderDistance;
    }

    private void ShowMessage(string content)
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = content;
        }
    }

    private void HideMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
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
