using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    public void SetStatus(int level, int exp, int gem)
    {
        int requiredExp = Mathf.CeilToInt(1000 * Mathf.Pow(1.16f, Mathf.Max(1, level) - 1));
        SetStatus(level, exp, requiredExp, gem);
    }

    public void SetStatus(int level, int exp, int requiredExp, int gem)
    {
        int safeLevel = Mathf.Max(1, level);
        int safeExp = Mathf.Max(0, exp);
        int safeRequiredExp = Mathf.Max(1, requiredExp);

        if (levelText != null)
            levelText.text = safeLevel.ToString();

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = (float)safeExp / safeRequiredExp;
        }

        if (expText != null)
            expText.text = $"{safeExp} / {safeRequiredExp}";

        Debug.Log(
            $"[PlayerLevelUI] UI 갱신: Lv.{safeLevel}, EXP {safeExp}/{safeRequiredExp}, GEM {gem}"
        );
    }
}