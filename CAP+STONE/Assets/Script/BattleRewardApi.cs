using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BattleRewardApi : MonoBehaviour
{
    public static BattleRewardApi Instance { get; private set; }

    public const string USER_ID_KEY = "USER_ID";

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";
    //[SerializeField] private string rewardEndpoint = "/battle/reward";

    [Header("User")]
    [Tooltip("0이면 TitleManager에서 저장한 USER_ID 사용. 테스트로 특정 유저를 강제할 때만 입력.")]
    [SerializeField] private int userIdOverride = 0;

    [Serializable]
    public class UserStatusResponse
    {
        public bool success;
        public int user_id;

        public int player_level;
        public int player_exp;
        public int required_exp;

        public int current_stage;
        public int total_boss_kill_count;

        public string message;

        public int max_hp;
        public int attack_power;
        public int defense_power;

        public int hp_upgrade_lvl;
        public int attack_upgrade_lvl;
        public int defense_upgrade_lvl;

        public int gold;
    }

    [Serializable]
    public class BattleRewardRequest
    {
        public int user_id;
        public int stage_id;
        public int reward_gold;
        public int reward_exp;
        public int kill_count_add;
        public bool is_clear;
    }

    [Serializable]
    public class BattleRewardResponse
    {
        public bool success;
        public string message;

        public int user_id;
        public bool is_clear;

        public int cleared_stage;
        public int current_stage;
        public int max_cleared_stage;

        public int reward_gold;
        public int reward_exp;

        public int gold;
        public int gem;

        public int exp;
        public int level;
        public int required_exp;
        public int level_up_count;

        public int total_boss_kill_count;

        public int max_hp;
        public int attack_power;
        public int defense_power;
    }

    [Serializable]
    public class AddGoldRequest
    {
        public int amount;
    }

    [Serializable]
    public class AiDemoFeedbackRequest
    {
        public int user_id;
        public int completed_count;
        public int total_count;
    }

    [Serializable]
    public class AiDemoFeedbackResponse
    {
        public int user_id;
        public int completed_count;
        public int total_count;
        public string condition_result;
        public string feedback_text;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public int GetUserId()
    {
        if (userIdOverride > 0)
        {
            CurrentUser.UserId = userIdOverride;
            return userIdOverride;
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

    public void SaveUserId(int userId)
    {
        if (userId <= 0)
            return;

        CurrentUser.UserId = userId;

        PlayerPrefs.SetInt(USER_ID_KEY, userId);
        PlayerPrefs.Save();

        Debug.Log($"[BattleRewardApi] USER_ID 저장: {userId}");
    }

    public void ClearSavedUserId()
    {
        CurrentUser.UserId = -1;

        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.Save();

        Debug.Log("[BattleRewardApi] USER_ID 삭제 완료");
    }

    public IEnumerator GetUserStatus(
        int userId,
        Action<UserStatusResponse> onSuccess,
        Action<string> onError = null)
    {
        string url = $"{baseUrl}/battle/status/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error =
                    $"GetUserStatus 실패\n" +
                    $"url: {url}\n" +
                    $"code: {request.responseCode}\n" +
                    $"error: {request.error}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            try
            {
                UserStatusResponse response =
                    JsonUtility.FromJson<UserStatusResponse>(request.downloadHandler.text);

                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                string error =
                    $"GetUserStatus JSON 파싱 실패: {e.Message}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }

   public IEnumerator SaveBattleReward(
        int userId,
        int stageId,
        int rewardGold,
        int rewardExp,
        int killCountAdd,
        bool isClear,
        Action<BattleRewardResponse> onSuccess,
        Action<string> onError
    )
    {
        string url = $"{baseUrl}/battle/reward";

        BattleRewardRequest body = new BattleRewardRequest
        {
            user_id = userId,
            stage_id = stageId,
            reward_gold = rewardGold,
            reward_exp = rewardExp,
            kill_count_add = killCountAdd,
            is_clear = isClear
        };

        string json = JsonUtility.ToJson(body);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage =
                    "SaveBattleReward 실패\n" +
                    $"url: {url}\n" +
                    $"code: {request.responseCode}\n" +
                    $"error: {request.error}\n" +
                    $"response: {request.downloadHandler.text}";

                onError?.Invoke(errorMessage);
                yield break;
            }

            BattleRewardResponse response =
                JsonUtility.FromJson<BattleRewardResponse>(request.downloadHandler.text);

            onSuccess?.Invoke(response);
        }
    }
    public IEnumerator AddGoldToUser(
        int userId,
        int amount,
        Action<UserStatusResponse> onSuccess = null,
        Action<string> onError = null)
    {
        string url = $"{baseUrl}/user-status/{userId}/gold";

        AddGoldRequest data = new AddGoldRequest
        {
            amount = amount
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error =
                    $"AddGoldToUser 실패\n" +
                    $"url: {url}\n" +
                    $"code: {request.responseCode}\n" +
                    $"error: {request.error}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            try
            {
                UserStatusResponse response =
                    JsonUtility.FromJson<UserStatusResponse>(request.downloadHandler.text);

                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                string error =
                    $"AddGoldToUser JSON 파싱 실패: {e.Message}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }

    public IEnumerator SendAiDemoFeedback(
        int userId,
        int completedCount,
        int totalCount,
        Action<AiDemoFeedbackResponse> onSuccess = null,
        Action<string> onError = null)
    {
        string url = $"{baseUrl}/ai/demo-feedback";

        AiDemoFeedbackRequest data = new AiDemoFeedbackRequest
        {
            user_id = userId,
            completed_count = completedCount,
            total_count = totalCount
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error =
                    $"SendAiDemoFeedback 실패\n" +
                    $"url: {url}\n" +
                    $"code: {request.responseCode}\n" +
                    $"error: {request.error}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            try
            {
                AiDemoFeedbackResponse response =
                    JsonUtility.FromJson<AiDemoFeedbackResponse>(request.downloadHandler.text);

                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                string error =
                    $"SendAiDemoFeedback JSON 파싱 실패: {e.Message}\n" +
                    $"response: {request.downloadHandler.text}";

                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }
}