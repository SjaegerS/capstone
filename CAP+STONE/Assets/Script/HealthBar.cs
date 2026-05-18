using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Position")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    private Transform owner;

    // UnitController.Initialize()에서 호출 — 추적 대상 등록 및 즉시 위치 반영
    public void Setup(Transform ownerTransform)
    {
        owner = ownerTransform;
        UpdatePosition();
    }

    void LateUpdate()
    {
        if (owner != null)
            UpdatePosition();
    }

    void UpdatePosition()
    {
        transform.position = owner.position + worldOffset;
    }

    public void SetHP(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(max > 0f ? current / max : 0f);
        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    public void SetRatio(float ratio)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(ratio);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
