using UnityEngine;
using System.Collections;

public class CoinUI : MonoBehaviour
{
    [Header("Animation")]
    public float popUpDistance = 60f;
    public float popDuration   = 0.35f;
    public float flyDuration   = 0.5f;
    public float spreadX       = 40f;

    private RectTransform rt;
    private RectTransform goldTarget;
    private int goldValue;

    public void Init(RectTransform target, int gold)
    {
        rt         = GetComponent<RectTransform>();
        goldTarget = target;
        goldValue  = gold;
        StartCoroutine(AnimateCoin());
    }

    IEnumerator AnimateCoin()
    {
        // Pop-up phase
        Vector3 startPos = rt.position;
        float xOffset    = Random.Range(-spreadX, spreadX);
        Vector3 popPos   = startPos + new Vector3(xOffset, popUpDistance, 0f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(popDuration, 0.01f))
        {
            rt.position = Vector3.Lerp(startPos, popPos, EaseOut(t));
            yield return null;
        }
        rt.position = popPos;

        // Fly-to-target phase
        Vector3 flyStart = rt.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(flyDuration, 0.01f))
        {
            rt.position = Vector3.Lerp(flyStart, goldTarget.position, EaseIn(t));
            yield return null;
        }

        Destroy(gameObject);
    }

    static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    static float EaseIn(float t)  { t = Mathf.Clamp01(t); return t * t; }
}
