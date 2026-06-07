using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;

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
    public int playerDefenseUpgradeLevel = 1;

    [Header("Stage Challenge UI")]
    [SerializeField] private Button stageChallengeButton;

    private bool isStageChallengeRunning = false;

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
    [SerializeField] private StageChallengeApi stageChallengeApi;


    [Tooltip("0이면 TitleManager에서 생성/저장한 USER_ID 사용.")]
    public int userId = 0;

    [Tooltip("DB 저장 실패해도 전투를 계속 진행할지 여부")]
    public bool continueBattleWhenDbSaveFails = true;

    [Header("UI")]
    public TextMeshProUGUI stageText;
    public PlayerLevelUI playerLevelUI;

    [Header("Enemy Hp slider")]
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private float enemyHpSmoothSpeed = 14f;

    private float enemyHpDisplayValue = 0f;
    private float enemyHpTargetValue = 0f;
    private float enemyHpMaxValue = 1f;

    private Coroutine battleLoopCoroutine;

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

    private void Update()
    {
        RefreshEnemyHpSliderVisual();
    }

    private void ResolveReferences()
    {
        if (battleRewardApi == null)
            battleRewardApi = FindFirstObjectByType<BattleRewardApi>();

        if (stageChallengeApi == null)
            stageChallengeApi = FindFirstObjectByType<StageChallengeApi>();

        if (playerLevelUI == null)
            playerLevelUI = FindFirstObjectByType<PlayerLevelUI>(FindObjectsInactive.Include);    
    }

    private void ResolveUserId()
    {
        if (battleRewardApi != null)
        {
            int apiUserId = battleRewardApi.GetUserId();

            if (apiUserId > 0)
            {
                userId = apiUserId;
                CurrentUser.UserId = apiUserId;

                Debug.Log($"[BattleManager] BattleRewardApi에서 USER_ID 로드: {userId}");
                return;
            }
        }

        if (CurrentUser.UserId > 0)
        {
            userId = CurrentUser.UserId;

            Debug.Log($"[BattleManager] CurrentUser에서 USER_ID 로드: {userId}");
            return;
        }

        int savedUserId = PlayerPrefs.GetInt(BattleRewardApi.USER_ID_KEY, -1);

        if (savedUserId > 0)
        {
            userId = savedUserId;
            CurrentUser.UserId = savedUserId;

            Debug.Log($"[BattleManager] PlayerPrefs USER_ID에서 로드: {userId}");
            return;
        }

        if (userId > 0)
        {
            CurrentUser.UserId = userId;

            PlayerPrefs.SetInt(BattleRewardApi.USER_ID_KEY, userId);
            PlayerPrefs.Save();

            Debug.LogWarning($"[BattleManager] Inspector userId 사용: {userId}");
            return;
        }

        Debug.LogError(
            "[BattleManager] 사용 가능한 USER_ID가 없습니다. " +
            "TitleScene에서 게임 시작 버튼을 통해 유저를 먼저 생성해야 합니다."
        );
    }

    private void SyncLegacyUpgradeLevel()
    {
        if (playerUpgradeLevel > 0)
        {
            if (playerHpUpgradeLevel <= 0)
                playerHpUpgradeLevel = playerUpgradeLevel;

            if (playerAttackUpgradeLevel <= 0)
                playerAttackUpgradeLevel = playerUpgradeLevel;

            if (playerDefenseUpgradeLevel <= 0)
                playerDefenseUpgradeLevel = playerUpgradeLevel;
        }

        playerHpUpgradeLevel = Mathf.Max(1, playerHpUpgradeLevel);
        playerAttackUpgradeLevel = Mathf.Max(1, playerAttackUpgradeLevel);
        playerDefenseUpgradeLevel = Mathf.Max(1, playerDefenseUpgradeLevel);

        playerUpgradeLevel = Mathf.Max(
            playerHpUpgradeLevel,
            playerAttackUpgradeLevel,
            playerDefenseUpgradeLevel
        );
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
        playerDefenseUpgradeLevel = playerUpgradeLevel;

        ApplyCurrentPlayerStats();
    }

    private void FinishStageChallenge()
    {
        isStageChallengeRunning = false;
        SetStageChallengeButtonInteractable(true);
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
            playerStats = CalculateFinalPlayerStatsFromDb(
                dbMaxHp,
                dbAttackPower,
                dbDefensePower
            );
        }
        else
        {
            playerStats = CalculateFinalPlayerStatsFromUpgradeLevel(
                playerHpUpgradeLevel,
                playerAttackUpgradeLevel,
                playerDefenseUpgradeLevel
            );
        }

        currentPlayer.ApplyStats(playerStats);
    }

    private CharacterStats CalculateFinalPlayerStatsFromDb(
        int baseMaxHp,
        int baseAttackPower,
        int baseDefensePower
    )
    {
        EquipmentStatSummary equipmentStats =
            EquipmentInventory.CalculateEquippedStatSummary();

        int finalMaxHp = GameBalance.CalculateFinalHPFromDbValue(
            baseMaxHp,
            equipmentStats.HpMainEffectSum,
            equipmentStats.HpSubEffectMultiplier
        );

        int finalAttackPower = GameBalance.CalculateFinalAttackPowerFromDbValue(
            baseAttackPower,
            equipmentStats.AttackMainEffectSum,
            equipmentStats.AttackSubEffectMultiplier
        );

        int finalDefensePower = GameBalance.CalculateFinalDefensePowerFromDbValue(
            baseDefensePower,
            equipmentStats.DefenseMainEffectSum,
            equipmentStats.DefenseSubEffectMultiplier
        );

        return CharacterStats.CreatePlayerFromDb(
            finalMaxHp,
            finalAttackPower,
            finalDefensePower
        );
    }

    private CharacterStats CalculateFinalPlayerStatsFromUpgradeLevel(
        int hpUpgradeLvl,
        int attackUpgradeLvl,
        int defenseUpgradeLvl
    )
    {
        EquipmentStatSummary equipmentStats =
            EquipmentInventory.CalculateEquippedStatSummary();

        int finalMaxHp = GameBalance.CalculateFinalHP(
            hpUpgradeLvl,
            equipmentStats.HpMainEffectSum,
            equipmentStats.HpSubEffectMultiplier
        );

        int finalAttackPower = GameBalance.CalculateFinalAttackPower(
            attackUpgradeLvl,
            equipmentStats.AttackMainEffectSum,
            equipmentStats.AttackSubEffectMultiplier
        );

        int finalDefensePower = GameBalance.CalculateFinalDefensePower(
            defenseUpgradeLvl,
            equipmentStats.DefenseMainEffectSum,
            equipmentStats.DefenseSubEffectMultiplier
        );

        return CharacterStats.CreatePlayerFromDb(
            finalMaxHp,
            finalAttackPower,
            finalDefensePower
        );
    }

    public void ApplyPlayerStatsFromDb(
        int maxHp,
        int attackPower,
        int defensePower,
        int hpUpgradeLvl,
        int attackUpgradeLvl,
        int defenseUpgradeLvl
    )
    {
        dbMaxHp = Mathf.Max(1, maxHp);
        dbAttackPower = Mathf.Max(0, attackPower);
        dbDefensePower = Mathf.Max(0, defensePower);

        playerHpUpgradeLevel = Mathf.Max(1, hpUpgradeLvl);
        playerAttackUpgradeLevel = Mathf.Max(1, attackUpgradeLvl);
        playerDefenseUpgradeLevel = Mathf.Max(1, defenseUpgradeLvl);
        playerUpgradeLevel = Mathf.Max(
                                playerHpUpgradeLevel,
                                playerAttackUpgradeLevel,
                                playerDefenseUpgradeLevel
                             );

        hasDbStatus = true;

        ApplyCurrentPlayerStats();

        Debug.Log(
            $"[BattleManager] 강화 스탯 적용 완료 - " +
            $"HP: {dbMaxHp}, ATK: {dbAttackPower}, DEF: {dbDefensePower}, " +
            $"HP_Lv: {playerHpUpgradeLevel}"+
            $"ATK_Lv: {playerAttackUpgradeLevel}"+
            $"DEF_Lv: {playerDefenseUpgradeLevel}"
            
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
                        playerDefenseUpgradeLevel = Mathf.Max(1, status.defense_upgrade_lvl);
                        playerUpgradeLevel = Mathf.Max(
                            playerHpUpgradeLevel, 
                            playerAttackUpgradeLevel,
                            playerDefenseUpgradeLevel
                            );

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
                    $"HP_Lv: {playerHpUpgradeLevel}"+
                    $"ATK_Lv: {playerAttackUpgradeLevel}"+
                    $"DEF_Lv: {playerDefenseUpgradeLevel}"
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

        battleLoopCoroutine = StartCoroutine(BattleLoop());
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

    public void OnClickStageChallengeButton()
    {
        if (isStageChallengeRunning)
        {
            Debug.Log("[BattleManager] 이미 스테이지 도전 중입니다.");
            return;
        }

        if (userId <= 0)
        {
            ResolveUserId();
        }

        if (userId <= 0)
        {
            Debug.LogError("[BattleManager] USER_ID가 없습니다. 스테이지 도전 불가");
            return;
        }

        if (stageChallengeApi == null)
        {
            stageChallengeApi = FindFirstObjectByType<StageChallengeApi>();
        }

        if (stageChallengeApi == null)
        {
            Debug.LogError("[BattleManager] StageChallengeApi가 연결되지 않았습니다.");
            return;
        }

        isStageChallengeRunning = true;
        SetStageChallengeButtonInteractable(false);

        StartCoroutine(stageChallengeApi.ChallengeStage(
            userId,
            response =>
            {
                if (response == null)
                {
                    Debug.LogError("[BattleManager] 스테이지 도전 응답이 null입니다.");

                    isStageChallengeRunning = false;
                    SetStageChallengeButtonInteractable(true);
                    return;
                }

                int challengeStage = Mathf.Max(1, response.current_stage);

                Debug.Log(
                    $"[BattleManager] 스테이지 도전 시작 | " +
                    $"user_id: {userId}, current_stage: {challengeStage}, " +
                    $"max_cleared_stage: {response.max_cleared_stage}"
                );

                RestartBattleAtStage(challengeStage);
            },
            error =>
            {
                Debug.LogError($"[BattleManager] 스테이지 도전 실패: {error}");

                isStageChallengeRunning = false;
                SetStageChallengeButtonInteractable(true);
            }
        ));
    }

    private void SetStageChallengeButtonInteractable(bool value)
    {
        if (stageChallengeButton != null)
            stageChallengeButton.interactable = value;
    }

    private void UpdateEnemyHpUI(bool instant = false)
    {
        if (currentEnemy == null)
        {
            SetEnemyHpUI(0f, 1f, instant);
            return;
        }

        SetEnemyHpUI(
            currentEnemy.CurrentHp,
            currentEnemy.MaxHp,
            instant
        );
    }

    private void HandleEnemyHpChanged(float currentHp, float maxHp)
    {
        SetEnemyHpUI(currentHp, maxHp, false);
    }

    private void SetEnemyHpUI(float currentHp, float maxHp, bool instant)
    {
        if (enemyHpSlider == null)
            return;

        enemyHpMaxValue = Mathf.Max(1f, maxHp);
        enemyHpTargetValue = Mathf.Clamp(currentHp, 0f, enemyHpMaxValue);

        enemyHpSlider.minValue = 0f;
        enemyHpSlider.maxValue = enemyHpMaxValue;

        if (instant)
        {
            enemyHpDisplayValue = enemyHpTargetValue;
            enemyHpSlider.value = enemyHpDisplayValue;
        }
    }

    private void RefreshEnemyHpSliderVisual()
    {
        if (enemyHpSlider == null)
            return;

        enemyHpSlider.minValue = 0f;
        enemyHpSlider.maxValue = Mathf.Max(1f, enemyHpMaxValue);

        enemyHpDisplayValue = Mathf.Lerp(
            enemyHpDisplayValue,
            enemyHpTargetValue,
            Time.deltaTime * enemyHpSmoothSpeed
        );

        if (Mathf.Abs(enemyHpDisplayValue - enemyHpTargetValue) < 0.25f)
        {
            enemyHpDisplayValue = enemyHpTargetValue;
        }

        enemyHpSlider.value = Mathf.Clamp(
            enemyHpDisplayValue,
            0f,
            enemyHpSlider.maxValue
        );
    }

    private void RestartBattleAtStage(int stage)
    {
        stage = Mathf.Max(1, stage);


        if (battleLoopCoroutine != null)
        {
            StopCoroutine(battleLoopCoroutine);
            battleLoopCoroutine = null;
        }

        if (currentPlayer != null)
        {
            currentPlayer.SetTarget(null);
            Destroy(currentPlayer.gameObject);
            currentPlayer = null;
        }

        if (currentEnemy != null)
        {
            currentEnemy.SetTarget(null);
            Destroy(currentEnemy.gameObject);
            currentEnemy = null;
        }

        UpdateEnemyHpUI(true);

        currentStage = stage;
        startingStage = stage;
        roundCount = 0;

        UpdateStageText(currentStage);

        battleLoopCoroutine = StartCoroutine(BattleLoop());

        Debug.Log($"[BattleManager] 전투 재시작 | Stage {stage}");
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

            // 몬스터 머리 위 HP바는 사용하지 않음
            currentEnemy.SetWorldHealthBarEnabled(false);

            currentEnemy.Initialize(enemyStats, enemySpawn.position);

            // 스테이지 아래 EnemyHp Slider 갱신
            UpdateEnemyHpUI(true);

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

            float moveWaitTime = 0f;
            float maxMoveWaitTime = 1.2f;

            yield return new WaitUntil(() =>
            {
                moveWaitTime += Time.deltaTime;

                bool playerArrived =
                    currentPlayer != null &&
                    Vector3.Distance(currentPlayer.transform.position, playerStopPos) <= 0.5f;

                bool enemyArrived =
                    currentEnemy != null &&
                    Vector3.Distance(currentEnemy.transform.position, enemyStopPos) <= 0.5f;

                return roundDone || (playerArrived && enemyArrived) || moveWaitTime >= maxMoveWaitTime;
            });

            if (!roundDone)
            {
                currentPlayer.StartCombat();
                currentEnemy.StartCombat();
            }

            while (!roundDone)
            {
                UpdateEnemyHpUI(false);
                yield return null;
            }

            // 죽는 순간의 HP 0을 확실하게 반영
            UpdateEnemyHpUI(true);
            yield return new WaitForSeconds(0.08f);

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
                    yield return StartCoroutine(battleRewardApi.SaveBattleReward(
                        userId,
                        currentStage,
                        rewardGold,
                        rewardExp,
                        1,
                        true,
                        response =>
                        {
                            if (response == null)
                            {
                                Debug.LogError("[BattleManager] BattleRewardResponse가 null입니다.");
                                return;
                            }

                            dbSaveSuccess = true;

                            nextStage = Mathf.Max(1, response.current_stage);

                            CurrencyUIManager.Instance?.SetGold(response.gold);
                            CurrencyUIManager.Instance?.SetGem(response.gem);

                            if (playerLevelUI != null)
                            {
                                playerLevelUI.SetStatus(
                                    response.level,
                                    response.exp,
                                    response.required_exp
                                );
                            }

                            Debug.Log(
                                $"[BattleManager] 보상 UI 즉시 갱신 완료 | " +
                                $"Lv.{response.level}, EXP {response.exp}/{response.required_exp}, " +
                                $"Gold {response.gold}, Gem {response.gem}, LevelUp {response.level_up_count}"
                            );
                        },
                        error =>
                        {
                            Debug.LogError($"[BattleManager] 보상 저장 실패: {error}");
                        }
                    ));
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
                    CurrencyUIManager.Instance?.AddGold(rewardGold);
                }

                QuestProgressReporter questReporter =
                    FindFirstObjectByType<QuestProgressReporter>();

                if (questReporter != null)
                {
                    questReporter.ReportProgress(QuestEvent.BattleWin, 1);
                }

                coinSpawner?.SpawnCoins(enemyDeathPos);

                currentPlayer.SetTarget(null);

                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
                UpdateEnemyHpUI(true);

                currentStage = nextStage;
                UpdateStageText(currentStage);
                FinishStageChallenge();


                yield return new WaitForSeconds(roundInterval);
            }
            else
            {
                Debug.Log($"[BattleManager] 플레이어 사망 — 스테이지 {currentStage} 실패");

                int failedStage = currentStage;

                if (battleRewardApi != null && userId > 0)
                {
                    yield return StartCoroutine(battleRewardApi.SaveBattleReward(
                        userId,
                        failedStage,
                        0,
                        0,
                        0,
                        false,
                        response =>
                        {
                            if (response == null)
                            {
                                Debug.LogError("[BattleManager] 실패 처리 응답이 null입니다.");
                                return;
                            }

                            currentStage = Mathf.Max(1, response.current_stage);
                            startingStage = currentStage;

                            UpdateStageText(currentStage);

                            Debug.Log(
                                $"[BattleManager] 스테이지 실패 처리 완료 | " +
                                $"current_stage: {response.current_stage}, " +
                                $"max_cleared_stage: {response.max_cleared_stage}"
                            );
                        },
                        error =>
                        {
                            Debug.LogError($"[BattleManager] 스테이지 실패 저장 실패: {error}");
                        }
                    ));
                }

                currentPlayer.SetTarget(null);

                if (currentEnemy != null)
                {
                    currentEnemy.SetTarget(null);
                    Destroy(currentEnemy.gameObject);
                    currentEnemy = null;
                }
                UpdateEnemyHpUI(true);
                FinishStageChallenge();

                yield return new WaitForSeconds(roundInterval);
            }
        }
    }
}