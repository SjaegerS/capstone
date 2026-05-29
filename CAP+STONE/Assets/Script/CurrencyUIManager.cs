using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CurrencyUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;

    [Header("API")]
    [SerializeField] private UserCreateApi userCreateApi;
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    private static CurrencyUIManager instance;

    public static CurrencyUIManager Instance => instance;

    private void Awake()
    {
        instance = this;

        if (userCreateApi == null)
        {
            userCreateApi = FindFirstObjectByType<UserCreateApi>();
        }
    }

    private void Start()
    {
        StartCoroutine(LoadCurrencyWhenUserReady());
    }

    private IEnumerator LoadCurrencyWhenUserReady()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while ((userCreateApi == null || userCreateApi.CurrentUserId <= 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (userCreateApi == null || userCreateApi.CurrentUserId <= 0)
        {
            Debug.LogWarning("CurrencyUIManager: user_id가 준비되지 않았습니다.");
            yield break;
        }

        yield return StartCoroutine(LoadCurrencyFromDb());
    }

    public IEnumerator LoadCurrencyFromDb()
    {
        string url = $"{baseUrl}/users/{userCreateApi.CurrentUserId}/currency/";

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("currency 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        CurrencyResponse response = JsonUtility.FromJson<CurrencyResponse>(request.downloadHandler.text);

        if (response == null)
        {
            yield break;
        }

        SetGold(response.gold);
        SetGem(response.gem);
    }

    public void SetGold(long amount)
    {
        GoldManager.Instance?.SetGold(amount);

        if (goldText == null)
        {
            return;
        }

        goldText.textWrappingMode = TextWrappingModes.NoWrap;
        goldText.overflowMode = TextOverflowModes.Overflow;
        goldText.text = Math.Max(0L, amount).ToString();
    }

    public void SetGem(long amount)
    {
        if (gemText == null)
        {
            return;
        }

        gemText.textWrappingMode = TextWrappingModes.NoWrap;
        gemText.overflowMode = TextOverflowModes.Overflow;
        gemText.text = Math.Max(0L, amount).ToString();
    }

    [Serializable]
    private class CurrencyResponse
    {
        public long user_id;
        public long gold;
        public long gem;
        public string updated_at;
    }
}
