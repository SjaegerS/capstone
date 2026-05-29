using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BattleRewardApi : MonoBehaviour
{
    public static BattleRewardApi Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";
    [SerializeField] private string rewardEndpoint = "/battle/reward";

    [Header("User")]
    [SerializeField] private int userIdOverride = 7;
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
        }

    [Serializable]
    public class BattleRewardRequest
    {
        public int user_id;
        public int stage_id;
        public int reward_gold;
        public int reward_exp;
    }

    [Serializable]
    public class BattleRewardResponse
    {
        public bool success;
        public string message;
        public int user_id;
        public int current_stage;
        public int gold;
        public int exp;
        public int level;
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
            return userIdOverride;

        return PlayerPrefs.GetInt("user_id", 0);
    }

    public IEnumerator GetUserStatus(int userId, Action<UserStatusResponse> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}/battle/status/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"GetUserStatus 실패: {request.error}";
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
                string error = $"GetUserStatus JSON 파싱 실패: {e.Message}";
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
        Action<BattleRewardResponse> onSuccess = null,
        Action<string> onError = null)
    {
        string url = $"{baseUrl}{rewardEndpoint}";

        BattleRewardRequest data = new BattleRewardRequest
        {
            user_id = userId,
            stage_id = stageId,
            reward_gold = rewardGold,
            reward_exp = rewardExp
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"SaveBattleReward 실패: {request.error}\n응답: {request.downloadHandler.text}";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            try
            {
                BattleRewardResponse response =
                    JsonUtility.FromJson<BattleRewardResponse>(request.downloadHandler.text);

                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                string error = $"SaveBattleReward JSON 파싱 실패: {e.Message}\n응답: {request.downloadHandler.text}";
                Debug.LogError(error);
                onError?.Invoke(error);
            }
        }
    }
}