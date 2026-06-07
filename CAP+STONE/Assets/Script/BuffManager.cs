using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    [Header("Condition")]
    [SerializeField] private int conditionScore = 100;
    [SerializeField] private string conditionGrade = "BEST";

    [Header("Buff")]
    [SerializeField] private string buffType = "ACTIVITY";
    [SerializeField] private float currentEffectValue = 0f;

    [Header("UI")]
    [SerializeField] private Image buffIconImage;
    [SerializeField] private TextMeshProUGUI buffText;
    [SerializeField] private TextMeshProUGUI conditionScoreText;

    [Header("Buff Sprites")]
    [SerializeField] private Sprite activityBuffSprite;
    [SerializeField] private Sprite restraintBuffSprite;
    [SerializeField] private Sprite questBuffSprite;
    [SerializeField] private Sprite offlineBuffSprite;

    public event Action OnBuffChanged;

    public int ConditionScore => conditionScore;
    public string ConditionGrade => conditionGrade;
    public string BuffType => buffType;
    public float CurrentEffectValue => currentEffectValue;

    public float CurrentBuffPercent
    {
        get
        {
            return Mathf.Max(0f, currentEffectValue);
        }
    }

    public float CurrentBuffMultiplier
    {
        get
        {
            return 1f + CurrentBuffPercent / 100f;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void SetBuffFromServer(
        int serverConditionScore,
        string serverConditionGrade,
        string serverBuffType,
        float serverCurrentEffectValue
    )
    {
        conditionScore = Mathf.Max(0, serverConditionScore);

        conditionGrade = string.IsNullOrWhiteSpace(serverConditionGrade)
            ? "BEST"
            : NormalizeText(serverConditionGrade);

        buffType = string.IsNullOrWhiteSpace(serverBuffType)
            ? "ACTIVITY"
            : NormalizeText(serverBuffType);

        currentEffectValue = Mathf.Max(0f, serverCurrentEffectValue);

        RefreshUI();
        OnBuffChanged?.Invoke();
    }

    public void SetCurrentEffectValue(float value)
    {
        currentEffectValue = Mathf.Max(0f, value);

        RefreshUI();
        OnBuffChanged?.Invoke();
    }

    public void SetBuffType(string type)
    {
        buffType = string.IsNullOrWhiteSpace(type)
            ? "ACTIVITY"
            : NormalizeText(type);

        RefreshUI();
        OnBuffChanged?.Invoke();
    }

    public void SetConditionScore(int score)
    {
        conditionScore = Mathf.Max(0, score);

        RefreshUI();
        OnBuffChanged?.Invoke();
    }

    private void RefreshUI()
    {
        RefreshIcon();

        if (buffText != null)
        {
            buffText.text =
                $"{GetBuffTypeKoreanName(buffType)} / " +
                $"{GetConditionGradeKoreanName(conditionGrade)} / " +
                $"+{CurrentBuffPercent:0.#}%";
        }

        if (conditionScoreText != null)
        {
            conditionScoreText.text = $"컨디션 {conditionScore}";
        }
    }

    private void RefreshIcon()
    {
        if (buffIconImage == null)
            return;

        Sprite targetSprite = null;

        switch (NormalizeText(buffType))
        {
            case "ACTIVITY":
                targetSprite = activityBuffSprite;
                break;

            case "RESTRAINT":
                targetSprite = restraintBuffSprite;
                break;

            case "QUEST":
                targetSprite = questBuffSprite;
                break;

            case "OFFLINE":
                targetSprite = offlineBuffSprite;
                break;
        }

        buffIconImage.sprite = targetSprite;
        buffIconImage.enabled = targetSprite != null;
    }

    private string GetBuffTypeKoreanName(string type)
    {
        switch (NormalizeText(type))
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

    private string GetConditionGradeKoreanName(string grade)
    {
        switch (NormalizeText(grade))
        {
            case "BEST":
                return "최상";

            case "GOOD":
                return "좋음";

            case "NORMAL":
                return "보통";

            default:
                return "보통";
        }
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }
}