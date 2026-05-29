using UnityEngine;
using System.Collections;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("Prefabs (Project 창 에셋 — 씬 오브젝트 금지)")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    [Header("Spawn Points (Hierarchy 월드 오브젝트)")]
    public Transform playerSpawn;
    public Transform enemySpawn;

    [Header("Balance")]
    [Tooltip("기존 호환용. 가능하면 hp/attack 개별 레벨 사용")]
    public int playerUpgradeLevel = 1;

    public int playerHpUpgradeLevel = 1;
    public int playerAttackUpgradeLevel = 1;

    [Tooltip("시작 스테이지 번호")]
    public int startingStage = 1;

    [Header("Settings")]
    public float roundInterval = 1.5f;

    [Tooltip("두 유닛이 멈추는 간격")]
    public float stopGap = 1.5f;

    [Header("Coin System")]
    public CoinSpawner coinSpawner;

    [Header("API")]
    public BattleRewardApi battleRewardApi;
    public UserCreateApi userCreateApi;

    [Tooltip("테스트용 유저 ID. UserCreateApi 또는 PlayerPrefs 값이 있으면 자동으로 덮어씀.")]
    public int userId = 8;

    [Tooltip("DB 저장 실패해도 전투를 계속 진행할지 여부")]
    public bool continueBattleWhenDbSaveFails = true;

    [Header("UI")]
    public TextMeshProUGUI stageText;

    private UnitController currentPlayer;
    private UnitController currentEnemy;

    private int roundCount = 0;
    private int currentStage;

    private int dbMaxHp = 100;
    private int dbAttackPower = 20;
    private int dbDefensePower = 20;

    private bool hasDbStatus = false;

    void Start()
    {
        ResolveReferences();
        ResolveUserId();
        SyncLegacyUpgradeLevel();

        if (!ValidateReferences())
            return;

        StartCoroutine(InitializeBattleFromDb());
    }

    private void ResolveReferences()
    {
        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();

        if (userCreateApi == null)
            userCreateApi = FindFirstObjectByType<UserCreateApi>();
    }

    private void ResolveUserId()
    {
        if (userCreateApi != null && userCreateApi.CurrentUserId > 0)
        {
            userId = (int)userCreateApi.CurrentUserId;
            Debug.Log($"[BattleManager] UserCreateApi에서 user_id 로드: {userId}");
            return;
        }

        if (PlayerPrefs.HasKey("TEST_USER_ID") &&
            int.TryParse(PlayerPrefs.GetString("TEST_USER_ID"), out int testUserId) &&
            testUserId > 0)
        {
            userId = testUserId;
            Debug.Log($"[BattleManager] TEST_USER_ID에서 user_id 로드: {userId}");
            return;
        }

        int savedUserId = PlayerPrefs.GetInt("user_id", 8);

        if (savedUserId > 0)
        {
            userId = savedUserId;
            Debug.Log($"[BattleManager] PlayerPrefs user_id에서 로드: {userId}");
            return;
        }

        Debug.LogWarning($"[BattleManager] 저장된 user_id가 없어 Inspector userId 사용: {userId}");
    }

    private void SyncLegacyUpgradeLevel()
    {
        if (playerUpgradeLevel > 0)
        {
            if (playerHpUpgradeLevel <= 0)
                playerHpUpgradeLevel = playerUpgradeLevel;

            if (playerAttackUpgradeLevel <= 0)
                playerAttackUpgradeLevel = playerUpgradeLevel;
        }

        playerHpUpgradeLevel = Mathf.Max(1, playerHpUpgradeLevel);
        playerAttackUpgradeLevel = Mathf.Max(1, playerAttackUpgradeLevel);
        playerUpgradeLevel = Mathf.Max(playerHpUpgradeLevel, playerAttackUpgradeLevel);
    }

    bool ValidateReferences()
    {
        bool ok = true;

        if (playerPrefab == null)
        {
            Debug.LogError("[BattleManager] playerPrefab 미연결");
            ok = false;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[BattleManager] enemyPrefab 미연결");
            ok = false;
        }

        if (playerSpawn == null)
        {
            Debug.LogError("[BattleManager] playerSpawn 미연결");
            ok = false;
        }

        if (enemySpawn == null)
        {
            Debug.LogError("[BattleManager] enemySpawn 미연결");
            ok = false;
        }

        if (battleRewardApi == null)
        {
            Debug.LogWarning("[BattleManager] BattleRewardApi 미연결. DB 로드/저장 생략 가능");
        }

        return ok;
    }

    public void ApplyPlayerUpgradeLevel(int upgradeLvl)
    {
        playerUpgradeLevel = Mathf.Max(1, upgradeLvl);
        playerHpUpgradeLevel = playerUpgradeLevel;
        playerAttackUpgradeLevel = playerUpgradeLevel;

        ApplyCurrentPlayerStats();
    }

    public void ApplyPlayerUpgradeLevel(bool isHealthUpgrade, int upgradeLvl)
    {
        int safeUpgradeLvl = Mathf.Max(1, upgradeLvl);

        if (isHealthUpgrade)
            playerHpUpgradeLevel = safeUpgradeLvl;
        else
            playerAttackUpgradeLevel = safeUpgradeLvl;

        playerUpgradeLevel = Mathf.Max(playerHpUpgradeLevel, playerAttackUpgradeLevel);

        ApplyCurrentPlayerStats();
    }

    public int GetPlayerUpgradeLevel(bool isHealthUpgrade)
    {
        return isHealthUpgrade ? playerHpUpgradeLevel : playerAttackUpgradeLevel;
    }



    public void RefreshPlayerStats()
    {
        ApplyCurrentPlayerStats();
    }

    private void ApplyCurrentPlayerStats()
    {
        if (currentPlayer == null)
            return;

        CharacterStats playerStats;

        if (hasDbStatus)
        {
            playerStats = CharacterStats.CreatePlayerFromDb(
                dbMaxHp,
                dbAttackPower,
                dbDefensePower
            );
        }
        else
        {
            playerStats = CharacterStats.CreatePlayer(
                playerHpUpgradeLevel,
                playerAttackUpgradeLevel
            );
        }

        currentPlayer.ApplyStats(playerStats);
    }

    public void ApplyPlayerStatsFromDb(
        int maxHp,
        int attackPower,
        int defensePower,
        int hpUpgradeLvl,
        int attackUpgradeLvl
    )
    {
        dbMaxHp = Mathf.Max(1, maxHp);
        dbAttackPower = Mathf.Max(0, attackPower);
        dbDefensePower = Mathf.Max(0, defensePower);

        playerHpUpgradeLevel = Mathf.Max(1, hpUpgradeLvl);
        playerAttackUpgradeLevel = Mathf.Max(1, attackUpgradeLvl);
        playerUpgradeLevel = Mathf.Max(playerHpUpgradeLevel, playerAttackUpgradeLevel);

        hasDbStatus = true;

        ApplyCurrentPlayerStats();

        Debug.Log(
            $"[BattleManager] 강화 스탯 적용 완료 - " +
            $"HP: {dbMaxHp}, ATK: {dbAttackPower}, DEF: {dbDefensePower}, " +
            $"HP_Lv: {playerHpUpgradeLevel}, ATK_Lv: {playerAttackUpgradeLevel}"
        );
    }

    private IEnumerator InitializeBattleFromDb()
    {
        startingStage = Mathf.Max(1, startingStage);

        if (battleRewardApi != null && userId > 0)
        {
            bool loaded = false;
            int dbStage = startingStage;

            yield return StartCoroutine(
                battleRewardApi.GetUserStatus(
                    userId,
                    status =>
                    {
                        if (status == null)
                            return;

                        dbStage = Mathf.Max(1, status.current_stage);

                        dbMaxHp = Mathf.Max(1, status.max_hp);
                        dbAttackPower = Mathf.Max(0, status.attack_power);
                        dbDefensePower = Mathf.Max(0, status.defense_power);

                        playerHpUpgradeLevel = Mathf.Max(1, status.hp_upgrade_lvl);
                        playerAttackUpgradeLevel = Mathf.Max(1, status.attack_upgrade_lvl);
                        playerUpgradeLevel = Mathf.Max(playerHpUpgradeLevel, playerAttackUpgradeLevel);

                        hasDbStatus = true;
                        loaded = true;
                    },
                    error =>
                    {
                        Debug.LogWarning(
                            $"[BattleManager] DB 스테이지 로드 실패\n" +
                            $"user_id: {userId}\n" +
                            $"error: {error}"
                        );
                    }
                )
            );

            if (loaded)
            {
                startingStage = dbStage;
                Debug.Log(
                    $"[BattleManager] DB 상태 로드 완료\n" +
                    $"user_id: {userId}, stage: {startingStage}, " +
                    $"HP: {dbMaxHp}, ATK: {dbAttackPower}, DEF: {dbDefensePower}, " +
                    $"HP_Lv: {playerHpUpgradeLevel}, ATK_Lv: {playerAttackUpgradeLevel}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[BattleManager] DB 상태 로드 실패. startingStage={startingStage} 사용"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[BattleManager] BattleRewardApi가 없거나 userId가 0 이하입니다. " +
                $"startingStage={startingStage} 사용"
            );
        }

        StartCoroutine(BattleLoop());
    }

    private void UpdateStageText()
    {
        UpdateStageText(currentStage);
    }

    private void UpdateStageText(int stage)
    {
        if (stageText == null)
            stageText = FindStageText();

        if (stageText != null)
            stageText.text = $"Stage {stage}";
    }

    private TextMeshProUGUI FindStageText()
    {
        TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name.Contains("Stage") || text.text.ToUpperInvariant().Contains("STAGE"))
                return text;
        }

        return null;
    }

    private IEnumerator BattleLoop()
    {
        currentStage = Mathf.Max(1, startingStage);
        UpdateStageText(currentStage);

        CharacterStats playerStats;

        if (hasDbStatus)
        {
            playerStats = CharacterStats.CreatePlayerFromDb(
                dbMaxHp,
                dbAttackPower,
                dbDefensePower
            );
        }
        else
        {
            playerStats = CharacterStats.CreatePlayer(
                playerHpUpgradeLevel,
                playerAttackUpgradeLevel
            );
        }

        GameObject pObj = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        pObj.SetActive(true);

        currentPlayer = pObj.GetComponent<UnitController>();

        if (currentPlayer == null)
        {
            Debug.LogError("[BattleManager] playerPrefab에 UnitController 없음");
            yield break;
        }

        currentPlayer.Initialize(playerStats, playerSpawn.position);

        while (true)
        {
            roundCount++;

            if (roundCount > 1)
                currentPlayer.Revive(playerSpawn.position);

            CharacterStats enemyStats = CharacterStats.CreateEnemy(currentStage);

            Debug.Log(
                $"[BattleManager] 스테이지 {currentStage} 몬스터 — " +
                $"HP {(int)enemyStats.MaxHP} / ATK {(int)enemyStats.AttackDamage} / DEF {(int)enemyStats.Defense}"
            );

            GameObject eObj = Instantiate(enemyPrefab, enemySpawn.position, Quaternion.identity);
            eObj.SetActive(true);

            currentEnemy = eObj.GetComponent<UnitController>();

            if (currentEnemy == null)
            {
                Debug.LogError("[BattleManager] enemyPrefab에 UnitController 없음");
                yield break;
            }

            currentEnemy.Initialize(enemyStats, enemySpawn.position);

            bool roundDone = false;
            bool playerWon = false;
            Vector3 enemyDeathPos = Vector3.zero;

            currentEnemy.OnDeath = () =>
            {
                enemyDeathPos = currentEnemy.transform.position;
                roundDone = true;
                playerWon = true;
            };

            currentPlayer.OnDeath = () =>
            {
                roundDone = true;
                playerWon = false;
            };

            currentPlayer.SetTarget(currentEnemy);
            currentEnemy.SetTarget(currentPlayer);

            Vector3 center = (playerSpawn.position + enemySpawn.position) / 2f;
            Vector3 dir = (enemySpawn.position - playerSpawn.position).normalized;

            if (dir == Vector3.zero)
                dir = Vector3.right;

            Vector3 playerStopPos = center - dir * (stopGap / 2f);
            Vector3 enemyStopPos = center + dir * (stopGap / 2f);

            currentPlayer.MoveTo(playerStopPos);
            currentEnemy.MoveTo(enemyStopPos);

            yield return new WaitUntil(() =>
                roundDone ||
                Vector3.Distance(currentPlayer.transform.position, playerStopPos) <= 0.5f
            );

            if (!roundDone)
            {
                currentPlayer.StartCombat();
                currentEnemy.StartCombat();
            }

            yield return new WaitUntil(() => roundDone);

            if (playerWon)
            {
                int clearedStage = currentStage;
                int nextStage = currentStage + 1;

                int rewardGold = GameBalance.RewardGold(clearedStage);
                int rewardExp = GameBalance.RewardExp(clearedStage);

                Debug.Log(
                    $"[BattleManager] 스테이지 {clearedStage} 클리어 — " +
                    $"골드 +{rewardGold} / 경험치 +{rewardExp}"
                );

                bool dbSaveSuccess = false;

                if (battleRewardApi != null && userId > 0)
                {
                    yield return StartCoroutine(
                        battleRewardApi.SaveBattleReward(
                            userId,
                            clearedStage,
                            rewardGold,
                            rewardExp,
                            response =>
                            {
                                dbSaveSuccess = true;

                                if (response != null)
                                {
                                    nextStage = Mathf.Max(nextStage, response.current_stage);

                                    GoldManager.Instance?.SetGold(response.gold);
                                    CurrencyUIManager.Instance?.SetGold(response.gold);
                                }
                            },
                            error =>
                            {
                                dbSaveSuccess = false;
                                Debug.LogError(
                                    $"[BattleManager] 전투 보상 DB 저장 실패\n" +
                                    $"user_id: {userId}\n" +
                                    $"error: {error}"
                                );
                            }
                        )
                    );
                }
                else
                {
                    Debug.LogWarning("[BattleManager] BattleRewardApi가 없거나 userId가 0 이하입니다. DB 저장 생략");
                }

                if (!dbSaveSuccess && !continueBattleWhenDbSaveFails)
                {
                    Debug.LogError("[BattleManager] DB 저장 실패로 스테이지 진행 중단");
                    yield return new WaitForSeconds(roundInterval);
                    continue;
                }

                if (!dbSaveSuccess)
                {
                    GoldManager.Instance?.AddGold(rewardGold);
                }

                coinSpawner?.SpawnCoins(enemyDeathPos);

                currentPlayer.SetTarget(null);

                Destroy(currentEnemy.gameObject);
                currentEnemy = null;

                currentStage = nextStage;
                UpdateStageText(currentStage);

                yield return new WaitForSeconds(roundInterval);
            }
            else
            {
                Debug.Log($"[BattleManager] 플레이어 사망 — 스테이지 {currentStage} 재시도");

                currentPlayer.SetTarget(null);

                if (currentEnemy != null)
                {
                    currentEnemy.SetTarget(null);
                    Destroy(currentEnemy.gameObject);
                    currentEnemy = null;
                }

                yield return new WaitForSeconds(roundInterval);
            }
        }
    }
}