using System;
using System.Text;
using UnityEngine.Networking;
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

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";
    [SerializeField] private BattleRewardApi battleRewardApi;

    [Header("Gacha Type")]
    [SerializeField] private GachaItemType targetItemType = GachaItemType.Weapon;

    private const string USER_ID_KEY = "USER_ID";

    private ItemDto[] cachedItems;

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

    private enum GachaItemType
    {
        Weapon,
        Armor
    }

    private struct GachaResult
    {
        public Sprite Sprite;
        public EquipmentRarity Rarity;

        public long ItemId;
        public string ItemName;
        public string ItemType;
        public string Grade;

        public long UserItemId;
        public int EnhanceLevel;
        public int Quantity;
        public bool IsEquipped;
    }

    private bool isPlaying;
    private bool skipRequested;
    private float previousTimeScale = 1f;
    private Button[] gachaButtons;
    private readonly Dictionary<Button, UnityAction> buttonListeners = new Dictionary<Button, UnityAction>();
    private GameObject overlayObject;

    private void Awake()
    {
        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();

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
        StartCoroutine(LoadCurrencyWhenUserReady());
    }

    private void OnDestroy()
    {
        CleanupPlayback();

        if (gachaButtons == null)
            return;

        foreach (Button button in gachaButtons)
        {
            if (button != null && buttonListeners.TryGetValue(button, out UnityAction listener))
                button.onClick.RemoveListener(listener);
        }

        buttonListeners.Clear();
    }

    private void OnDisable()
    {
        CleanupPlayback();
    }

    private long GetUserId()
    {
        if (battleRewardApi != null)
        {
            int apiUserId = battleRewardApi.GetUserId();

            if (apiUserId > 0)
            {
                CurrentUser.UserId = apiUserId;
                return apiUserId;
            }
        }

        if (CurrentUser.UserId > 0)
            return CurrentUser.UserId;

        int savedUserId = PlayerPrefs.GetInt(USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            CurrentUser.UserId = savedUserId;
            return savedUserId;
        }

        return -1;
    }

    public void Play()
    {
        Play(1);
    }

    public void Play(int pullCount)
    {
        if (!isActiveAndEnabled || isPlaying)
            return;

        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[Gacha] USER_ID가 없습니다. TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다.");
            return;
        }

        int safePullCount = Mathf.Max(1, pullCount);
        StartCoroutine(SpendGemsThenPlay(safePullCount));
    }

    private IEnumerator SpendGemsThenPlay(int pullCount)
    {
        if (isPlaying)
            yield break;

        isPlaying = true;
        skipRequested = false;
        SetButtonsInteractable(false);

        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[Gacha] gem 차감 실패: USER_ID가 없습니다.");
            SetButtonsInteractable(true);
            isPlaying = false;
            yield break;
        }

        int cost = GetGemCost(pullCount);
        string url = $"{baseUrl}/users/{userId}/currency/spend-gem/";

        SpendCurrencyRequest requestBody = new SpendCurrencyRequest
        {
            amount = cost
        };

        string json = JsonUtility.ToJson(requestBody);

        UnityWebRequest request = new UnityWebRequest(url, "PATCH");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("gem 차감 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");

            SetButtonsInteractable(true);
            isPlaying = false;
            yield break;
        }

        CurrencyResponse response = JsonUtility.FromJson<CurrencyResponse>(request.downloadHandler.text);

        if (response != null && CurrencyUIManager.Instance != null)
            CurrencyUIManager.Instance.SetGem(response.gem);

        yield return StartCoroutine(PlayRoutine(pullCount));
    }

    private IEnumerator PlayRoutine(int pullCount)
    {
        yield return StartCoroutine(LoadItemsIfNeeded());

        GachaResult[] results = RollResults(pullCount);

        yield return StartCoroutine(SaveResultsToDb(results));
        yield return StartCoroutine(SyncUserItemsFromDb());

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

    private GachaResult[] RollResults(int pullCount)
    {
        GachaResult[] results = new GachaResult[pullCount];

        for (int i = 0; i < results.Length; i++)
        {
            EquipmentRarity rarity = RollRarity();
            ItemDto selectedItem = PickItemByRarity(rarity);

            Sprite matchedSprite = selectedItem != null
                ? FindSpriteByItem(selectedItem, rarity)
                : PickEquipmentSprite(rarity);

            results[i] = new GachaResult
            {
                Rarity = rarity,
                Sprite = matchedSprite,
                ItemId = selectedItem != null ? selectedItem.item_id : 0,
                ItemName = selectedItem != null ? selectedItem.item_name : "",
                ItemType = selectedItem != null ? selectedItem.item_type : "",
                Grade = selectedItem != null ? selectedItem.grade : "",
                UserItemId = 0,
                EnhanceLevel = 0,
                Quantity = 0,
                IsEquipped = false
            };
        }

        return results;
    }

    private EquipmentRarity RollRarity()
    {
        float roll = UnityEngine.Random.value;

        if (roll < normalRate)
            return EquipmentRarity.Normal;

        if (roll < normalRate + rareRate)
            return EquipmentRarity.Rare;

        return EquipmentRarity.SuperRare;
    }

    private IEnumerator SaveResultsToDb(GachaResult[] results)
    {
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].ItemId <= 0)
            {
                Debug.LogError("[Gacha] item_id is 0. Skipping user_item save.");
                continue;
            }

            UserItemResponse savedItem = null;

            yield return StartCoroutine(CreateUserItem(results[i].ItemId, response =>
            {
                savedItem = response;
            }));

            if (savedItem == null)
            {
                Debug.LogError($"[Gacha] savedItem이 null입니다. index={i}");
                continue;
            }

            results[i].UserItemId = savedItem.user_item_id;
            results[i].EnhanceLevel = savedItem.enhance_level;
            results[i].IsEquipped = savedItem.is_equipped;
            results[i].Quantity = savedItem.quantity;
            results[i].ItemId = savedItem.item_id;
        }
    }

    private IEnumerator CreateUserItem(long itemId, Action<UserItemResponse> onSuccess)
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[Gacha] user_item 저장 실패: USER_ID가 없습니다.");
            yield break;
        }

        string url = $"{baseUrl}/user-items/";

        UserItemCreateRequest requestBody = new UserItemCreateRequest
        {
            user_id = userId,
            item_id = itemId,
            enhance_level = 1,
            is_equipped = false,
            quantity = 1
        };

        string json = JsonUtility.ToJson(requestBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to save user_item.");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        UserItemResponse response = JsonUtility.FromJson<UserItemResponse>(request.downloadHandler.text);
        onSuccess?.Invoke(response);
    }

    private IEnumerator LoadCurrencyWhenUserReady()
    {
        float timeout = 5f;
        float elapsed = 0f;

        long userId = GetUserId();

        while (userId <= 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            userId = GetUserId();
            yield return null;
        }

        if (userId <= 0)
        {
            Debug.LogWarning("[Gacha] 유저 ID가 준비되지 않아 currency를 불러오지 못했습니다.");
            yield break;
        }

        yield return StartCoroutine(LoadCurrencyFromDb());
    }

    private IEnumerator LoadCurrencyFromDb()
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[Gacha] currency 조회 실패: USER_ID가 없습니다.");
            yield break;
        }

        string url = $"{baseUrl}/users/{userId}/currency/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("currency 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        CurrencyResponse response = JsonUtility.FromJson<CurrencyResponse>(request.downloadHandler.text);

        if (response != null)
            SetGemAmount(response.gem);
    }

    private void SetGemAmount(long amount)
    {
        if (gemText == null)
            return;

        gemText.textWrappingMode = TextWrappingModes.NoWrap;
        gemText.overflowMode = TextOverflowModes.Overflow;
        gemText.alignment = TextAlignmentOptions.Center;
        gemText.text = Math.Max(0L, amount).ToString();
    }

    private int GetGemCost(int pullCount)
    {
        if (pullCount >= 55)
            return fiftyFivePullGemCost;

        if (pullCount >= 11)
            return elevenPullGemCost;

        return onePullGemCost;
    }

    private IEnumerator LoadItemsIfNeeded()
    {
        if (cachedItems != null && cachedItems.Length > 0)
            yield break;

        string url = $"{baseUrl}/items/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("아이템 목록 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        string wrappedJson = "{\"items\":" + request.downloadHandler.text + "}";
        ItemListResponse response = JsonUtility.FromJson<ItemListResponse>(wrappedJson);

        cachedItems = response.items;
    }

    private ItemDto PickItemByRarity(EquipmentRarity rarity)
    {
        if (cachedItems == null || cachedItems.Length == 0)
        {
            Debug.LogError("[Gacha] cachedItems가 비어 있습니다.");
            return null;
        }

        string grade = ToDbGrade(rarity);
        string targetDbItemType = ToDbItemType(targetItemType);

        List<ItemDto> candidates = new List<ItemDto>();

        foreach (ItemDto item in cachedItems)
        {
            if (item == null)
                continue;

            string itemGrade = item.grade != null ? item.grade.Trim().ToUpperInvariant() : "";
            string itemType = item.item_type != null ? item.item_type.Trim().ToUpperInvariant() : "";

            bool gradeMatches = itemGrade == grade;
            bool typeMatches = itemType == targetDbItemType;

            if (gradeMatches && typeMatches)
            {
                candidates.Add(item);
            }
        }

        Debug.Log(
            $"[Gacha] 후보 검색 결과: " +
            $"TargetItemType={targetItemType}, " +
            $"TargetDbType={targetDbItemType}, " +
            $"Grade={grade}, " +
            $"CandidateCount={candidates.Count}"
        );

        if (candidates.Count == 0)
        {
            Debug.LogError(
                $"[Gacha] 조건에 맞는 아이템이 없습니다. " +
                $"grade={grade}, item_type={targetDbItemType}"
            );

            return null;
        }

        ItemDto pickedItem = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        Debug.Log(
            $"[Gacha] 선택된 아이템: " +
            $"TargetType={targetItemType}, " +
            $"DBType={pickedItem.item_type}, " +
            $"Grade={pickedItem.grade}, " +
            $"ItemId={pickedItem.item_id}, " +
            $"Name={pickedItem.item_name}, " +
            $"ImageKey={pickedItem.image_key}"
        );

        return pickedItem;
    }

    private IEnumerator SyncUserItemsFromDb()
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[Gacha] user_item 동기화 실패: USER_ID가 없습니다.");
            yield break;
        }

        string url = $"{baseUrl}/users/{userId}/items/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("user_item 목록 동기화 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        string wrappedJson = "{\"items\":" + request.downloadHandler.text + "}";
        UserItemListResponse response = JsonUtility.FromJson<UserItemListResponse>(wrappedJson);

        if (response == null || response.items == null)
            yield break;

        EquipmentInventory.ResetAll();

        foreach (UserItemResponse userItem in response.items)
        {
            ItemDto item = FindCachedItemById(userItem.item_id);

            if (item == null)
                continue;

            EquipmentRarity rarity = ToEquipmentRarity(item.grade);
            Sprite sprite = FindSpriteByItem(item, rarity);

            if (sprite == null)
                continue;

            EquipmentInventory.ApplyServerUserItem(
                sprite,
                (int)userItem.user_item_id,
                (int)userItem.item_id,
                userItem.quantity,
                userItem.enhance_level
            );

            EquipmentInventoryRecord record = EquipmentInventory.GetRecord(sprite);
            record.SetMetadata(
                GetInventoryCategoryFromItemType(item.item_type, sprite),
                ToInventoryRarity(rarity)
            );
        }

        PlayerPrefs.Save();
    }

    private ItemDto FindCachedItemById(long itemId)
    {
        if (cachedItems == null)
            return null;

        foreach (ItemDto item in cachedItems)
        {
            if (item != null && item.item_id == itemId)
                return item;
        }

        return null;
    }

    private Sprite FindSpriteByItem(ItemDto item, EquipmentRarity rarity)
    {
        if (item == null)
            return PickEquipmentSprite(rarity);

        Sprite[] pool = GetPool(rarity);

        foreach (Sprite sprite in pool)
        {
            if (sprite == null)
                continue;

            if (!string.IsNullOrEmpty(item.image_key) && sprite.name == item.image_key)
                return sprite;

            if (!string.IsNullOrEmpty(item.item_key) && sprite.name == item.item_key)
                return sprite;

            if (!string.IsNullOrEmpty(item.item_name) && sprite.name == item.item_name)
                return sprite;
        }

        Debug.LogWarning(
            $"Sprite 매칭 실패: item_id={item.item_id}, " +
            $"item_key={item.item_key}, image_key={item.image_key}, item_name={item.item_name}. " +
            "등급 풀에서 임시 스프라이트를 사용합니다."
        );

        return PickEquipmentSprite(rarity);
    }

    private Sprite PickEquipmentSprite(EquipmentRarity rarity)
    {
        Sprite[] pool = GetPool(rarity);

        if (pool.Length == 0)
            pool = GetPool(EquipmentRarity.Normal);

        if (pool.Length == 0)
            return null;

        return pool[UnityEngine.Random.Range(0, pool.Length)];
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
                return pool[0];
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

    private Sprite EnsureResultSprite(GachaResult result)
    {
        if (result.Sprite != null)
            return result.Sprite;

        Sprite fallback = PickEquipmentSprite(result.Rarity);

        if (fallback != null)
            return fallback;

        fallback = PickFirstAvailableEquipmentSprite();
        return fallback != null ? fallback : defaultEquipmentSprite;
    }

    private string ToDbGrade(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.SuperRare:
                return "SUPER_RARE";

            case EquipmentRarity.Rare:
                return "RARE";

            default:
                return "NORMAL";
        }
    }

    private string ToDbItemType(GachaItemType type)
    {
        switch (type)
        {
            case GachaItemType.Armor:
                return "ARMOR";

            case GachaItemType.Weapon:
            default:
                return "WEAPON";
        }
    }

    private EquipmentRarity ToEquipmentRarity(string grade)
    {
        switch (grade)
        {
            case "SUPER_RARE":
                return EquipmentRarity.SuperRare;

            case "RARE":
                return EquipmentRarity.Rare;

            default:
                return EquipmentRarity.Normal;
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

    private EquipmentCategory GetInventoryCategoryFromItemType(string itemType, Sprite sprite)
    {
        if (!string.IsNullOrEmpty(itemType))
        {
            string lowerType = itemType.ToLowerInvariant();

            if (lowerType.Contains("weapon") || lowerType.Contains("무기"))
                return EquipmentCategory.Weapon;

            if (lowerType.Contains("armor") || lowerType.Contains("방어구"))
                return EquipmentCategory.Armor;
        }

        if (sprite != null)
        {
            string lowerName = sprite.name.ToLowerInvariant();

            if (
                lowerName.Contains("wp") ||
                lowerName.Contains("weapon") ||
                lowerName.Contains("sword") ||
                lowerName.Contains("axe") ||
                lowerName.Contains("bow") ||
                lowerName.Contains("spear") ||
                lowerName.Contains("staff") ||
                lowerName.Contains("blunt") ||
                lowerName.Contains("fist") ||
                lowerName.Contains("sickle")
            )
            {
                return EquipmentCategory.Weapon;
            }
        }

        return EquipmentCategory.Armor;
    }

    private RectTransform CreateOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.root;

        overlayObject = new GameObject(
            "GachaChestAnimationOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

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
            skipRequested = true;
    }

    private IEnumerator RevealResults(RectTransform overlay, GachaResult[] results)
    {
        RectTransform resultRoot = CreateContainer(
            "GachaResultRoot",
            overlay,
            GetResultRootSize(overlay, results.Length),
            resultStartPosition
        );

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
                continue;

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
        float height = Mathf.Min(
            overlay.rect.height * 0.86f,
            rows * GetLargeResultCellSize(overlay.rect.width, columns, 18f) + (rows - 1) * 18f
        );

        return new Vector2(width, height);
    }

    private int GetResultColumns(int resultCount)
    {
        if (resultCount <= 1)
            return 1;

        if (resultCount >= 55)
            return 8;

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

    private RectTransform CreateImage(
        string objectName,
        Transform parent,
        Sprite sprite,
        Vector2 size,
        Vector2 anchoredPosition,
        bool preserveAspect
    )
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
        float size = Mathf.Max(
            Mathf.Max(overlay.rect.width, overlay.rect.height),
            Mathf.Max(innerLightSize.x, innerLightSize.y)
        );

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

    private IEnumerator OpenChest(
        CanvasGroup closedGroup,
        CanvasGroup openGroup,
        RectTransform openChest,
        RectTransform light,
        CanvasGroup lightGroup
    )
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

    private EquipmentRarity GetHighestRarity(GachaResult[] results)
    {
        EquipmentRarity highest = EquipmentRarity.Normal;

        foreach (GachaResult result in results)
        {
            if (result.Rarity > highest)
                highest = result.Rarity;
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

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private int GetPullCount(Button button)
    {
        if (button == null)
            return 1;

        List<Button> orderedButtons = new List<Button>(gachaButtons);
        orderedButtons.Sort((left, right) => GetButtonY(right).CompareTo(GetButtonY(left)));

        int index = orderedButtons.IndexOf(button);

        if (index == 1)
            return 11;

        if (index == 2)
            return 55;

        return 1;
    }

    private float GetButtonY(Button button)
    {
        RectTransform rectTransform = button.transform as RectTransform;

        if (rectTransform == null)
            return button.transform.position.y;

        return rectTransform.position.y;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (gachaButtons == null)
            return;

        foreach (Button button in gachaButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void CleanupPlayback()
    {
        if (!isPlaying)
            return;

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

    private void RefreshBattlePlayerStats()
    {
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager != null)
            battleManager.RefreshPlayerStats();
    }

    [Serializable]
    private class ItemListResponse
    {
        public ItemDto[] items;
    }

    [Serializable]
    private class ItemDto
    {
        public long item_id;
        public string item_key;
        public string item_name;
        public string item_type;
        public string grade;
        public string image_key;
        public int base_attack;
        public int base_defense;
    }

    [Serializable]
    private class UserItemListResponse
    {
        public UserItemResponse[] items;
    }

    [Serializable]
    private class UserItemCreateRequest
    {
        public long user_id;
        public long item_id;
        public int enhance_level;
        public bool is_equipped;
        public int quantity;
    }

    [Serializable]
    private class UserItemResponse
    {
        public long user_item_id;
        public long user_id;
        public long item_id;
        public int enhance_level;
        public bool is_equipped;
        public int quantity;
    }

    [Serializable]
    private class SpendCurrencyRequest
    {
        public int amount;
    }

    [Serializable]
    private class CurrencyResponse
    {
        public long user_id;
        public long gold;
        public long gem;
        public string updated_at;
    }
}