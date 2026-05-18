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

    [Header("Settings")]
    public float roundInterval = 1.5f;
    [Tooltip("두 유닛이 멈추는 간격 (월드 단위). 캐릭터가 겹치면 키우세요.")]
    public float stopGap = 1.5f;

    private UnitController currentPlayer;
    private UnitController currentEnemy;
    private int roundCount = 0;

    void Start()
    {
        UnitController[] sceneUnits = FindObjectsOfType<UnitController>();
        if (sceneUnits.Length > 0)
            Debug.LogWarning($"[BattleManager] 씬에 UnitController 인스턴스 {sceneUnits.Length}개 감지. " +
                             "Hierarchy에서 player, Enemy, player_healthbar, enemy_healthbar를 삭제하고 씬을 저장하세요.");

        if (!ValidateReferences()) return;
        StartCoroutine(BattleLoop());
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

    IEnumerator BattleLoop()
    {
        var pStats = new CharacterStats("P1", "Hero", 100, 15, 1.2f, 3f, 5);
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
            Debug.Log($"[BattleManager] 라운드 {roundCount} 시작");

            if (roundCount > 1)
                currentPlayer.Revive(playerSpawn.position);

            var eStats = new CharacterStats("E1", "Monster", 80, 10, 1.0f, 2f, 2);
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

            currentEnemy.OnDeath  = () => { roundDone = true; playerWon = true;  };
            currentPlayer.OnDeath = () => { roundDone = true; playerWon = false; };

            currentPlayer.SetTarget(currentEnemy);
            currentEnemy.SetTarget(currentPlayer);

            // 중앙에서 ±(stopGap/2) 떨어진 각자의 정지 지점 계산
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
                Debug.Log($"[BattleManager] 라운드 {roundCount} 클리어");
                currentPlayer.SetTarget(null);
                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
                yield return new WaitForSeconds(roundInterval);
            }
            else
            {
                Debug.Log("[BattleManager] 플레이어 사망 — 배틀 종료");
                yield break;
            }
        }
    }
}
