using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterUpgradeApi : MonoBehaviour
{
    public static CharacterUpgradeApi Instance { get; private set; }

    private const string USER_ID_KEY = "USER_ID";

    [Header("API")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";

    [Header("User")]
    [Tooltip("0이면 TitleManager에서 생성/저장한 USER_ID 사용. 테스트용으로 특정 ID를 강제할 때만 입력.")]
    [SerializeField] private int userIdOverride = 0;

    [Header("References")]
    [SerializeField] private BattleRewardApi battleRewardApi;

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

        public long gold;
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

        if (battleRewardApi == null)
            battleRewardApi = BattleRewardApi.Instance;

        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();
    }

    public long GetUserId()
    {
        // 1. Inspector에서 강제 지정한 ID
        // 평소에는 반드시 0으로 둬야 함.
        if (userIdOverride > 0)
        {
            CurrentUser.UserId = userIdOverride;
            return userIdOverride;
        }

        // 2. BattleRewardApi 기준 ID 사용
        if (battleRewardApi != null)
        {
            int apiUserId = battleRewardApi.GetUserId();

            if (apiUserId > 0)
            {
                CurrentUser.UserId = apiUserId;
                return apiUserId;
            }
        }

        // 3. TitleManager가 씬 이동 전에 넣어둔 현재 유저 ID
        if (CurrentUser.UserId > 0)
        {
            return CurrentUser.UserId;
        }

        // 4. PlayerPrefs에 저장된 USER_ID
        int savedUserId = PlayerPrefs.GetInt(USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            CurrentUser.UserId = savedUserId;
            return savedUserId;
        }

        // 5. 예전 키 정리용 fallback
        // 기존 user_id 또는 TEST_USER_ID가 남아 있어도 이제는 사용하지 않는 게 원칙임.
        return -1;
    }

    public IEnumerator LoadUserStatus(
        Action<bool, UserStatusResponse> onComplete
    )
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError(
                "[CharacterUpgradeApi] USER_ID가 없습니다. " +
                "TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다."
            );

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


    public enum UpgradeStatType
    {
        Hp,
        Attack,
        Defense
    }
    
    public IEnumerator UpgradeCharacter(
        UpgradeStatType upgradeType,
        int currentUpgradeLvl,
        int costGold,
        int nextMaxHp,
        int nextAttackPower,
        int nextDefensePower,
        Action<bool, CharacterUpgradeResponse> onComplete
    )
    {
        long userId = GetUserId();

        if (userId <= 0)
        {
            Debug.LogError(
                "[CharacterUpgradeApi] USER_ID가 없습니다. " +
                "TitleScene에서 게임 시작 버튼으로 유저를 먼저 생성해야 합니다."
            );

            onComplete?.Invoke(false, null);
            yield break;
        }

        string endpoint;

        switch (upgradeType)
        {
            case UpgradeStatType.Hp:
                endpoint = $"/users/{userId}/status/upgrade-hp/";
                break;

            case UpgradeStatType.Attack:
                endpoint = $"/users/{userId}/status/upgrade-attack/";
                break;

            case UpgradeStatType.Defense:
                endpoint = $"/users/{userId}/status/upgrade-defense/";
                break;

            default:
                Debug.LogError("[CharacterUpgradeApi] 알 수 없는 강화 타입입니다.");
                onComplete?.Invoke(false, null);
                yield break;
        }

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

            switch (upgradeType)
            {
                case UpgradeStatType.Hp:
                    response.upgrade_lvl = response.hp_upgrade_lvl;
                    break;

                case UpgradeStatType.Attack:
                    response.upgrade_lvl = response.attack_upgrade_lvl;
                    break;

                case UpgradeStatType.Defense:
                    response.upgrade_lvl = response.defense_upgrade_lvl;
                    break;
            }

            if (string.IsNullOrEmpty(response.message))
            {
                switch (upgradeType)
                {
                    case UpgradeStatType.Hp:
                        response.message = "체력 강화 완료";
                        break;

                    case UpgradeStatType.Attack:
                        response.message = "공격력 강화 완료";
                        break;

                    case UpgradeStatType.Defense:
                        response.message = "방어력 강화 완료";
                        break;
                }
            }

            Debug.Log(
                $"[CharacterUpgradeApi] 강화 성공\n" +
                $"UserId: {response.user_id}\n" +
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