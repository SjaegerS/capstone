using TMPro;
using UnityEngine;

public class CurrencyUI : MonoBehaviour
{
    public static CurrencyUI Instance { get; private set; }

    [Header("Currency Text")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;

    private int currentGold;
    private int currentGem;

    public int CurrentGold => currentGold;
    public int CurrentGem => currentGem;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCurrency(int gold, int gem)
    {
        currentGold = Mathf.Max(0, gold);
        currentGem = Mathf.Max(0, gem);

        RefreshUI();
    }

    public void SetGold(int gold)
    {
        currentGold = Mathf.Max(0, gold);
        RefreshGold();
    }

    public void SetGem(int gem)
    {
        currentGem = Mathf.Max(0, gem);
        RefreshGem();
    }

    public void AddGold(int amount)
    {
        currentGold = Mathf.Max(0, currentGold + amount);
        RefreshGold();
    }

    public void AddGem(int amount)
    {
        currentGem = Mathf.Max(0, currentGem + amount);
        RefreshGem();
    }

    private void RefreshUI()
    {
        RefreshGold();
        RefreshGem();
    }

    private void RefreshGold()
    {
        if (goldText != null)
            goldText.text = FormatCurrency(currentGold);
    }

    private void RefreshGem()
    {
        if (gemText != null)
            gemText.text = FormatCurrency(currentGem);
    }

    public static string FormatCurrency(int value)
    {
        if (value < 1000)
            return value.ToString();

        string[] units = { "", "a", "b", "c", "d", "e", "f" };

        double displayValue = value;
        int unitIndex = 0;

        while (displayValue >= 1000.0 && unitIndex < units.Length - 1)
        {
            displayValue /= 1000.0;
            unitIndex++;
        }

        if (displayValue >= 100)
            return displayValue.ToString("0") + units[unitIndex];

        if (displayValue >= 10)
            return displayValue.ToString("0.#") + units[unitIndex];

        return displayValue.ToString("0.##") + units[unitIndex];
    }
}