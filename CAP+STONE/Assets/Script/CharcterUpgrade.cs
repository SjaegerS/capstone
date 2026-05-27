using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharcterUpgrade : MonoBehaviour
{
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private int upgradeCount;
    [SerializeField] private TextMeshProUGUI LVText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI statValueText;

    public int UpgradeCount => upgradeCount;
    public int CurrentCost => GameBalance.CharacterUpgradeGoldCost(EffectiveUpgradeCount);

    private int EffectiveUpgradeCount => battleManager != null
        ? Mathf.Max(upgradeCount, battleManager.GetPlayerUpgradeLevel(IsHealthUpgrade))
        : upgradeCount;
    private bool IsHealthUpgrade => DisplaysHealth();

    private void Awake()
    {
        AutoBindMissingReferences();

        if (battleManager != null)
        {
            upgradeCount = Mathf.Max(upgradeCount, battleManager.GetPlayerUpgradeLevel(IsHealthUpgrade));
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(TryUpgrade);
            upgradeButton.onClick.AddListener(TryUpgrade);
        }
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    public void TryUpgrade()
    {
        GoldManager goldManager = GoldManager.Instance;
        if (goldManager == null)
        {
            Debug.LogWarning("[CharcterUpgrade] GoldManager is missing.");
            return;
        }

        int currentUpgradeCount = EffectiveUpgradeCount;
        int cost = GameBalance.CharacterUpgradeGoldCost(currentUpgradeCount);
        if (!goldManager.TrySpendGold(cost))
        {
            Debug.Log($"[CharcterUpgrade] Not enough gold. Need {cost}, current {goldManager.currentGold}");
            UpdateUI();
            return;
        }

        upgradeCount = currentUpgradeCount + 1;

        battleManager?.ApplyPlayerUpgradeLevel(IsHealthUpgrade, upgradeCount);

        Debug.Log($"[CharcterUpgrade] Upgrade success. Level {upgradeCount}, spent {cost} gold.");
        UpdateUI();
    }

    private void UpdateUI()
    {
        int currentUpgradeCount = EffectiveUpgradeCount;
        int cost = GameBalance.CharacterUpgradeGoldCost(currentUpgradeCount);

        if (costText != null)
        {
            costText.text = cost.ToString();
        }

        if (LVText != null)
        {
            LVText.text = $"LV {currentUpgradeCount + 1}";
        }

        CharacterStats playerStats = GetCurrentPlayerStats(currentUpgradeCount);

        if (hpText != null)
        {
            hpText.text = Mathf.RoundToInt(playerStats.MaxHP).ToString();
        }

        if (attackText != null)
        {
            attackText.text = Mathf.RoundToInt(playerStats.AttackDamage).ToString();
        }

        if (statValueText != null)
        {
            statValueText.text = GetDisplayedStatText(currentUpgradeCount);
        }

        if (upgradeButton != null && GoldManager.Instance != null)
        {
            upgradeButton.interactable = GoldManager.Instance.CanSpendGold(cost);
        }
    }

    private void AutoBindMissingReferences()
    {
        if (upgradeButton == null)
        {
            upgradeButton = FindUpgradeButton();
        }

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (LVText == null)
        {
            LVText = FindLevelText();
        }

        if (hpText == null)
        {
            hpText = FindTextByName("HPText", "HealthText", "HpText");
        }

        if (attackText == null)
        {
            attackText = FindTextByName("AttackText", "ATKText", "AtkText");
        }

        if (statValueText == null)
        {
            statValueText = FindStatValueText();
        }
    }

    private Button FindUpgradeButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button.name.Trim() == "UpgradeButton")
            {
                return button;
            }
        }

        return buttons.Length > 0 ? buttons[0] : null;
    }

    private TextMeshProUGUI FindLevelText()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name.Trim() == "LVText")
            {
                return text;
            }
        }

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.text.Trim().StartsWith("LV"))
            {
                return text;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindTextByName(params string[] names)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (string targetName in names)
        {
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name.Trim() == targetName)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private TextMeshProUGUI FindStatValueText()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            string value = text.text.Trim();
            if (text != costText && text != LVText && int.TryParse(value, out _))
            {
                return text;
            }
        }

        return null;
    }

    private string GetDisplayedStatText(int currentUpgradeCount)
    {
        int baseStat = GetUpgradedBaseStat(currentUpgradeCount);
        int equipmentIncrease = GetEquipmentStatIncrease(baseStat);
        return $"{baseStat} + ({equipmentIncrease})";
    }

    private int GetUpgradedBaseStat(int currentUpgradeCount)
    {
        if (IsHealthUpgrade)
        {
            return Mathf.RoundToInt(GameBalance.PlayerStatAfterUpgrade(GameBalance.PlayerBaseHP, currentUpgradeCount));
        }

        return Mathf.RoundToInt(GameBalance.PlayerStatAfterUpgrade(GameBalance.PlayerBaseATK, currentUpgradeCount));
    }

    private int GetEquipmentStatIncrease(int baseStat)
    {
        EquipmentStatBonus bonus = IsHealthUpgrade
            ? EquipmentStatCalculator.GetArmorBonus()
            : EquipmentStatCalculator.GetWeaponBonus();

        return EquipmentStatCalculator.GetBonusIncrease(baseStat, bonus);
    }

    private CharacterStats GetCurrentPlayerStats(int currentUpgradeCount)
    {
        int hpUpgradeLevel = IsHealthUpgrade ? currentUpgradeCount : 0;
        int attackUpgradeLevel = IsHealthUpgrade ? 0 : currentUpgradeCount;

        if (battleManager != null)
        {
            hpUpgradeLevel = IsHealthUpgrade ? currentUpgradeCount : battleManager.GetPlayerUpgradeLevel(true);
            attackUpgradeLevel = IsHealthUpgrade ? battleManager.GetPlayerUpgradeLevel(false) : currentUpgradeCount;
        }

        return CharacterStats.CreatePlayer(hpUpgradeLevel, attackUpgradeLevel);
    }

    private bool DisplaysHealth()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            string value = text.text.Trim();
            if (value.Contains("\uCCB4\uB825"))
            {
                return true;
            }

            if (value.Contains("체력") || value.Contains("HP") || value.Contains("Health"))
            {
                return true;
            }
        }

        return false;
    }
}
