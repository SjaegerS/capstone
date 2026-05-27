using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GachaChestAnimation : MonoBehaviour
{
    [Header("Chest Sprites")]
    [SerializeField] private Sprite closedChestSprite;
    [SerializeField] private Sprite openChestSprite;
    [SerializeField] private Sprite lightSprite;

    [Header("Equipment Pools")]
    [SerializeField] private Sprite defaultEquipmentSprite;
    [SerializeField] private Sprite[] normalEquipmentSprites;
    [SerializeField] private Sprite[] rareEquipmentSprites;
    [SerializeField] private Sprite[] superRareEquipmentSprites;

    [Header("Gem Cost")]
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private int onePullGemCost = 100;
    [SerializeField] private int elevenPullGemCost = 1000;
    [SerializeField] private int fiftyFivePullGemCost = 5000;

    [Header("Rates")]
    [SerializeField, Range(0f, 1f)] private float normalRate = 0.8f;
    [SerializeField, Range(0f, 1f)] private float rareRate = 0.17f;

    [Header("Timing")]
    [SerializeField] private float shakeDuration = 1.15f;
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float lightDuration = 0.8f;
    [SerializeField] private float finishDelay = 0.35f;

    [Header("Look")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.86f);
    [SerializeField] private Color lightColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color normalLightColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color rareLightColor = new Color(0.15f, 1f, 0.35f, 0.95f);
    [SerializeField] private Color superRareLightColor = new Color(1f, 0.12f, 0.08f, 0.95f);
    [SerializeField] private Vector2 closedChestSize = new Vector2(410f, 365f);
    [SerializeField] private Vector2 openChestSize = new Vector2(420f, 445f);
    [SerializeField] private Vector2 innerLightSize = new Vector2(980f, 980f);
    [SerializeField] private Vector2 innerLightPosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 resultStartPosition = new Vector2(0f, -40f);

    private enum EquipmentRarity
    {
        Normal,
        Rare,
        SuperRare
    }

    private struct GachaResult
    {
        public Sprite Sprite;
        public EquipmentRarity Rarity;
    }

    private bool isPlaying;
    private bool skipRequested;
    private float previousTimeScale = 1f;
    private Button[] gachaButtons;
    private readonly Dictionary<Button, UnityAction> buttonListeners = new Dictionary<Button, UnityAction>();
    private GameObject overlayObject;

    private void Awake()
    {
        AutoBindGemText();
        gachaButtons = GetComponentsInChildren<Button>(true);

        foreach (Button button in gachaButtons)
        {
            Button capturedButton = button;
            UnityAction listener = () => Play(GetPullCount(capturedButton));
            buttonListeners[button] = listener;
            button.onClick.AddListener(listener);
        }
    }

    private void Start()
    {
        EquipmentInventoryView.RefreshAll();
    }

    private void OnDestroy()
    {
        CleanupPlayback();

        if (gachaButtons == null)
        {
            return;
        }

        foreach (Button button in gachaButtons)
        {
            if (button != null && buttonListeners.TryGetValue(button, out UnityAction listener))
            {
                button.onClick.RemoveListener(listener);
            }
        }

        buttonListeners.Clear();
    }

    private void OnDisable()
    {
        CleanupPlayback();
    }

    public void Play()
    {
        Play(1);
    }

    public void Play(int pullCount)
    {
        if (!isActiveAndEnabled || isPlaying)
        {
            return;
        }

        int safePullCount = Mathf.Max(1, pullCount);
        if (!TrySpendGems(GetGemCost(safePullCount)))
        {
            return;
        }

        StartCoroutine(PlayRoutine(safePullCount));
    }

    private IEnumerator PlayRoutine(int pullCount)
    {
        isPlaying = true;
        skipRequested = false;
        SetButtonsInteractable(false);
        GachaResult[] results = RollResults(pullCount);
        AddResultsToInventory(results);
        EquipmentInventoryView.RefreshAll();
        RefreshBattlePlayerStats();
        lightColor = GetLightColor(GetHighestRarity(results));

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        RectTransform overlay = CreateOverlay();
        Image dimImage = overlay.GetComponent<Image>();

        RectTransform lightMask = CreateMask("ScreenLightMask", overlay, GetScreenLightSize(overlay), Vector2.zero);
        RectTransform light = CreateImage("ChestScreenLight", lightMask, lightSprite, GetScreenLightSize(overlay) * 1.25f, innerLightPosition, true);
        RectTransform chestRoot = CreateContainer("ChestRoot", overlay, openChestSize, Vector2.zero);
        RectTransform closedChest = CreateImage("ClosedChest", chestRoot, closedChestSprite, closedChestSize, Vector2.zero, true);
        RectTransform openChest = CreateImage("OpenChest", chestRoot, openChestSprite, openChestSize, Vector2.zero, true);

        CanvasGroup closedGroup = closedChest.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup openGroup = openChest.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup lightGroup = lightMask.gameObject.AddComponent<CanvasGroup>();

        lightGroup.alpha = 0f;
        openGroup.alpha = 0f;

        yield return FadeDim(dimImage, 0f, dimColor.a, 0.16f);
        yield return ShakeChest(chestRoot);
        yield return OpenChest(closedGroup, openGroup, openChest, light, lightGroup);
        yield return GlowLight(light, lightGroup);
        yield return RevealResults(overlay, results);
        skipRequested = false;
        yield return WaitRealtime(finishDelay);

        Destroy(overlayObject);
        overlayObject = null;

        Time.timeScale = previousTimeScale;
        SetButtonsInteractable(true);
        isPlaying = false;
    }

    private RectTransform CreateOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.root;

        overlayObject = new GameObject("GachaChestAnimationOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        overlayObject.transform.SetParent(parent, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlayObject.GetComponent<Image>();
        image.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
        image.raycastTarget = true;

        Button skipButton = overlayObject.GetComponent<Button>();
        skipButton.transition = Selectable.Transition.None;
        skipButton.onClick.AddListener(RequestSkip);

        return rect;
    }

    private void RequestSkip()
    {
        if (isPlaying)
        {
            skipRequested = true;
        }
    }

    private IEnumerator RevealResults(RectTransform overlay, GachaResult[] results)
    {
        RectTransform resultRoot = CreateContainer("GachaResultRoot", overlay, GetResultRootSize(overlay, results.Length), resultStartPosition);
        GridLayoutGroup grid = resultRoot.gameObject.AddComponent<GridLayoutGroup>();
        int columns = GetResultColumns(results.Length);
        float gap = 18f;
        float cellSize = GetResultCellSize(resultRoot.sizeDelta, columns, gap, results.Length);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(gap, gap);
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < results.Length; i++)
        {
            RectTransform slot = CreateResultSlot(resultRoot, results[i], cellSize);
            CanvasGroup slotGroup = slot.gameObject.AddComponent<CanvasGroup>();
            slotGroup.alpha = skipRequested ? 1f : 0f;
            slot.localScale = skipRequested ? Vector3.one : Vector3.one * 0.55f;

            if (skipRequested)
            {
                continue;
            }

            float elapsed = 0f;
            const float revealDuration = 0.08f;
            while (elapsed < revealDuration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(elapsed / revealDuration));
                slotGroup.alpha = t;
                slot.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, t);
                yield return null;
            }

            slotGroup.alpha = 1f;
            slot.localScale = Vector3.one;
            yield return WaitRealtime(results.Length > 11 ? 0.025f : 0.06f);

            if (skipRequested)
            {
                ShowRemainingResults(resultRoot, results, i + 1, cellSize);
                yield break;
            }
        }
    }

    private RectTransform CreateResultSlot(Transform parent, GachaResult result, float size)
    {
        RectTransform slot = CreateImage("EquipmentResult", parent, null, new Vector2(size, size), Vector2.zero, false);
        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = GetSlotColor(result.Rarity);

        Sprite equipmentSprite = EnsureResultSprite(result);
        RectTransform icon = CreateImage("EquipmentIcon", slot, equipmentSprite, new Vector2(size * 0.9f, size * 0.9f), Vector2.zero, true);
        Image iconImage = icon.GetComponent<Image>();
        iconImage.color = Color.white;
        iconImage.enabled = equipmentSprite != null;
        icon.SetAsLastSibling();

        return slot;
    }

    private Sprite EnsureResultSprite(GachaResult result)
    {
        if (result.Sprite != null)
        {
            return result.Sprite;
        }

        Sprite fallback = PickEquipmentSprite(result.Rarity);
        if (fallback != null)
        {
            return fallback;
        }

        fallback = PickFirstAvailableEquipmentSprite();
        return fallback != null ? fallback : defaultEquipmentSprite;
    }

    private void ShowRemainingResults(Transform parent, GachaResult[] results, int startIndex, float cellSize)
    {
        for (int i = startIndex; i < results.Length; i++)
        {
            RectTransform slot = CreateResultSlot(parent, results[i], cellSize);
            CanvasGroup slotGroup = slot.gameObject.AddComponent<CanvasGroup>();
            slotGroup.alpha = 1f;
            slot.localScale = Vector3.one;
        }
    }

    private Vector2 GetResultRootSize(RectTransform overlay, int resultCount)
    {
        int columns = GetResultColumns(resultCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)resultCount / columns));
        float width = Mathf.Max(overlay.rect.width * 0.96f, 420f);
        float height = Mathf.Min(overlay.rect.height * 0.86f, rows * GetLargeResultCellSize(overlay.rect.width, columns, 18f) + (rows - 1) * 18f);
        return new Vector2(width, height);
    }

    private int GetResultColumns(int resultCount)
    {
        if (resultCount <= 1)
        {
            return 1;
        }

        if (resultCount >= 55)
        {
            return 8;
        }

        return Mathf.Min(resultCount, 5);
    }

    private float GetResultCellSize(Vector2 rootSize, int columns, float gap, int resultCount)
    {
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)resultCount / columns));
        float widthSize = (rootSize.x - gap * (columns - 1)) / columns;
        float heightSize = (rootSize.y - gap * (rows - 1)) / rows;
        return Mathf.Clamp(Mathf.Min(widthSize, heightSize), 52f, 170f);
    }

    private float GetLargeResultCellSize(float overlayWidth, int columns, float gap)
    {
        float rootWidth = Mathf.Max(overlayWidth * 0.96f, 420f);
        return Mathf.Clamp((rootWidth - gap * (columns - 1)) / columns, 90f, 170f);
    }

    private Color GetSlotColor(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.SuperRare:
                return new Color(1f, 0.12f, 0.08f, 0.82f);
            case EquipmentRarity.Rare:
                return new Color(0.1f, 0.85f, 0.28f, 0.82f);
            default:
                return new Color(1f, 1f, 1f, 0.78f);
        }
    }

    private RectTransform CreateImage(string objectName, Transform parent, Sprite sprite, Vector2 size, Vector2 anchoredPosition, bool preserveAspect)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        image.color = sprite == lightSprite ? lightColor : Color.white;

        return rect;
    }

    private RectTransform CreateMask(string objectName, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject maskObject = new GameObject(objectName, typeof(RectTransform), typeof(RectMask2D));
        maskObject.transform.SetParent(parent, false);

        RectTransform rect = maskObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        return rect;
    }

    private Vector2 GetScreenLightSize(RectTransform overlay)
    {
        float size = Mathf.Max(Mathf.Max(overlay.rect.width, overlay.rect.height), Mathf.Max(innerLightSize.x, innerLightSize.y));
        return new Vector2(size, size);
    }

    private RectTransform CreateContainer(string objectName, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform));
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        return rect;
    }

    private IEnumerator ShakeChest(RectTransform chestRoot)
    {
        Vector2 startPosition = chestRoot.anchoredPosition;
        Vector3 startScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < shakeDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / shakeDuration);
            float strength = Mathf.Lerp(20f, 5f, t);
            float speed = Mathf.Lerp(42f, 72f, t);
            float x = Mathf.Sin(elapsed * speed) * strength;
            float y = Mathf.Abs(Mathf.Sin(elapsed * speed * 0.5f)) * Mathf.Lerp(8f, 18f, t);
            float angle = Mathf.Sin(elapsed * speed * 0.75f) * Mathf.Lerp(8f, 3f, t);

            chestRoot.anchoredPosition = startPosition + new Vector2(x, y);
            chestRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            chestRoot.localScale = startScale * (1f + Mathf.Sin(elapsed * 18f) * 0.025f);

            yield return null;
        }

        chestRoot.anchoredPosition = startPosition;
        chestRoot.localRotation = Quaternion.identity;
        chestRoot.localScale = startScale;
    }

    private IEnumerator OpenChest(CanvasGroup closedGroup, CanvasGroup openGroup, RectTransform openChest, RectTransform light, CanvasGroup lightGroup)
    {
        Vector3 lightStartScale = Vector3.one * 0.25f;
        Vector3 lightEndScale = Vector3.one * 0.95f;
        Vector3 openStartScale = Vector3.one * 0.88f;

        float elapsed = 0f;
        openChest.localScale = openStartScale;
        light.localScale = lightStartScale;

        while (elapsed < openDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / openDuration));

            closedGroup.alpha = 1f - t;
            openGroup.alpha = t;
            openChest.localScale = Vector3.LerpUnclamped(openStartScale, Vector3.one, t);
            lightGroup.alpha = Mathf.Lerp(0f, 0.8f, t);
            light.localScale = Vector3.LerpUnclamped(lightStartScale, lightEndScale, t);

            yield return null;
        }

        closedGroup.alpha = 0f;
        openGroup.alpha = 1f;
        openChest.localScale = Vector3.one;
        lightGroup.alpha = 0.8f;
        light.localScale = lightEndScale;
    }

    private IEnumerator GlowLight(RectTransform light, CanvasGroup lightGroup)
    {
        float elapsed = 0f;

        while (elapsed < lightDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / lightDuration);
            float pulse = Mathf.Sin(elapsed * 14f) * 0.08f;

            lightGroup.alpha = Mathf.Lerp(0.78f, 1f, Mathf.Sin(t * Mathf.PI)) + pulse;
            light.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.28f, t);
            light.localRotation = Quaternion.Euler(0f, 0f, elapsed * 10f);

            yield return null;
        }

        lightGroup.alpha = 0.95f;
    }

    private IEnumerator FadeDim(Image image, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            image.color = new Color(dimColor.r, dimColor.g, dimColor.b, Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        image.color = new Color(dimColor.r, dimColor.g, dimColor.b, toAlpha);
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private GachaResult[] RollResults(int pullCount)
    {
        GachaResult[] results = new GachaResult[pullCount];

        for (int i = 0; i < results.Length; i++)
        {
            EquipmentRarity rarity = RollRarity();
            results[i] = new GachaResult
            {
                Rarity = rarity,
                Sprite = PickEquipmentSprite(rarity)
            };
        }

        return results;
    }

    private void AddResultsToInventory(GachaResult[] results)
    {
        foreach (GachaResult result in results)
        {
            EquipmentInventory.Add(EnsureResultSprite(result), ToInventoryRarity(result.Rarity));
        }
    }

    private EquipmentRarityGrade ToInventoryRarity(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.SuperRare:
                return EquipmentRarityGrade.SuperRare;
            case EquipmentRarity.Rare:
                return EquipmentRarityGrade.Rare;
            default:
                return EquipmentRarityGrade.Normal;
        }
    }

    private void RefreshBattlePlayerStats()
    {
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.RefreshPlayerStats();
        }
    }

    private EquipmentRarity RollRarity()
    {
        float roll = Random.value;

        if (roll < normalRate)
        {
            return EquipmentRarity.Normal;
        }

        if (roll < normalRate + rareRate)
        {
            return EquipmentRarity.Rare;
        }

        return EquipmentRarity.SuperRare;
    }

    private Sprite PickEquipmentSprite(EquipmentRarity rarity)
    {
        Sprite[] pool = GetPool(rarity);

        if (pool.Length == 0)
        {
            pool = GetPool(EquipmentRarity.Normal);
        }

        if (pool.Length == 0)
        {
            return null;
        }

        return pool[Random.Range(0, pool.Length)];
    }

    private Sprite PickFirstAvailableEquipmentSprite()
    {
        Sprite[][] pools =
        {
            normalEquipmentSprites,
            rareEquipmentSprites,
            superRareEquipmentSprites
        };

        foreach (Sprite[] pool in pools)
        {
            if (pool != null && pool.Length > 0)
            {
                return pool[0];
            }
        }

        return null;
    }

    private Sprite[] GetPool(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.SuperRare:
                return superRareEquipmentSprites ?? new Sprite[0];
            case EquipmentRarity.Rare:
                return rareEquipmentSprites ?? new Sprite[0];
            default:
                return normalEquipmentSprites ?? new Sprite[0];
        }
    }

    private EquipmentRarity GetHighestRarity(GachaResult[] results)
    {
        EquipmentRarity highest = EquipmentRarity.Normal;

        foreach (GachaResult result in results)
        {
            if (result.Rarity > highest)
            {
                highest = result.Rarity;
            }
        }

        return highest;
    }

    private Color GetLightColor(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.SuperRare:
                return superRareLightColor;
            case EquipmentRarity.Rare:
                return rareLightColor;
            default:
                return normalLightColor;
        }
    }

    private int GetGemCost(int pullCount)
    {
        if (pullCount >= 55)
        {
            return fiftyFivePullGemCost;
        }

        if (pullCount >= 11)
        {
            return elevenPullGemCost;
        }

        return onePullGemCost;
    }

    private bool TrySpendGems(int cost)
    {
        AutoBindGemText();
        if (gemText == null)
        {
            Debug.LogWarning("GemUI text was not found. Gacha cannot spend gems.");
            return false;
        }

        int currentGems = GetGemAmount();
        if (currentGems < cost)
        {
            Debug.Log("Not enough gems for gacha.");
            return false;
        }

        SetGemAmount(currentGems - cost);
        return true;
    }

    private int GetGemAmount()
    {
        if (gemText == null)
        {
            return 0;
        }

        string digits = string.Empty;
        bool isInsideTag = false;
        foreach (char character in gemText.text)
        {
            if (character == '<')
            {
                isInsideTag = true;
                continue;
            }

            if (character == '>')
            {
                isInsideTag = false;
                continue;
            }

            if (!isInsideTag && char.IsDigit(character))
            {
                digits += character;
            }
        }

        if (int.TryParse(digits, out int amount))
        {
            return amount;
        }

        return 0;
    }

    private void SetGemAmount(int amount)
    {
        if (gemText != null)
        {
            gemText.text = Mathf.Max(0, amount).ToString();
        }
    }

    private void AutoBindGemText()
    {
        if (gemText != null)
        {
            return;
        }

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && HasNameInHierarchy(text.transform, "GemUI"))
            {
                gemText = text;
                return;
            }
        }

        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && HasNameInHierarchy(text.transform, "Gem"))
            {
                gemText = text;
                return;
            }
        }
    }

    private bool HasNameInHierarchy(Transform target, string namePart)
    {
        while (target != null)
        {
            if (target.name.Contains(namePart))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private int GetPullCount(Button button)
    {
        if (button == null)
        {
            return 1;
        }

        List<Button> orderedButtons = new List<Button>(gachaButtons);
        orderedButtons.Sort((left, right) => GetButtonY(right).CompareTo(GetButtonY(left)));

        int index = orderedButtons.IndexOf(button);
        if (index == 1)
        {
            return 11;
        }

        if (index == 2)
        {
            return 55;
        }

        return 1;
    }

    private float GetButtonY(Button button)
    {
        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
        {
            return button.transform.position.y;
        }

        return rectTransform.position.y;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (gachaButtons == null)
        {
            return;
        }

        foreach (Button button in gachaButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void CleanupPlayback()
    {
        if (!isPlaying)
        {
            return;
        }

        StopAllCoroutines();

        if (overlayObject != null)
        {
            Destroy(overlayObject);
            overlayObject = null;
        }

        Time.timeScale = previousTimeScale;
        SetButtonsInteractable(true);
        isPlaying = false;
    }
}
