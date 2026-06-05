using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("Item Visual")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Transform itemPrefabParent;

    [Header("Item Detail UI")]
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI mainEffectText;

    [Header("Progress Slider")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Color upgradePossibleColor = new Color(0.15f, 0.9f, 0.25f, 1f);
    [SerializeField] private Color upgradeImpossibleColor = new Color(0.95f, 0.78f, 0.2f, 1f);

    [Header("Equip")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;

    [Header("Enhance")]
    [SerializeField] private Button enhanceButton;
    [SerializeField] private TextMeshProUGUI enhanceButtonText;
    [SerializeField] private TextMeshProUGUI enhanceGoldText;

    [Header("Close")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button outsideCloseButton;

    private EquipmentInventoryRecord currentRecord;
    private GameObject spawnedItemPrefab;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (outsideCloseButton != null)
            outsideCloseButton.onClick.AddListener(Close);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnClickEquip);

        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(OnClickEnhance);

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
        gameObject.SetActive(true);
    }

    public void Close()
    {
        ClearSpawnedPrefab();
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
            itemNameText.text = string.IsNullOrEmpty(currentRecord.ItemName)
                ? currentRecord.ItemKey
                : currentRecord.ItemName;

        if (levelText != null)
            levelText.text = "Lv." + currentRecord.Level;

        if (progressText != null)
            progressText.text = currentRecord.TotalCount + "/" + currentRecord.RequiredCount;

        RefreshProgressSlider();

        if (mainEffectText != null)
            mainEffectText.text = GetMainEffectText();

        if (equipButtonText != null)
            equipButtonText.text = currentRecord.IsEquipped ? "장착중" : "장착";

        if (equipButton != null)
            equipButton.interactable = currentRecord.IsOwned && !currentRecord.IsEquipped;

        if (enhanceButtonText != null)
            enhanceButtonText.text = currentRecord.CanUpgrade ? "강화" : "강화불가";

        if (enhanceButton != null)
            enhanceButton.interactable = currentRecord.CanUpgrade;

        if (enhanceGoldText != null)
            enhanceGoldText.text = currentRecord.EnhanceGoldCost.ToString();
    }

    private void RefreshProgressSlider()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.interactable = false;

            if (currentRecord.RequiredCount > 0)
            {
                progressSlider.value = Mathf.Clamp01(
                    (float)currentRecord.TotalCount / currentRecord.RequiredCount
                );
            }
            else
            {
                progressSlider.value = 0f;
            }
        }

        Image fillImage = progressFillImage;

        if (fillImage == null && progressSlider != null && progressSlider.fillRect != null)
            fillImage = progressSlider.fillRect.GetComponent<Image>();

        if (fillImage != null)
        {
            fillImage.color = currentRecord.CanUpgrade
                ? upgradePossibleColor
                : upgradeImpossibleColor;
        }
    }

    private void LoadItemVisual()
    {
        ClearSpawnedPrefab();

        string itemKey = currentRecord.ItemKey;
        string imageKey = string.IsNullOrEmpty(currentRecord.ImageKey)
            ? currentRecord.ItemKey
            : currentRecord.ImageKey;

        // 1순위: 프리팹 로딩
        // 실제 파일 위치 예:
        // Assets/Resources/GachaRare/Rare/rare_sword.prefab
        if (itemPrefabParent != null && !string.IsNullOrEmpty(itemKey))
        {
            string gradeFolder = GetGradeFolder(currentRecord.ItemGrade);
            string prefabPath = $"GachaRare/{gradeFolder}/{itemKey}";

            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab != null)
            {
                spawnedItemPrefab = Instantiate(prefab, itemPrefabParent);
                spawnedItemPrefab.transform.localPosition = Vector3.zero;
                spawnedItemPrefab.transform.localRotation = Quaternion.identity;
                spawnedItemPrefab.transform.localScale = Vector3.one;
                return;
            }

            Debug.LogWarning("아이템 프리팹을 찾지 못했습니다: Resources/" + prefabPath);
        }

        // 2순위: 이미지 로딩
        // 실제 파일 위치 예:
        // Assets/Resources/ItemImages/rare_sword.png
        if (itemIconImage != null && !string.IsNullOrEmpty(imageKey))
        {
            string imagePath = "ItemImages/" + imageKey;
            Sprite sprite = Resources.Load<Sprite>(imagePath);

            if (sprite != null)
            {
                itemIconImage.sprite = sprite;
                itemIconImage.preserveAspect = true;
                itemIconImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning("아이템 이미지를 찾지 못했습니다: Resources/" + imagePath);
            }
        }
    }

    private void ClearSpawnedPrefab()
    {
        if (spawnedItemPrefab != null)
        {
            Destroy(spawnedItemPrefab);
            spawnedItemPrefab = null;
        }

        if (itemPrefabParent == null)
            return;

        for (int i = itemPrefabParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemPrefabParent.GetChild(i).gameObject);
        }
    }

    private string GetMainEffectText()
    {
        string itemType = (currentRecord.ItemType ?? "").ToUpperInvariant();

        if (itemType == "WEAPON")
            return "+공격력 " + currentRecord.FinalAttack;

        if (itemType == "ARMOR")
            return "+방어력 " + currentRecord.FinalDefense;

        return "+효과 없음";
    }

    private string FormatGrade(string grade)
    {
        string normalized = (grade ?? "").ToUpperInvariant();

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
        string normalized = (grade ?? "").ToUpperInvariant();

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

        SetButtonsInteractable(false);

        EquipmentApi.Instance.EquipItem(
            currentRecord.UserItemId,
            success =>
            {
                SetButtonsInteractable(true);

                if (!success)
                    return;

                Close();
                EquipmentInventoryView.RefreshAll();
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

        SetButtonsInteractable(false);

        EquipmentApi.Instance.EnhanceItem(
            currentRecord.UserItemId,
            success =>
            {
                SetButtonsInteractable(true);

                if (!success)
                    return;

                Close();
                EquipmentInventoryView.RefreshAll();
            }
        );
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