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

    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI defenseValueText;
    [SerializeField] private TextMeshProUGUI goldText;
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

    private void Awake()
    {
        if (characterUpgradeApi == null)
        {
            characterUpgradeApi = CharacterUpgradeApi.Instance;
        }

        if (characterUpgradeApi == null)
        {
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();
        }

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

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
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
        {
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();
        }

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

    private void OnClickHpUpgrade()
    {
        if (characterUpgradeApi == null)
        {
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();
        }

        if (characterUpgradeApi == null)
        {
            Debug.LogError("[CharcterUpgrade] CharacterUpgradeApi가 없습니다.");
            SetMessage("CharacterUpgradeApi가 없습니다.");
            return;
        }

        SetButtonsInteractable(false);
        SetMessage("체력 강화 중...");

        int currentCost = CalculateClientPreviewCost(hpUpgradeLvl);

        StartCoroutine(characterUpgradeApi.UpgradeCharacter(
            true,
            hpUpgradeLvl,
            currentCost,
            0,
            0,
            (success, response) =>
            {
                SetButtonsInteractable(true);

                if (!success || response == null)
                {
                    Debug.LogError("[CharcterUpgrade] 체력 강화 실패");
                    SetMessage("체력 강화 실패");
                    return;
                }

                ApplyUpgradeResponseToUI(response);
                SetMessage("체력 강화 완료");
            }
        ));
    }

    private void OnClickAttackUpgrade()
    {
        if (characterUpgradeApi == null)
        {
            characterUpgradeApi = FindFirstObjectByType<CharacterUpgradeApi>();
        }

        if (characterUpgradeApi == null)
        {
            Debug.LogError("[CharcterUpgrade] CharacterUpgradeApi가 없습니다.");
            SetMessage("CharacterUpgradeApi가 없습니다.");
            return;
        }

        SetButtonsInteractable(false);
        SetMessage("공격력 강화 중...");

        int currentCost = CalculateClientPreviewCost(attackUpgradeLvl);

        StartCoroutine(characterUpgradeApi.UpgradeCharacter(
            false,
            attackUpgradeLvl,
            currentCost,
            0,
            0,
            (success, response) =>
            {
                SetButtonsInteractable(true);

                if (!success || response == null)
                {
                    Debug.LogError("[CharcterUpgrade] 공격력 강화 실패");
                    SetMessage("공격력 강화 실패");
                    return;
                }

                ApplyUpgradeResponseToUI(response);
                SetMessage("공격력 강화 완료");
            }
        ));
    }

    private void ApplyStatusToUI(CharacterUpgradeApi.UserStatusResponse status)
    {
        currentMaxHp = status.max_hp;
        currentAttackPower = status.attack_power;
        currentDefensePower = status.defense_power;

        hpUpgradeLvl = status.hp_upgrade_lvl;
        attackUpgradeLvl = status.attack_upgrade_lvl;
        defenseUpgradeLvl = status.defense_upgrade_lvl;

        RefreshAllTexts();

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.ApplyPlayerStatsFromDb(
                status.max_hp,
                status.attack_power,
                status.defense_power,
                status.hp_upgrade_lvl,
                status.attack_upgrade_lvl
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

        if (goldText != null)
        {
            goldText.text = $"Gold: {response.gold}";
        }

        GoldManager.Instance?.SetGold(response.gold);
        CurrencyUIManager.Instance?.SetGold(response.gold);

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (battleManager != null)
        {
            battleManager.ApplyPlayerStatsFromDb(
                response.max_hp,
                response.attack_power,
                response.defense_power,
                response.hp_upgrade_lvl,
                response.attack_upgrade_lvl
            );
        }
    }

    private void RefreshAllTexts()
    {
        if (hpValueText != null)
        {
            hpValueText.text = $"체력: {currentMaxHp}";
        }

        if (attackValueText != null)
        {
            attackValueText.text = $"공격력: {currentAttackPower}";
        }

        if (defenseValueText != null)
        {
            defenseValueText.text = $"방어력: {currentDefensePower}";
        }

        if (hpUpgradeLvlText != null)
        {
            hpUpgradeLvlText.text = $"Lv.{hpUpgradeLvl}";
        }

        if (attackUpgradeLvlText != null)
        {
            attackUpgradeLvlText.text = $"Lv.{attackUpgradeLvl}";
        }

        if (hpUpgradeCostText != null)
        {
            hpUpgradeCostText.text = $"{CalculateClientPreviewCost(hpUpgradeLvl)} Gold";
        }

        if (attackUpgradeCostText != null)
        {
            attackUpgradeCostText.text = $"{CalculateClientPreviewCost(attackUpgradeLvl)} Gold";
        }
    }

    private int CalculateClientPreviewCost(int upgradeLvl)
    {
        return GameBalance.StatUpgradeGoldCost(upgradeLvl);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hpUpgradeButton != null)
        {
            hpUpgradeButton.interactable = value;
        }

        if (attackUpgradeButton != null)
        {
            attackUpgradeButton.interactable = value;
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (!string.IsNullOrEmpty(message))
        {
            Debug.Log($"[CharcterUpgrade] {message}");
        }
    }

    public void RefreshFromServer()
    {
        LoadUserStatusToUI();
    }
}