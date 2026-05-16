using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Position")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Size (World Units)")]
    public Vector2 barSize = new Vector2(1.0f, 0.15f);

    [Header("Colors")]
    public Color backColor = Color.black;
    public Color fillColor = Color.red;

    private Image fillImage;
    private GameObject canvasRoot;

    void Awake()
    {
        BuildBar();
    }

    void BuildBar()
    {
        canvasRoot = new GameObject("_HPBar");
        canvasRoot.transform.SetParent(transform);
        canvasRoot.transform.localPosition = worldOffset;
        canvasRoot.transform.localRotation = Quaternion.identity;
        // 1 pixel = 0.01 world unit → barSize(1.0, 0.15) → sizeDelta(100, 15)
        canvasRoot.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = 10;

        RectTransform rt = canvasRoot.GetComponent<RectTransform>();
        rt.sizeDelta = barSize * 100f;

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasRoot.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = backColor;
        Stretch(bgImg.rectTransform);

        // Fill (left-to-right filled image)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(canvasRoot.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        Stretch(fillImage.rectTransform);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public void SetRatio(float ratio)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(ratio);
    }

    public void SetVisible(bool visible)
    {
        if (canvasRoot != null)
            canvasRoot.SetActive(visible);
    }
}
