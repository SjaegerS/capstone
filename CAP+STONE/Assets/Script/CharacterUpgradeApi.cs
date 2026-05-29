using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterUpgradeApi : MonoBehaviour
{
    public static CharacterUpgradeApi Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Header("User")]
    [SerializeField] private UserCreateApi userCreateApi;

    [Serializable]
    public class UserStatusResponse
    {
        public long user_id;
        public long current_character_id;

        public int player_level;
        public int player_exp;
        public int required_exp;

        public int current_stage;
        public int total_boss_kill_count;

        public int max_hp;
        public int attack_power;
        public int defense_power;

        public int hp_upgrade_lvl;
        public int attack_upgrade_lvl;
        public int defense_upgrade_lvl;
    }

    [Serializable]
    public class CharacterUpgradeResponse
    {
        public long user_id;
        public string upgrade_type;

        public int max_hp;
        public int attack_power;
        public int defense_power;

        public int hp_upgrade_lvl;
        public int attack_upgrade_lvl;
        public int defense_upgrade_lvl;

        // 현재 누른 버튼에 해당하는 강화 레벨
        public int upgrade_lvl;

        public long gold;
        public int cost_gold;

        public bool success;
        public string message;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (userCreateApi == null)
        {
            userCreateApi = FindFirstObjectByType<UserCreateApi>();
        }
    }

    public long GetUserId()
    {
        if (userCreateApi != null && userCreateApi.CurrentUserId > 0)
        {
            return userCreateApi.CurrentUserId;
        }

        if (PlayerPrefs.HasKey("TEST_USER_ID") &&
            long.TryParse(PlayerPrefs.GetString("TEST_USER_ID"), out long testUserId))
        {
            return testUserId;
        }

        int savedUserId = PlayerPrefs.GetInt("user_id", 0);

        if (savedUserId > 0)
        {
            return savedUserId;
        }

        return 0;
    }

    public IEnumerator LoadUserStatus(
        Action<bool, UserStatusResponse> onComplete
    )
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[CharacterUpgradeApi] user_id가 없습니다.");
            onComplete?.Invoke(false, null);
            yield break;
        }

        string url = $"{baseUrl}/battle/status/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[CharacterUpgradeApi] 유저 상태 로드 실패\n" +
                    $"Code: {request.responseCode}\n" +
                    $"URL: {url}\n" +
                    $"Response: {request.downloadHandler.text}"
                );

                onComplete?.Invoke(false, null);
                yield break;
            }

            UserStatusResponse response;

            try
            {
                response = JsonUtility.FromJson<UserStatusResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[CharacterUpgradeApi] 유저 상태 파싱 실패: {e.Message}\n" +
                    $"Response: {request.downloadHandler.text}"
                );

                onComplete?.Invoke(false, null);
                yield break;
            }

            onComplete?.Invoke(true, response);
        }
    }

    public IEnumerator UpgradeCharacter(
        bool isHealthUpgrade,
        int currentUpgradeLvl,
        int costGold,
        int nextMaxHp,
        int nextAttackPower,
        Action<bool, CharacterUpgradeResponse> onComplete
    )
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError("[CharacterUpgradeApi] user_id가 없습니다.");
            onComplete?.Invoke(false, null);
            yield break;
        }

        string endpoint = isHealthUpgrade
            ? $"/users/{userId}/status/upgrade-hp/"
            : $"/users/{userId}/status/upgrade-attack/";

        string url = $"{baseUrl}{endpoint}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[CharacterUpgradeApi] 강화 실패\n" +
                    $"Code: {request.responseCode}\n" +
                    $"URL: {url}\n" +
                    $"Response: {request.downloadHandler.text}"
                );

                onComplete?.Invoke(false, null);
                yield break;
            }

            CharacterUpgradeResponse response;

            try
            {
                response = JsonUtility.FromJson<CharacterUpgradeResponse>(
                    request.downloadHandler.text
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[CharacterUpgradeApi] 응답 파싱 실패: {e.Message}\n" +
                    $"Response: {request.downloadHandler.text}"
                );

                onComplete?.Invoke(false, null);
                yield break;
            }

            if (response == null)
            {
                Debug.LogError(
                    $"[CharacterUpgradeApi] 응답이 null입니다.\n" +
                    $"Response: {request.downloadHandler.text}"
                );

                onComplete?.Invoke(false, null);
                yield break;
            }

            response.success = true;

            response.upgrade_lvl = isHealthUpgrade
                ? response.hp_upgrade_lvl
                : response.attack_upgrade_lvl;

            if (string.IsNullOrEmpty(response.message))
            {
                response.message = isHealthUpgrade
                    ? "체력 강화 완료"
                    : "공격력 강화 완료";
            }

            Debug.Log(
                $"[CharacterUpgradeApi] 강화 성공\n" +
                $"Type: {response.upgrade_type}\n" +
                $"HP: {response.max_hp}, ATK: {response.attack_power}, DEF: {response.defense_power}\n" +
                $"HP_Lv: {response.hp_upgrade_lvl}, ATK_Lv: {response.attack_upgrade_lvl}, DEF_Lv: {response.defense_upgrade_lvl}\n" +
                $"Selected_Lv: {response.upgrade_lvl}\n" +
                $"Gold: {response.gold}, Cost: {response.cost_gold}"
            );

            onComplete?.Invoke(true, response);
        }
    }
}