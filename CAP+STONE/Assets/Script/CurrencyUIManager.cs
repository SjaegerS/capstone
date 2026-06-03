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

    private void Awake()
    {
        instance = this;

        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();
    }

    private void Start()
    {
        StartCoroutine(LoadCurrencyWhenUserReady());
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
        // 1. BattleRewardApi 기준으로 가져오기
        if (battleRewardApi != null)
        {
            int apiUserId = battleRewardApi.GetUserId();

            if (apiUserId > 0)
            {
                CurrentUser.UserId = apiUserId;
                return apiUserId;
            }
        }

        // 2. CurrentUser에서 가져오기
        if (CurrentUser.UserId > 0)
        {
            return CurrentUser.UserId;
        }

        // 3. PlayerPrefs USER_ID에서 가져오기
        int savedUserId = PlayerPrefs.GetInt(USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            CurrentUser.UserId = savedUserId;
            return savedUserId;
        }

        return -1;
    }

    public IEnumerator LoadCurrencyFromDb()
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[CurrencyUIManager] LoadCurrencyFromDb 실패: USER_ID가 없습니다.");
            yield break;
        }

        string url = $"{baseUrl}/users/{userId}/currency/";

        Debug.Log($"[CurrencyUIManager] 재화 조회 API 호출: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

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

            SetGold(response.gold);
            SetGem(response.gem);

            Debug.Log($"[CurrencyUIManager] 재화 로드 완료. Gold={response.gold}, Gem={response.gem}");
        }
    }

    public void SetGold(long amount)
    {
        GoldManager.Instance?.SetGold(amount);

        if (goldText == null)
            return;

        goldText.textWrappingMode = TextWrappingModes.NoWrap;
        goldText.overflowMode = TextOverflowModes.Overflow;
        goldText.text = Math.Max(0L, amount).ToString();
    }

    public void SetGem(long amount)
    {
        if (gemText == null)
            return;

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