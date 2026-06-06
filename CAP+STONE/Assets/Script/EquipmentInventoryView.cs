using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class EquipmentInventoryView
{
    private static ItemDetailPanel cachedDetailPanel;
    private static EquipmentInventoryViewRunner runner;
    private static bool isRefreshing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInitialize()
    {
        cachedDetailPanel = null;
        EnsureRunner();
    }

    public static void RefreshAll()
    {
        if (isRefreshing)
            return;

        isRefreshing = true;

        EnsureRunner();
        cachedDetailPanel = FindItemDetailPanel();

        RefreshContent("WeaponContent", "WEAPON");
        RefreshContent("ArmorContent", "ARMOR");

        isRefreshing = false;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        GameObject runnerObject = new GameObject("EquipmentInventoryViewRunner");
        Object.DontDestroyOnLoad(runnerObject);

        runner = runnerObject.AddComponent<EquipmentInventoryViewRunner>();
    }

    private static void RefreshContent(string contentName, string itemType)
    {
        Transform content = FindSceneTransform(contentName);
        if (content == null)
        {
            Debug.LogWarning(contentName + "을 찾지 못했습니다.");
            return;
        }

        for (int i = 0; i < content.childCount; i++)
        {
            Transform slot = content.GetChild(i);

            if (IsSpacer(slot))
            {
                ClearSpacer(slot);
                continue;
            }

            slot.gameObject.SetActive(true);

            Sprite sprite = GetCurrentSlotSprite(slot);
            if (sprite != null)
            {
                ApplySlot(slot, sprite, itemType);
            }
            else
            {
                ApplyEmptySlot(slot);
            }
        }
    }

    private static Sprite GetCurrentSlotSprite(Transform slot)
    {
        Image icon = FindIconImage(slot);
        return icon != null ? icon.sprite : null;
    }

    private static bool IsSpacer(Transform slot)
    {
        return slot != null && slot.name.ToLowerInvariant().Contains("spacer");
    }

    private static void ClearSpacer(Transform slot)
    {
        if (slot == null)
            return;

        EquipmentSlotUpgradeButton upgradeButton = slot.GetComponent<EquipmentSlotUpgradeButton>();
        if (upgradeButton != null)
            Object.Destroy(upgradeButton);

        Button button = slot.GetComponent<Button>();
        if (button != null)
            Object.Destroy(button);

        Transform levelText = slot.Find("LVText");
        if (levelText != null)
            levelText.gameObject.SetActive(false);

        Transform progressSlider = slot.Find("EquipmentProgressSlider");
        if (progressSlider != null)
            progressSlider.gameObject.SetActive(false);
    }

    private static void ApplySlot(Transform slot, Sprite sprite, string itemType)
    {
        if (slot == null || sprite == null)
            return;

        slot.gameObject.SetActive(true);

        EquipmentSlotUpgradeButton upgradeButton = slot.GetComponent<EquipmentSlotUpgradeButton>();
        if (upgradeButton != null)
            Object.Destroy(upgradeButton);

        ConfigureSlotButton(slot, sprite);

        EquipmentInventoryRecord record = EquipmentInventory.GetRecord(sprite);

        Image icon = FindIconImage(slot);
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = record.IsOwned ? Color.white : new Color(0.16f, 0.16f, 0.16f, 0.75f);
            icon.raycastTarget = false;
        }

        TextMeshProUGUI levelText = GetOrCreateLevelText(slot);
        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.raycastTarget = false;
            levelText.text = record.IsOwned ? "LV." + record.Level : "LV.-";
        }

        Slider slider = GetOrCreateSlider(slot);
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = record.IsOwned && record.RequiredCount > 0
                ? Mathf.Clamp01((float)record.TotalCount / record.RequiredCount)
                : 0f;

            SetSliderFillColor(
                slider,
                record.CanUpgrade
                    ? new Color(0.15f, 0.9f, 0.25f, 1f)
                    : new Color(0.95f, 0.78f, 0.2f, 1f)
            );
        }

        TextMeshProUGUI progressText = GetOrCreateProgressText(slot);
        if (progressText != null)
        {
            progressText.raycastTarget = false;
            progressText.gameObject.SetActive(true);
            progressText.text = record.IsOwned ? record.TotalCount + "/" + record.RequiredCount : "0/2";
        }
    }

    private static void ConfigureSlotButton(Transform slot, Sprite sprite)
    {
        if (slot == null)
            return;

        Image rootImage = slot.GetComponent<Image>();

        if (rootImage == null)
        {
            rootImage = slot.gameObject.AddComponent<Image>();
            rootImage.color = new Color(1f, 1f, 1f, 0f);
        }

        rootImage.raycastTarget = true;

        Button button = slot.GetComponent<Button>();
        if (button == null)
            button = slot.gameObject.AddComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.targetGraphic = rootImage;
        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            if (sprite == null)
            {
                Debug.Log("슬롯 스프라이트가 없습니다.");
                return;
            }

            EquipmentInventoryRecord latestRecord = EquipmentInventory.GetRecord(sprite);

            Debug.Log(
                "[EquipmentInventoryView] 슬롯 클릭 " +
                $"sprite={sprite.name}, " +
                $"owned={latestRecord.IsOwned}, " +
                $"userItemId={latestRecord.UserItemId}, " +
                $"level={latestRecord.Level}, " +
                $"quantity={latestRecord.TotalCount}, " +
                $"required={latestRecord.RequiredCount}, " +
                $"grade={latestRecord.ItemGrade}"
            );

            if (latestRecord == null || !latestRecord.IsOwned)
            {
                Debug.Log("보유하지 않은 장비입니다.");
                return;
            }

            ItemDetailPanel detailPanel = FindItemDetailPanel();
            if (detailPanel == null)
            {
                Debug.LogError("ItemDetailPanel을 찾지 못했습니다. ItemDetailRoot에 ItemDetailPanel.cs가 붙어있는지 확인하세요.");
                return;
            }

            detailPanel.Open(latestRecord);
        });
    }

    private static ItemDetailPanel FindItemDetailPanel()
    {
        if (
            cachedDetailPanel != null &&
            cachedDetailPanel.gameObject != null &&
            cachedDetailPanel.gameObject.scene.IsValid() &&
            cachedDetailPanel.gameObject.scene.isLoaded
        )
        {
            return cachedDetailPanel;
        }

        cachedDetailPanel = null;

        ItemDetailPanel[] panels = Resources.FindObjectsOfTypeAll<ItemDetailPanel>();

        foreach (ItemDetailPanel panel in panels)
        {
            if (panel == null)
                continue;

            if (!panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded)
                continue;

            cachedDetailPanel = panel;
            return cachedDetailPanel;
        }

        Debug.LogError("ItemDetailPanel을 찾지 못했습니다.");
        return null;
    }

    private static void ApplyEmptySlot(Transform slot)
    {
        EquipmentSlotUpgradeButton upgradeButton = slot.GetComponent<EquipmentSlotUpgradeButton>();
        if (upgradeButton != null)
            Object.Destroy(upgradeButton);

        Button button = slot.GetComponent<Button>();
        if (button != null)
            button.onClick.RemoveAllListeners();

        Image icon = FindIconImage(slot);
        if (icon != null)
        {
            icon.color = new Color(0.16f, 0.16f, 0.16f, 0.75f);
            icon.raycastTarget = false;
        }

        TextMeshProUGUI levelText = GetOrCreateLevelText(slot);
        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = "LV.-";
            levelText.raycastTarget = false;
        }

        Slider slider = GetOrCreateSlider(slot);
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            SetSliderFillColor(slider, new Color(0.95f, 0.78f, 0.2f, 1f));
        }

        TextMeshProUGUI progressText = GetOrCreateProgressText(slot);
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
            progressText.raycastTarget = false;
            progressText.text = "0/2";
        }
    }

    private static Image FindIconImage(Transform slot)
    {
        Image[] images = slot.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            if (image.name.Contains("Character"))
                return image;
        }

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            if (image.transform != slot)
                return image;
        }

        return null;
    }

    private static void SetSliderFillColor(Slider slider, Color color)
    {
        if (slider == null)
            return;

        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = color;
                return;
            }
        }

        Image[] images = slider.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            if (image.name.ToLowerInvariant().Contains("fill"))
            {
                image.color = color;
                return;
            }
        }

        if (slider.targetGraphic is Image targetImage)
            targetImage.color = color;
    }

    private static TextMeshProUGUI GetOrCreateLevelText(Transform slot)
    {
        TextMeshProUGUI[] texts = slot.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null)
                continue;

            if (text.name.Contains("LV") || text.text.Contains("LV"))
            {
                text.raycastTarget = false;
                return text;
            }
        }

        GameObject textObject = new GameObject("LVText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(slot, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -6f);
        rect.sizeDelta = new Vector2(-12f, 32f);

        TextMeshProUGUI levelText = textObject.GetComponent<TextMeshProUGUI>();
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.fontSize = 22f;
        levelText.color = Color.white;
        levelText.raycastTarget = false;

        return levelText;
    }

    private static TextMeshProUGUI GetOrCreateProgressText(Transform slot)
    {
        Slider slider = slot.GetComponentInChildren<Slider>(true);

        if (slider != null)
        {
            TextMeshProUGUI[] sliderTexts = slider.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI sliderText in sliderTexts)
            {
                if (sliderText != null)
                {
                    sliderText.gameObject.SetActive(true);
                    sliderText.raycastTarget = false;
                    return sliderText;
                }
            }

            Transform existing = slider.transform.Find("Text (TMP)");
            if (existing != null)
            {
                TextMeshProUGUI text = existing.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.gameObject.SetActive(true);
                    text.raycastTarget = false;
                    return text;
                }
            }
        }

        return null;
    }

    private static Slider GetOrCreateSlider(Transform slot)
    {
        Slider slider = slot.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            slider.interactable = false;

            Image[] images = slider.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != null)
                    image.raycastTarget = false;
            }

            return slider;
        }

        GameObject sliderObject = new GameObject("EquipmentProgressSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(slot, false);

        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 27f);
        rect.sizeDelta = new Vector2(-22f, 14f);

        RectTransform background = CreateSliderImage("Background", sliderObject.transform, new Color(0f, 0f, 0f, 0.55f));
        RectTransform fillArea = CreateContainer("Fill Area", sliderObject.transform);
        RectTransform fill = CreateSliderImage("Fill", fillArea, new Color(0.95f, 0.78f, 0.2f, 1f));

        Slider newSlider = sliderObject.GetComponent<Slider>();
        newSlider.transition = Selectable.Transition.None;
        newSlider.interactable = false;
        newSlider.targetGraphic = fill.GetComponent<Image>();
        newSlider.fillRect = fill;
        newSlider.direction = Slider.Direction.LeftToRight;

        background.SetAsFirstSibling();
        fillArea.SetAsLastSibling();

        return newSlider;
    }

    private static RectTransform CreateSliderImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private static RectTransform CreateContainer(string objectName, Transform parent)
    {
        GameObject container = new GameObject(objectName, typeof(RectTransform));
        container.transform.SetParent(parent, false);

        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform transform in transforms)
        {
            if (transform == null)
                continue;

            if (!transform.gameObject.scene.IsValid() || !transform.gameObject.scene.isLoaded)
                continue;

            if (transform.name.Trim() == objectName)
                return transform;
        }

        return null;
    }
}

public class EquipmentInventoryViewRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        EquipmentInventoryView.RefreshAll();

        yield return new WaitForSeconds(0.1f);
        EquipmentInventoryView.RefreshAll();
    }
}