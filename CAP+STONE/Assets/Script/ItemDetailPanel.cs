using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("Item Visual")]
    [SerializeField] private Image itemIconImage;

    [Header("Item Detail UI")]
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI mainEffectText;
    [SerializeField] private TextMeshProUGUI subEffectText;

    [Header("Progress Slider")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Color upgradePossibleColor = new Color(0.15f, 0.9f, 0.25f, 1f);
    [SerializeField] private Color upgradeImpossibleColor = new Color(0.95f, 0.78f, 0.2f, 1f);

    [Header("Equip")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;
    [SerializeField] private CanvasGroup equipButtonCanvasGroup;

    [Header("Enhance")]
    [SerializeField] private Button enhanceButton;
    [SerializeField] private TextMeshProUGUI enhanceButtonText;
    [SerializeField] private TextMeshProUGUI enhanceGoldText;

    [Header("Close")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button outsideCloseButton;

    private EquipmentInventoryRecord currentRecord;

    private void Awake()
    {
         if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (outsideCloseButton != null)
        {
            outsideCloseButton.onClick.RemoveAllListeners();
            outsideCloseButton.onClick.AddListener(Close);
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnClickEquip);
        }

        if (enhanceButton != null)
        {
            enhanceButton.onClick.RemoveAllListeners();
            enhanceButton.onClick.AddListener(OnClickEnhance);
        }

        gameObject.SetActive(false);
    }

    public void Open(EquipmentInventoryRecord record)
    {
        currentRecord = record;

        if (currentRecord == null)
        {
            Debug.LogError("ItemDetailPanel.Open()에 record가 null로 들어왔습니다.");
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        Debug.Log(
            "[ItemDetail Open] " +
            $"itemName={currentRecord.ItemName}, " +
            $"itemKey={currentRecord.ItemKey}, " +
            $"imageKey={currentRecord.ImageKey}, " +
            $"type={currentRecord.ItemType}, " +
            $"grade={currentRecord.ItemGrade}, " +
            $"level={currentRecord.Level}, " +
            $"quantity={currentRecord.TotalCount}, " +
            $"required={currentRecord.RequiredCount}, " +
            $"canUpgrade={currentRecord.CanUpgrade}, " +
            $"isEquipped={currentRecord.IsEquipped}, " +
            $"goldCost={currentRecord.EnhanceGoldCost}, " +
            $"finalAtk={currentRecord.FinalAttack}, " +
            $"finalDef={currentRecord.FinalDefense}"
        );

        RefreshUI();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (currentRecord == null)
            return;

        LoadItemVisual();

        if (gradeText != null)
            gradeText.text = FormatGrade(currentRecord.ItemGrade);

        if (itemNameText != null)
        {
            itemNameText.text = string.IsNullOrEmpty(currentRecord.ItemName)
                ? currentRecord.ItemKey
                : currentRecord.ItemName;
        }

        if (levelText != null)
            levelText.text = "Lv." + currentRecord.Level;

        if (progressText != null)
            progressText.text = currentRecord.TotalCount + "/" + currentRecord.RequiredCount;

        RefreshProgressSlider();
        RefreshEffectTexts();
        RefreshEquipButtonState();
        RefreshEnhanceButtonState();

        if (enhanceGoldText != null)
            enhanceGoldText.text = GameBalance.EquipmentEnhanceGoldCost(currentRecord.Level).ToString();
    }

    private void RefreshProgressSlider()
    {
        if (currentRecord == null)
            return;

        float progress = 0f;

        if (currentRecord.RequiredCount > 0)
        {
            progress = Mathf.Clamp01(
                (float)currentRecord.TotalCount / currentRecord.RequiredCount
            );
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = progress;
            progressSlider.interactable = false;
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] progressSlider가 연결되지 않았습니다.");
        }

        Color targetColor = currentRecord.CanUpgrade
            ? upgradePossibleColor
            : upgradeImpossibleColor;

        Image fillImage = progressFillImage;

        if (fillImage == null && progressSlider != null && progressSlider.fillRect != null)
        {
            fillImage = progressSlider.fillRect.GetComponent<Image>();
        }

        if (fillImage == null && progressSlider != null)
        {
            Image[] images = progressSlider.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image == null)
                    continue;

                string lowerName = image.name.ToLowerInvariant();

                if (lowerName.Contains("fill"))
                {
                    fillImage = image;
                    break;
                }
            }
        }

        if (fillImage != null)
        {
            fillImage.color = targetColor;
            progressFillImage = fillImage;
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] Progress Slider Fill 이미지를 찾지 못했습니다.");
        }

        Debug.Log(
            "[ItemDetailPanel] 팝업 슬라이더 갱신 | " +
            $"quantity={currentRecord.TotalCount}, " +
            $"required={currentRecord.RequiredCount}, " +
            $"progress={progress}, " +
            $"canUpgrade={currentRecord.CanUpgrade}"
        );
    }

    private void LoadItemVisual()
    {
        if (itemIconImage == null)
            return;

        itemIconImage.sprite = null;
        itemIconImage.color = new Color(1f, 1f, 1f, 0f);

        string itemKey = currentRecord.ItemKey;
        string imageKey = string.IsNullOrEmpty(currentRecord.ImageKey)
            ? currentRecord.ItemKey
            : currentRecord.ImageKey;

        string gradeFolder = GetGradeFolder(currentRecord.ItemGrade);

        Sprite sprite = null;

        if (!string.IsNullOrEmpty(imageKey))
        {
            string imageKeyPath = $"GachaRare/{gradeFolder}/{imageKey}";
            sprite = Resources.Load<Sprite>(imageKeyPath);

            Debug.Log(
                $"[ItemDetailPanel] 스프라이트 로드 imageKey path={imageKeyPath}, " +
                $"result={(sprite != null ? sprite.name : "null")}"
            );
        }

        if (sprite == null && !string.IsNullOrEmpty(itemKey) && itemKey != imageKey)
        {
            string itemKeyPath = $"GachaRare/{gradeFolder}/{itemKey}";
            sprite = Resources.Load<Sprite>(itemKeyPath);

            Debug.Log(
                $"[ItemDetailPanel] 스프라이트 로드 itemKey path={itemKeyPath}, " +
                $"result={(sprite != null ? sprite.name : "null")}"
            );
        }

        if (sprite == null)
        {
            Debug.LogWarning(
                "아이템 스프라이트를 찾지 못했습니다. " +
                $"grade={currentRecord.ItemGrade}, " +
                $"folder={gradeFolder}, " +
                $"itemKey={itemKey}, imageKey={imageKey}"
            );
            return;
        }

        itemIconImage.sprite = sprite;
        itemIconImage.preserveAspect = true;
        itemIconImage.color = Color.white;
    }

    private void RefreshEffectTexts()
    {
        string mainText = GetMainEffectText();
        string subText = GetSubEffectText();

        if (mainEffectText != null)
            mainEffectText.text = mainText;

        if (subEffectText != null)
        {
            subEffectText.text = subText;
        }
        else if (mainEffectText != null)
        {
            mainEffectText.text = mainText + "\n" + subText;
        }
    }

    private string GetMainEffectText()
    {
        if (currentRecord == null)
            return "";

        string itemType = NormalizeItemType(currentRecord.ItemType);

        int mainEffect = GameBalance.EquipmentMainEffect(
            currentRecord.ItemGrade,
            currentRecord.Level
        );

        if (itemType == "WEAPON")
            return "기본 공격력 +" + mainEffect;

        if (itemType == "ARMOR")
            return "기본 방어력 +" + mainEffect;

        return "기본 효과 +" + mainEffect;
    }

    private string GetSubEffectText()
    {
        if (currentRecord == null)
            return "";

        string itemType = NormalizeItemType(currentRecord.ItemType);

        float subEffectRate = GameBalance.EquipmentSubEffectRate(
            currentRecord.ItemGrade,
            currentRecord.Level
        );

        float percent = subEffectRate * 100f;

        if (itemType == "WEAPON")
            return $"최종 공격력 +{percent:0.##}%";

        if (itemType == "ARMOR")
            return $"최종 방어력 +{percent:0.##}%";

        return $"최종 효과 +{percent:0.##}%";
    }

    private void RefreshEquipButtonState()
    {
        bool isEquipped = currentRecord != null && currentRecord.IsEquipped;
        bool canEquip = currentRecord != null && currentRecord.IsOwned && !isEquipped;

        if (equipButtonText != null)
            equipButtonText.text = isEquipped ? "장착중" : "장착";

        if (equipButton != null)
            equipButton.interactable = canEquip;

        if (equipButtonCanvasGroup != null)
            equipButtonCanvasGroup.alpha = canEquip ? 1f : 0.45f;
    }

    private void RefreshEnhanceButtonState()
    {
        bool canEnhance = currentRecord != null && currentRecord.CanUpgrade;

        if (enhanceButtonText != null)
            enhanceButtonText.text = canEnhance ? "강화" : "강화불가";

        if (enhanceButton != null)
            enhanceButton.interactable = canEnhance;
    }

    private string FormatGrade(string grade)
    {
        string normalized = NormalizeGrade(grade);

        switch (normalized)
        {
            case "NORMAL":
                return "Normal";

            case "RARE":
                return "Rare";

            case "SUPER_RARE":
            case "SUPERRARE":
                return "SuperRare";

            default:
                return string.IsNullOrEmpty(grade) ? "" : grade;
        }
    }

    private string GetGradeFolder(string grade)
    {
        string normalized = NormalizeGrade(grade);

        switch (normalized)
        {
            case "NORMAL":
                return "Normal";

            case "RARE":
                return "Rare";

            case "SUPER_RARE":
            case "SUPERRARE":
                return "SuperRare";

            default:
                return "Normal";
        }
    }

    private void OnClickEquip()
    {
        if (currentRecord == null)
            return;

        if (EquipmentApi.Instance == null)
        {
            Debug.LogError("EquipmentApi.Instance가 없습니다. 씬에 EquipmentApi 오브젝트를 추가하세요.");
            return;
        }

        Debug.Log($"[ItemDetailPanel] 장착 버튼 클릭. EquipItem 호출. userItemId={currentRecord.UserItemId}");

        SetButtonsInteractable(false);

        EquipmentApi.Instance.EquipItem(
            currentRecord.UserItemId,
            success =>
            {
                SetButtonsInteractable(true);

                if (!success)
                    return;

                EquipmentInventory.EquipOnlyThis(currentRecord);

                RefreshUI();
                EquipmentInventoryView.RefreshAll();
                RefreshBattlePlayerStats();
            }
        );
    }

    private void OnClickEnhance()
    {
        if (currentRecord == null)
            return;

        if (!currentRecord.CanUpgrade)
        {
            Debug.Log(
                "강화 조건을 만족하지 않습니다. " +
                $"quantity={currentRecord.TotalCount}, " +
                $"required={currentRecord.RequiredCount}, " +
                $"canUpgrade={currentRecord.CanUpgrade}, " +
                $"goldCost={currentRecord.EnhanceGoldCost}"
            );
            return;
        }

        if (EquipmentApi.Instance == null)
        {
            Debug.LogError("EquipmentApi.Instance가 없습니다. 씬에 EquipmentApi 오브젝트를 추가하세요.");
            return;
        }

        Debug.Log($"[ItemDetailPanel] 강화 버튼 클릭. EnhanceItem 호출. userItemId={currentRecord.UserItemId}");

        SetButtonsInteractable(false);

        EquipmentApi.Instance.EnhanceItem(
            currentRecord.UserItemId,
            success =>
            {
                SetButtonsInteractable(true);

                if (!success)
                    return;

                bool localUpdated = currentRecord.TryUpgradeLocalOnly();

                if (localUpdated)
                {
                    EquipmentInventory.SaveRecordToPrefs(currentRecord);
                }

                Debug.Log(
                    "[ItemDetailPanel] 강화 성공 후 로컬 갱신 " +
                    $"result={localUpdated}, " +
                    $"level={currentRecord.Level}, " +
                    $"quantity={currentRecord.TotalCount}, " +
                    $"required={currentRecord.RequiredCount}, " +
                    $"goldCost={currentRecord.EnhanceGoldCost}"
                );

                EquipmentInventoryView.RefreshAll();
                RefreshUI();
                RefreshBattlePlayerStats();

                QuestProgressReporter questReporter = FindFirstObjectByType<QuestProgressReporter>();

                if (questReporter != null)
                {
                    questReporter.ReportProgress("EQUIPMENT_ENHANCE", 1);
                }
            }
        );
    }

    private void RefreshBattlePlayerStats()
    {
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager != null)
            battleManager.RefreshPlayerStats();
    }

    private static string NormalizeItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return string.Empty;

        return itemType
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }

    private static string NormalizeGrade(string grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
            return "NORMAL";

        return grade
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }

    private void SetButtonsInteractable(bool value)
    {
        if (equipButton != null)
        {
            equipButton.interactable =
                value &&
                currentRecord != null &&
                currentRecord.IsOwned &&
                !currentRecord.IsEquipped;
        }

        if (enhanceButton != null)
        {
            enhanceButton.interactable =
                value &&
                currentRecord != null &&
                currentRecord.CanUpgrade;
        }

        if (closeButton != null)
            closeButton.interactable = value;

        if (outsideCloseButton != null)
            outsideCloseButton.interactable = value;
    }
}