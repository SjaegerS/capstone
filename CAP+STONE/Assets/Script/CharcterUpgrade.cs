using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharcterUpgrade : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private CharacterUpgradeApi characterUpgradeApi;

    [Header("HP Upgrade UI")]
    [SerializeField] private Button hpUpgradeButton;
    [SerializeField] private TextMeshProUGUI hpValueText;
    [SerializeField] private TextMeshProUGUI hpUpgradeLvlText;
    [SerializeField] private TextMeshProUGUI hpUpgradeCostText;

    [Header("Attack Upgrade UI")]
    [SerializeField] private Button attackUpgradeButton;
    [SerializeField] private TextMeshProUGUI attackValueText;
    [SerializeField] private TextMeshProUGUI attackUpgradeLvlText;
    [SerializeField] private TextMeshProUGUI attackUpgradeCostText;

    [Header("Defense Upgrade UI")]
    [SerializeField] private Button defenseUpgradeButton;
    [SerializeField] private TextMeshProUGUI defenseValueText;
    [SerializeField] private TextMeshProUGUI defenseUpgradeLvlText;
    [SerializeField] private TextMeshProUGUI defenseUpgradeCostText;

    [Header("Message")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Battle")]
    [SerializeField] private BattleManager battleManager;

    private int currentMaxHp = 100;
    private int currentAttackPower = 10;
    private int currentDefensePower = 5;

    private int hpUpgradeLvl = 1;
    private int attackUpgradeLvl = 1;
    private int defenseUpgradeLvl = 1;

    private bool isLoading = false;
    private bool isUpgrading = false;

    private void Awake()
    {
        if (characterUpgradeApi == null)
            characterUpgradeApi = CharacterUpgradeApi.Instance;

        if (characterUpgradeApi == null)
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();

        if (hpUpgradeButton != null)
        {
            hpUpgradeButton.onClick.RemoveListener(OnClickHpUpgrade);
            hpUpgradeButton.onClick.AddListener(OnClickHpUpgrade);
        }

        if (attackUpgradeButton != null)
        {
            attackUpgradeButton.onClick.RemoveListener(OnClickAttackUpgrade);
            attackUpgradeButton.onClick.AddListener(OnClickAttackUpgrade);
        }

        if (defenseUpgradeButton != null)
        {
            defenseUpgradeButton.onClick.RemoveListener(OnClickDefenseUpgrade);
            defenseUpgradeButton.onClick.AddListener(OnClickDefenseUpgrade);
        }

        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();
    }

    private void Start()
    {
        LoadUserStatusToUI();
    }

    private void OnEnable()
    {
        LoadUserStatusToUI();
    }

    private void LoadUserStatusToUI()
    {
        if (isLoading)
            return;

        if (characterUpgradeApi == null)
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();

        if (characterUpgradeApi == null)
        {
            Debug.LogError("[CharcterUpgrade] CharacterUpgradeApi가 없습니다.");
            SetMessage("CharacterUpgradeApi가 없습니다.");
            return;
        }

        isLoading = true;
        SetButtonsInteractable(false);

        StartCoroutine(characterUpgradeApi.LoadUserStatus(
            (success, status) =>
            {
                isLoading = false;
                SetButtonsInteractable(true);

                if (!success || status == null)
                {
                    Debug.LogError("[CharcterUpgrade] 유저 상태 로드 실패");
                    SetMessage("유저 상태 로드 실패");
                    return;
                }

                ApplyStatusToUI(status);
                SetMessage("");
            }
        ));
    }

    private void RequestUpgrade(
        CharacterUpgradeApi.UpgradeStatType upgradeType,
        int upgradeLvl,
        string label
    )
    {
        if (isUpgrading)
        {
            Debug.LogWarning($"[CharcterUpgrade] 이미 강화 요청 중입니다. {label} 강화 중복 요청 차단");
            return;
        }

        if (!ValidateApi())
            return;

        isUpgrading = true;
        SetButtonsInteractable(false);
        SetMessage($"{label} 강화 중...");

        int currentCost = CalculateClientPreviewCost(upgradeLvl);

        Debug.Log($"[UPGRADE REQUEST] Type={upgradeType}, Lv={upgradeLvl}, Cost={currentCost}");

        StartCoroutine(characterUpgradeApi.UpgradeCharacter(
            upgradeType,
            upgradeLvl,
            currentCost,
            0,
            0,
            0,
            (success, response) =>
            {
                isUpgrading = false;
                SetButtonsInteractable(true);

                if (!success || response == null)
                {
                    Debug.LogError($"[CharcterUpgrade] {label} 강화 실패");
                    SetMessage($"{label} 강화 실패");
                    return;
                }

                Debug.Log(
                    $"[UPGRADE RESPONSE] " +
                    $"Type={upgradeType}, " +
                    $"HP_Lv={response.hp_upgrade_lvl}, " +
                    $"ATK_Lv={response.attack_upgrade_lvl}, " +
                    $"DEF_Lv={response.defense_upgrade_lvl}, " +
                    $"HP={response.max_hp}, " +
                    $"ATK={response.attack_power}, " +
                    $"DEF={response.defense_power}, " +
                    $"Gold={response.gold}"
                );

                ApplyUpgradeResponseToUI(response);
                SetMessage($"{label} 강화 완료");

                QuestProgressReporter questReporter =
                FindFirstObjectByType<QuestProgressReporter>();

                if (questReporter != null)
                {
                    questReporter.ReportProgress(QuestEvent.Stat, 1);
                }
            }
        ));
    }

    private void OnClickHpUpgrade()
    {
        RequestUpgrade(CharacterUpgradeApi.UpgradeStatType.Hp, hpUpgradeLvl, "체력");
    }

    private void OnClickAttackUpgrade()
    {
        RequestUpgrade(CharacterUpgradeApi.UpgradeStatType.Attack, attackUpgradeLvl, "공격력");
    }

    private void OnClickDefenseUpgrade()
    {
        RequestUpgrade(CharacterUpgradeApi.UpgradeStatType.Defense, defenseUpgradeLvl, "방어력");
    }

    private bool ValidateApi()
    {
        if (characterUpgradeApi == null)
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();

        if (characterUpgradeApi == null)
        {
            Debug.LogError("[CharcterUpgrade] CharacterUpgradeApi가 없습니다.");
            SetMessage("CharacterUpgradeApi가 없습니다.");
            return false;
        }

        return true;
    }

    private void ApplyStatusToUI(CharacterUpgradeApi.UserStatusResponse status)
    {
        currentMaxHp = status.max_hp;
        currentAttackPower = status.attack_power;
        currentDefensePower = status.defense_power;

        hpUpgradeLvl = Mathf.Max(1, status.hp_upgrade_lvl);
        attackUpgradeLvl = Mathf.Max(1, status.attack_upgrade_lvl);
        defenseUpgradeLvl = Mathf.Max(1, status.defense_upgrade_lvl);

        RefreshAllTexts();

        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager != null)
        {
            battleManager.ApplyPlayerStatsFromDb(
                status.max_hp,
                status.attack_power,
                status.defense_power,
                status.hp_upgrade_lvl,
                status.attack_upgrade_lvl,
                status.defense_upgrade_lvl
            );
        }
    }

    private void ApplyUpgradeResponseToUI(CharacterUpgradeApi.CharacterUpgradeResponse response)
    {
        currentMaxHp = response.max_hp;
        currentAttackPower = response.attack_power;
        currentDefensePower = response.defense_power;

        hpUpgradeLvl = Mathf.Max(1, response.hp_upgrade_lvl);
        attackUpgradeLvl = Mathf.Max(1, response.attack_upgrade_lvl);
        defenseUpgradeLvl = Mathf.Max(1, response.defense_upgrade_lvl);

        RefreshAllTexts();

        CurrencyUIManager.Instance?.SetGold(response.gold);

        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager != null)
        {
            battleManager.ApplyPlayerStatsFromDb(
                response.max_hp,
                response.attack_power,
                response.defense_power,
                response.hp_upgrade_lvl,
                response.attack_upgrade_lvl,
                response.defense_upgrade_lvl
            );
        }
    }

    private void RefreshAllTexts()
    {
        if (hpValueText != null)
            hpValueText.text = $"체력: {currentMaxHp}";

        if (attackValueText != null)
            attackValueText.text = $"공격력: {currentAttackPower}";

        if (defenseValueText != null)
            defenseValueText.text = $"방어력: {currentDefensePower}";

        if (hpUpgradeLvlText != null)
            hpUpgradeLvlText.text = $"Lv.{hpUpgradeLvl}";

        if (attackUpgradeLvlText != null)
            attackUpgradeLvlText.text = $"Lv.{attackUpgradeLvl}";

        if (defenseUpgradeLvlText != null)
            defenseUpgradeLvlText.text = $"Lv.{defenseUpgradeLvl}";

        if (hpUpgradeCostText != null)
            hpUpgradeCostText.text = CurrencyUIManager.FormatCurrency(CalculateClientPreviewCost(hpUpgradeLvl));

        if (attackUpgradeCostText != null)
            attackUpgradeCostText.text = CurrencyUIManager.FormatCurrency(CalculateClientPreviewCost(attackUpgradeLvl));

        if (defenseUpgradeCostText != null)
            defenseUpgradeCostText.text = CurrencyUIManager.FormatCurrency(CalculateClientPreviewCost(defenseUpgradeLvl));
    }

    private int CalculateClientPreviewCost(int upgradeLvl)
    {
        return GameBalance.StatUpgradeGoldCost(upgradeLvl);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hpUpgradeButton != null)
            hpUpgradeButton.interactable = value;

        if (attackUpgradeButton != null)
            attackUpgradeButton.interactable = value;

        if (defenseUpgradeButton != null)
            defenseUpgradeButton.interactable = value;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log($"[CharcterUpgrade] {message}");
    }

    public void RefreshFromServer()
    {
        LoadUserStatusToUI();
    }
}