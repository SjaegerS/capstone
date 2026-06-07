using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelPanelController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private LevelPanelApi levelPanelApi;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private TextMeshProUGUI totalAttackText;
    [SerializeField] private TextMeshProUGUI totalDefenseText;
    [SerializeField] private TextMeshProUGUI buffInfoText;
    [SerializeField] private TextMeshProUGUI conditionScoreText;
    [SerializeField] private TextMeshProUGUI aiFeedbackText;
    [SerializeField] private TextMeshProUGUI messageText;

    private LevelPanelApi.UserLevelPanelResponse cachedData;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshPanel);
    }

    private void OnEnable()
    {
        if (BuffManager.Instance != null)
            BuffManager.Instance.OnBuffChanged += RefreshCalculatedStatsOnly;
    }

    private void OnDisable()
    {
        if (BuffManager.Instance != null)
            BuffManager.Instance.OnBuffChanged -= RefreshCalculatedStatsOnly;
    }

    public void OpenPanel()
    {
        if (panelRoot == null)
            return;

        bool nextActiveState = !panelRoot.activeSelf;
        panelRoot.SetActive(nextActiveState);

        if (nextActiveState)
            RefreshPanel();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void RefreshPanel()
    {
        if (levelPanelApi == null)
        {
            SetMessage("LevelPanelApi가 연결되지 않았습니다.");
            return;
        }

        int currentUserId = GetCurrentUserId();

        if (currentUserId <= 0)
        {
            SetMessage("현재 사용 중인 user_id를 찾을 수 없습니다.");
            Debug.LogWarning("CurrentUser에서 현재 user_id를 찾지 못했습니다.");
            return;
        }

        SetMessage("불러오는 중...");

        StartCoroutine(levelPanelApi.GetUserLevelPanel(
            currentUserId,
            response =>
            {
                cachedData = response;

                if (BuffManager.Instance != null)
                {
                    BuffManager.Instance.SetBuffFromServer(
                        response.condition_score,
                        response.condition_grade,
                        response.buff_type,
                        response.current_effect_value
                    );
                }

                Render(response);
                SetMessage("");
            },
            error =>
            {
                SetMessage(error);
                Debug.LogWarning($"레벨 패널 조회 실패: {error}");
            }
        ));
    }

    private int GetCurrentUserId()
    {
        int id = TryGetCurrentUserIdByReflection();

        if (id > 0)
            return id;

        return 0;
    }

    private int TryGetCurrentUserIdByReflection()
    {
        Type currentUserType = FindTypeByName("CurrentUser");

        if (currentUserType == null)
            return 0;

        string[] possibleNames =
        {
            "UserId",
            "userId",
            "CurrentUserId",
            "currentUserId",
            "CurrentId",
            "currentId",
            "Id",
            "id"
        };

        foreach (string name in possibleNames)
        {
            FieldInfo field = currentUserType.GetField(
                name,
                BindingFlags.Public | BindingFlags.Static
            );

            if (field != null && field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(null);

                if (value > 0)
                    return value;
            }

            PropertyInfo property = currentUserType.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Static
            );

            if (property != null && property.PropertyType == typeof(int))
            {
                int value = (int)property.GetValue(null);

                if (value > 0)
                    return value;
            }
        }

        return 0;
    }

    private Type FindTypeByName(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type type = assembly.GetType(typeName);

            if (type != null)
                return type;

            Type[] types = assembly.GetTypes();

            foreach (Type t in types)
            {
                if (t.Name == typeName)
                    return t;
            }
        }

        return null;
    }

    private void RefreshCalculatedStatsOnly()
    {
        if (panelRoot == null || panelRoot.activeSelf == false)
            return;

        if (cachedData == null)
            return;

        Render(cachedData);
    }

    private void Render(LevelPanelApi.UserLevelPanelResponse data)
    {
        if (data == null)
            return;

        int totalAttack = CalculateTotalAttack(data);
        int totalDefense = CalculateTotalDefense(data);

        if (userNameText != null)
            userNameText.text = data.user_name;

        if (totalAttackText != null)
            totalAttackText.text = $"{totalAttack}";

        if (totalDefenseText != null)
            totalDefenseText.text = $"{totalDefense}";

        if (aiFeedbackText != null)
        {
            if (string.IsNullOrEmpty(data.latest_feedback_content))
                aiFeedbackText.text = "아직 생성된 AI 피드백이 없습니다.";
            else
                aiFeedbackText.text = data.latest_feedback_content;
        }

        RefreshBuffTexts();
    }

    private int CalculateTotalAttack(LevelPanelApi.UserLevelPanelResponse data)
    {
        int weaponAttack = 0;

        if (data.weapon_base_attack > 0)
        {
            weaponAttack = GameBalance.EquipmentMainEffect(
                data.weapon_base_attack,
                data.weapon_enhance_level
            );
        }

        float equipmentSubMultiplier = GetEquipmentSubMultiplier(
            data.weapon_enhance_level
        );

        float buffMultiplier = GetCurrentBuffMultiplier();

        return GameBalance.CalculateFinalAttackPowerFromDbValue(
            dbAttackPower: data.base_attack,
            equipmentMainEffectSum: weaponAttack,
            equipmentSubEffectMultiplier: equipmentSubMultiplier,
            characterTypeBonus: GameBalance.CharacterTypeBonus,
            equipOptionBonus: GameBalance.EquipOptionBonus,
            conditionBonus: GameBalance.ConditionBonus,
            activeBuffBonus: buffMultiplier
        );
    }

    private int CalculateTotalDefense(LevelPanelApi.UserLevelPanelResponse data)
    {
        int armorDefense = 0;

        if (data.armor_base_defense > 0)
        {
            armorDefense = GameBalance.EquipmentMainEffect(
                data.armor_base_defense,
                data.armor_enhance_level
            );
        }

        float equipmentSubMultiplier = GetEquipmentSubMultiplier(
            data.armor_enhance_level
        );

        float buffMultiplier = GetCurrentBuffMultiplier();

        return GameBalance.CalculateFinalDefensePowerFromDbValue(
            dbDefensePower: data.base_defense,
            equipmentMainEffectSum: armorDefense,
            equipmentSubEffectMultiplier: equipmentSubMultiplier,
            characterTypeBonus: GameBalance.CharacterTypeBonus,
            equipOptionBonus: GameBalance.EquipOptionBonus,
            conditionBonus: GameBalance.ConditionBonus,
            activeBuffBonus: buffMultiplier
        );
    }

    private float GetEquipmentSubMultiplier(int enhanceLevel)
    {
        int safeLevel = Mathf.Max(GameBalance.EquipmentEnhanceLevelMin, enhanceLevel);
        int n = safeLevel - 1;

        float subRate =
            0.03f * Mathf.Pow(
                GameBalance.EquipmentSubEffectGrowthRate,
                n
            );

        return GameBalance.ConvertSubEffectRateToMultiplier(subRate);
    }

    private float GetCurrentBuffMultiplier()
    {
        if (BuffManager.Instance == null)
            return 1f;

        return BuffManager.Instance.CurrentBuffMultiplier;
    }

    private void RefreshBuffTexts()
    {
        if (BuffManager.Instance == null)
        {
            if (buffInfoText != null)
                buffInfoText.text = "현재 버프 : 없음";

            if (conditionScoreText != null)
                conditionScoreText.text = "컨디션 스코어 : -";

            return;
        }

        if (buffInfoText != null)
        {
            buffInfoText.text =
                $"현재 버프 : {GetBuffTypeKoreanName(BuffManager.Instance.BuffType)} " +
                $"+{BuffManager.Instance.CurrentBuffPercent:0.#}%";
        }

        if (conditionScoreText != null)
        {
            conditionScoreText.text =
                $"컨디션 스코어 : {BuffManager.Instance.ConditionScore}";
        }
    }

    private string GetBuffTypeKoreanName(string buffType)
    {
        if (string.IsNullOrWhiteSpace(buffType))
            return "버프";

        string type = buffType
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");

        switch (type)
        {
            case "ACTIVITY":
                return "활동형";

            case "RESTRAINT":
                return "절제형";

            case "QUEST":
                return "퀘스트형";

            case "OFFLINE":
                return "종료형";

            default:
                return "버프";
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}


[Serializable]
public class UserLevelPanelResponse
{
    public bool success;
    public int user_id;
    public string user_name;

    public int base_attack;
    public int base_defense;

    public int weapon_base_attack;
    public int armor_base_defense;

    public int weapon_enhance_level;
    public int armor_enhance_level;

    public int condition_score;
    public string condition_grade;

    public string buff_type;
    public float current_effect_value;

    public string latest_feedback_content;
}