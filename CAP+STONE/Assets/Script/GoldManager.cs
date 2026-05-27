using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI goldText;

    [Header("Settings")]
    public int currentGold = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => UpdateUI();

    public void AddGold(int amount)
    {
        currentGold = Mathf.Max(0, currentGold + amount);
        UpdateUI();
    }

    public bool CanSpendGold(int amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (!CanSpendGold(amount))
        {
            return false;
        }

        currentGold -= amount;
        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (goldText != null)
            goldText.text = currentGold.ToString();
    }
}
