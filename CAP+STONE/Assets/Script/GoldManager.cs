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
        currentGold += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (goldText != null)
            goldText.text = currentGold.ToString();
    }
}
