using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterStatusApi : MonoBehaviour
{
    private const string USER_ID_KEY = "USER_ID";

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Header("References")]
    [SerializeField] private BattleRewardApi battleRewardApi;

    private void Awake()
    {
        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();
    }

    public void OnClickGetCharacterStatuses()
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError(
                "[CharacterStatusApi] USER_ID가 없습니다. " +
                "TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다."
            );
            return;
        }

        StartCoroutine(GetCharacterStatuses(userId));
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

    public IEnumerator GetCharacterStatuses(long userId)
    {
        string url = $"{baseUrl}/users/{userId}/character-statuses/";

        Debug.Log($"[CharacterStatusApi] 캐릭터 상태 조회 API 호출: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[CharacterStatusApi] 캐릭터 상태 조회 실패");
                Debug.LogError($"HTTP Code: {request.responseCode}");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                yield break;
            }

            string responseText = request.downloadHandler.text;

            Debug.Log($"[CharacterStatusApi] 응답: {responseText}");

            string wrappedJson = "{\"characters\":" + responseText + "}";

            CharacterStatusListResponse response;

            try
            {
                response = JsonUtility.FromJson<CharacterStatusListResponse>(wrappedJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CharacterStatusApi] JSON 파싱 실패: {e.Message}");
                Debug.LogError($"Wrapped JSON: {wrappedJson}");
                yield break;
            }

            if (response == null || response.characters == null)
            {
                Debug.LogError("[CharacterStatusApi] 캐릭터 상태 응답이 비어 있습니다.");
                yield break;
            }

            Debug.Log($"[CharacterStatusApi] 캐릭터 상태 조회 성공. 개수 = {response.characters.Length}");

            foreach (CharacterStatusDto character in response.characters)
            {
                Debug.Log(
                    $"character_id={character.character_id}, " +
                    $"level={character.character_level}, " +
                    $"hp={character.current_hp}/{character.max_hp}, " +
                    $"atk={character.attack_power}, def={character.defense_power}"
                );
            }
        }
    }

    // =========================================================
    // 추가: 유저 상태 조회
    // level, exp, gem, gold 같은 플레이어 계정 상태용
    // =========================================================

    public void LoadUserStatus(Action<bool, UserStatusDto> onComplete)
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError(
                "[CharacterStatusApi] USER_ID가 없습니다. " +
                "유저 상태를 조회할 수 없습니다."
            );

            onComplete?.Invoke(false, null);
            return;
        }

        StartCoroutine(GetUserStatus(userId, onComplete));
    }

    private IEnumerator GetUserStatus(long userId, System.Action<bool, UserStatusDto> onComplete)
{
    string url = $"{baseUrl}/users/{userId}/status";

    Debug.Log($"[CharacterStatusApi] 유저 상태 조회 API 호출: {url}");

    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[CharacterStatusApi] 유저 상태 조회 실패");
            Debug.LogError($"HTTP Code: {request.responseCode}");
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");

            onComplete?.Invoke(false, null);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log($"[CharacterStatusApi] 유저 상태 응답: {responseText}");

        UserStatusDto response;

        try
        {
            response = JsonUtility.FromJson<UserStatusDto>(responseText);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterStatusApi] JSON 파싱 실패: {e.Message}");
            Debug.LogError($"Response: {responseText}");

            onComplete?.Invoke(false, null);
            yield break;
        }

        onComplete?.Invoke(true, response);
    }
}

    [Serializable]
    public class CharacterStatusListResponse
    {
        public CharacterStatusDto[] characters;
    }

    [Serializable]
    public class CharacterStatusDto
    {
        public long user_id;
        public long character_id;
        public int character_level;
        public int max_hp;
        public int current_hp;
        public int attack_power;
        public int defense_power;
    }

    [Serializable]
    public class UserStatusDto
    {
        public long user_id;
        public int level;
        public int exp;
        public int required_exp;
        public int gem;
    }
}