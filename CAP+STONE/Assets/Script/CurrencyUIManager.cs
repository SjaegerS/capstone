using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CurrencyUIManager : MonoBehaviour
{
    private const string USER_ID_KEY = "USER_ID";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Header("References")]
    [SerializeField] private BattleRewardApi battleRewardApi;

    private static CurrencyUIManager instance;
    public static CurrencyUIManager Instance => instance;

    private long currentGold;
    private long currentGem;

    public long CurrentGold => currentGold;
    public long CurrentGem => currentGem;

    private bool isLoadingCurrency;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();

        SetupText(goldText);
        SetupText(gemText);
    }

    private void Start()
    {
        StartCoroutine(LoadCurrencyWhenUserReady());
    }

    private void SetupText(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private IEnumerator LoadCurrencyWhenUserReady()
    {
        float timeout = 5f;
        float elapsed = 0f;

        long userId = GetUserId();

        while (userId <= 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            userId = GetUserId();
            yield return null;
        }

        if (userId <= 0)
        {
            Debug.LogWarning(
                "[CurrencyUIManager] USER_ID가 준비되지 않았습니다. " +
                "TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다."
            );
            yield break;
        }

        yield return StartCoroutine(LoadCurrencyFromDb());
    }

    private long GetUserId()
    {
        if (battleRewardApi != null)
        {
            int apiUserId = battleRewardApi.GetUserId();

            if (apiUserId > 0)
            {
                CurrentUser.UserId = apiUserId;
                return apiUserId;
            }
        }

        if (CurrentUser.UserId > 0)
            return CurrentUser.UserId;

        int savedUserId = PlayerPrefs.GetInt(USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            CurrentUser.UserId = savedUserId;
            return savedUserId;
        }

        return -1;
    }

    public void RefreshFromDb()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(LoadCurrencyFromDb());
    }

    public IEnumerator LoadCurrencyFromDb()
    {
        if (isLoadingCurrency)
            yield break;

        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[CurrencyUIManager] LoadCurrencyFromDb 실패: USER_ID가 없습니다.");
            yield break;
        }

        isLoadingCurrency = true;

        string url = $"{baseUrl}/users/{userId}/currency/";

        Debug.Log($"[CurrencyUIManager] 재화 조회 API 호출: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            isLoadingCurrency = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[CurrencyUIManager] currency 조회 실패");
                Debug.LogError($"HTTP Code: {request.responseCode}");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            CurrencyResponse response;

            try
            {
                response = JsonUtility.FromJson<CurrencyResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CurrencyUIManager] currency JSON 파싱 실패: {e.Message}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            if (response == null)
            {
                Debug.LogError("[CurrencyUIManager] currency 응답이 null입니다.");
                yield break;
            }

            SetCurrency(response.gold, response.gem);

            Debug.Log($"[CurrencyUIManager] 재화 로드 완료. Gold={response.gold}, Gem={response.gem}");
        }
    }

    public void SetCurrency(long gold, long gem)
    {
        currentGold = Math.Max(0L, gold);
        currentGem = Math.Max(0L, gem);

        RefreshGoldText();
        RefreshGemText();
    }

    public void SetGold(long amount)
    {
        currentGold = Math.Max(0L, amount);
        RefreshGoldText();
    }

    public void SetGem(long amount)
    {
        currentGem = Math.Max(0L, amount);
        RefreshGemText();
    }

    public void AddGold(long amount)
    {
        SetGold(currentGold + amount);
    }

    public void AddGem(long amount)
    {
        SetGem(currentGem + amount);
    }

    public bool CanSpendGold(long amount)
    {
        return amount >= 0 && currentGold >= amount;
    }

    public bool TrySpendGoldLocalOnly(long amount)
    {
        if (!CanSpendGold(amount))
            return false;

        SetGold(currentGold - amount);
        return true;
    }

    private void RefreshGoldText()
    {
        if (goldText == null)
            return;

        SetupText(goldText);
        goldText.text = FormatCurrency(currentGold);
    }

    private void RefreshGemText()
    {
        if (gemText == null)
            return;

        SetupText(gemText);
        gemText.text = FormatCurrency(currentGem);
    }

    public static string FormatCurrency(long value)
    {
        value = Math.Max(0L, value);

        if (value < 1000)
            return value.ToString();

        string[] units = { "", "a", "b", "c", "d", "e", "f", "g" };

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

    [Serializable]
    private class CurrencyResponse
    {
        public long user_id;
        public long gold;
        public long gem;
        public string updated_at;
    }
}