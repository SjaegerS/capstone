using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UserCurrencyApi : MonoBehaviour
{
    private const string USER_ID_KEY = "USER_ID";

    [Header("References")]
    [SerializeField] private BattleRewardApi battleRewardApi;

    private void Awake()
    {
        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();
    }

    public void OnClickGetCurrency()
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError(
                "[UserCurrencyApi] USER_ID가 없습니다. " +
                "TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다."
            );
            return;
        }

        StartCoroutine(GetCurrency(userId));
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

    public IEnumerator GetCurrency(long userId)
    {
        string url = $"{ApiConfig.BaseUrl}/users/{userId}/currency/";

        Debug.Log($"[UserCurrencyApi] 재화 조회 API 호출: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[UserCurrencyApi] 재화 조회 실패");
                Debug.LogError($"HTTP Code: {request.responseCode}");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            UserCurrencyResponse response;

            try
            {
                response = JsonUtility.FromJson<UserCurrencyResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserCurrencyApi] JSON 파싱 실패: {e.Message}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            if (response == null)
            {
                Debug.LogError("[UserCurrencyApi] 재화 응답이 null입니다.");
                yield break;
            }

            Debug.Log("[UserCurrencyApi] 재화 조회 성공");
            Debug.Log($"user_id={response.user_id}");
            Debug.Log($"gold={response.gold}");
            Debug.Log($"gem={response.gem}");

            CurrencyUIManager.Instance?.SetGold(response.gold);
            CurrencyUIManager.Instance?.SetGem(response.gem);
            GoldManager.Instance?.SetGold(response.gold);
        }
    }

    [Serializable]
    public class UserCurrencyResponse
    {
        public long user_id;
        public long gold;
        public long gem;
        public string updated_at;
    }
}