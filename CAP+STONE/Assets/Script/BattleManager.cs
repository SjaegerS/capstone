using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Prefabs (Project 창 에셋 — 씬 오브젝트 금지)")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    [Header("Spawn Points (Hierarchy 월드 오브젝트)")]
    public Transform playerSpawn;
    public Transform enemySpawn;

    [Header("Balance")]
    [Tooltip("플레이어 스탯 강화 횟수 (0 = 미강화)")]
    public int playerUpgradeLevel = 0;
    public int playerHpUpgradeLevel = 0;
    public int playerAttackUpgradeLevel = 0;
    [Tooltip("시작 스테이지 번호")]
    public int startingStage = 1;

    [Header("Settings")]
    public float roundInterval = 1.5f;
    [Tooltip("두 유닛이 멈추는 간격 (월드 단위). 캐릭터가 겹치면 키우세요.")]
    public float stopGap = 1.5f;

    [Header("Coin System")]
    public CoinSpawner coinSpawner;

    private UnitController currentPlayer;
    private UnitController currentEnemy;
    private int roundCount = 0;
    private int currentStage;

    void Start()
    {
        SyncLegacyUpgradeLevel();

        UnitController[] sceneUnits = FindObjectsByType<UnitController>( 
            FindObjectsInactive.Exclude, 
            FindObjectsSortMode.None
            );
        if (sceneUnits.Length > 0)
            Debug.LogWarning($"[BattleManager] 씬에 UnitController 인스턴스 {sceneUnits.Length}개 감지. " +
                             "Hierarchy에서 player, Enemy, player_healthbar, enemy_healthbar를 삭제하고 씬을 저장하세요.");

        if (!ValidateReferences()) return;
        StartCoroutine(BattleLoop());
    }

    private void SyncLegacyUpgradeLevel()
    {
        if (playerUpgradeLevel > 0 && playerHpUpgradeLevel == 0 && playerAttackUpgradeLevel == 0)
        {
            playerHpUpgradeLevel = playerUpgradeLevel;
            playerAttackUpgradeLevel = playerUpgradeLevel;
        }
    }

    bool ValidateReferences()
    {
        bool ok = true;
        if (playerPrefab == null) { Debug.LogError("[BattleManager] playerPrefab 미연결"); ok = false; }
        if (enemyPrefab  == null) { Debug.LogError("[BattleManager] enemyPrefab 미연결");  ok = false; }
        if (playerSpawn  == null) { Debug.LogError("[BattleManager] playerSpawn 미연결");   ok = false; }
        if (enemySpawn   == null) { Debug.LogError("[BattleManager] enemySpawn 미연결");    ok = false; }
        return ok;
    }

    public void ApplyPlayerUpgradeLevel(int upgradeLevel)
    {
        playerUpgradeLevel = Mathf.Max(0, upgradeLevel);
        playerHpUpgradeLevel = playerUpgradeLevel;
        playerAttackUpgradeLevel = playerUpgradeLevel;

        ApplyCurrentPlayerStats();
    }

    public void ApplyPlayerUpgradeLevel(bool isHealthUpgrade, int upgradeLevel)
    {
        int safeUpgradeLevel = Mathf.Max(0, upgradeLevel);

        if (isHealthUpgrade)
        {
            playerHpUpgradeLevel = safeUpgradeLevel;
        }
        else
        {
            playerAttackUpgradeLevel = safeUpgradeLevel;
        }

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
        if (currentPlayer != null)
        {
            currentPlayer.ApplyStats(CharacterStats.CreatePlayer(playerHpUpgradeLevel, playerAttackUpgradeLevel));
        }
    }

    IEnumerator BattleLoop()
    {
        currentStage = Mathf.Max(1, startingStage);

        var pStats = CharacterStats.CreatePlayer(playerHpUpgradeLevel, playerAttackUpgradeLevel);
        GameObject pObj = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        pObj.SetActive(true);
        currentPlayer = pObj.GetComponent<UnitController>();
        if (currentPlayer == null)
        {
            Debug.LogError("[BattleManager] playerPrefab에 UnitController 없음");
            yield break;
        }
        currentPlayer.Initialize(pStats, playerSpawn.position);

        while (true)
        {
            roundCount++;

            if (roundCount > 1)
                currentPlayer.Revive(playerSpawn.position);

            var eStats = CharacterStats.CreateEnemy(currentStage);
            Debug.Log($"[BattleManager] 스테이지 {currentStage} 몬스터 — HP {(int)eStats.MaxHP} / ATK {(int)eStats.AttackDamage} / DEF {(int)eStats.Defense}");

            GameObject eObj = Instantiate(enemyPrefab, enemySpawn.position, Quaternion.identity);
            eObj.SetActive(true);
            currentEnemy = eObj.GetComponent<UnitController>();
            if (currentEnemy == null)
            {
                Debug.LogError("[BattleManager] enemyPrefab에 UnitController 없음");
                yield break;
            }
            currentEnemy.Initialize(eStats, enemySpawn.position);

            bool roundDone = false;
            bool playerWon = false;
            Vector3 enemyDeathPos = Vector3.zero;

            currentEnemy.OnDeath  = () => { enemyDeathPos = currentEnemy.transform.position; roundDone = true; playerWon = true;  };
            currentPlayer.OnDeath = () => { roundDone = true; playerWon = false; };

            currentPlayer.SetTarget(currentEnemy);
            currentEnemy.SetTarget(currentPlayer);

            Vector3 center = (playerSpawn.position + enemySpawn.position) / 2f;
            Vector3 dir = (enemySpawn.position - playerSpawn.position).normalized;
            if (dir == Vector3.zero) dir = Vector3.right;

            Vector3 playerStopPos = center - dir * (stopGap / 2f);
            Vector3 enemyStopPos  = center + dir * (stopGap / 2f);

            currentPlayer.MoveTo(playerStopPos);
            currentEnemy.MoveTo(enemyStopPos);

            yield return new WaitUntil(() =>
                roundDone ||
                Vector3.Distance(currentPlayer.transform.position, playerStopPos) <= 0.5f);

            if (!roundDone)
            {
                currentPlayer.StartCombat();
                currentEnemy.StartCombat();
            }

            yield return new WaitUntil(() => roundDone);

            if (playerWon)
            {
                int rewardGold = GameBalance.RewardGold(currentStage);
                int rewardExp  = GameBalance.RewardExp(currentStage);
                Debug.Log($"[BattleManager] 스테이지 {currentStage} 클리어 — 골드 +{rewardGold} / 경험치 +{rewardExp}");

                GoldManager.Instance?.AddGold(rewardGold);
                coinSpawner?.SpawnCoins(enemyDeathPos);

                currentPlayer.SetTarget(null);
                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
                currentStage++;
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
