using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("References")]
    public Canvas        uiCanvas;
    public RectTransform goldTarget;
    public GameObject    coinPrefab;

    [Header("Settings")]
    public int coinsPerKill = 5;
    public int goldPerCoin  = 10;

    public void SpawnCoins(Vector3 worldPos)
    {
        if (!ValidateRefs()) return;

        Camera cam = Camera.main;
        if (cam == null) { Debug.LogError("[CoinSpawner] Camera.main 없음 — Main Camera 태그 확인"); return; }

        Vector3 screenPos      = cam.WorldToScreenPoint(worldPos);
        RectTransform canvasRt = uiCanvas.GetComponent<RectTransform>();
        Camera uiCam           = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        Vector2 canvasLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos, uiCam, out canvasLocalPos);

        for (int i = 0; i < coinsPerKill; i++)
        {
            GameObject    coinObj = Instantiate(coinPrefab, canvasRt);
            RectTransform coinRt  = coinObj.GetComponent<RectTransform>();
            // localPosition은 부모 pivot 기준 — anchoredPosition 앵커 설정에 무관하게 정확히 위치함
            coinRt.localPosition = new Vector3(canvasLocalPos.x, canvasLocalPos.y, 0f);

            CoinUI coinUI = coinObj.GetComponent<CoinUI>();
            if (coinUI != null)
                coinUI.Init(goldTarget, goldPerCoin);
            else
                Debug.LogWarning("[CoinSpawner] coinPrefab에 CoinUI 컴포넌트 없음");
        }
    }

    bool ValidateRefs()
    {
        bool ok = true;
        if (uiCanvas   == null) { Debug.LogError("[CoinSpawner] Ui Canvas 미연결");   ok = false; }
        if (goldTarget == null) { Debug.LogError("[CoinSpawner] Gold Target 미연결"); ok = false; }
        if (coinPrefab == null) { Debug.LogError("[CoinSpawner] Coin Prefab 미연결"); ok = false; }
        return ok;
    }
}
