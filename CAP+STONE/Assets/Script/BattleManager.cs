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

    private UnitController currentPlayer;
    private UnitController currentEnemy;
    private int roundCount = 0;

    void Start()
    {
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
        // 플레이어 최초 1회 생성
        var pStats = new CharacterStats("P1", "Hero",    100, 15, 1.2f, 3f, 5);
        GameObject pObj = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
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

            // 플레이어: HP 풀 리셋 + 스폰 지점 복귀 (2라운드~)
            if (roundCount > 1)
                currentPlayer.Revive(playerSpawn.position);

            // 적: 매 라운드 새 인스턴스
            var eStats = new CharacterStats("E1", "Monster", 80, 10, 1.0f, 2f, 2);
            GameObject eObj = Instantiate(enemyPrefab, enemySpawn.position, Quaternion.identity);
            currentEnemy = eObj.GetComponent<UnitController>();
            if (currentEnemy == null)
            {
                Debug.LogError("[BattleManager] enemyPrefab에 UnitController 없음");
                yield break;
            }
            currentEnemy.Initialize(eStats, enemySpawn.position);

            // 라운드 결과 추적
            bool roundDone  = false;
            bool playerWon  = false;

            currentEnemy.OnDeath  = () => { roundDone = true; playerWon = true;  };
            currentPlayer.OnDeath = () => { roundDone = true; playerWon = false; };

            currentPlayer.SetTarget(currentEnemy);
            currentEnemy.SetTarget(currentPlayer);

            Vector3 meetPos = (playerSpawn.position + enemySpawn.position) / 2f;
            currentPlayer.MoveTo(meetPos);
            currentEnemy.MoveTo(meetPos);

            // 플레이어가 중앙 근처에 도달할 때까지 대기 (or 라운드 조기 종료)
            yield return new WaitUntil(() =>
                roundDone ||
                Vector3.Distance(currentPlayer.transform.position, meetPos) <= 0.5f);

            if (!roundDone)
            {
                currentPlayer.StartCombat();
                currentEnemy.StartCombat();
            }

            // 라운드 종료 대기
            yield return new WaitUntil(() => roundDone);

            if (playerWon)
            {
                Debug.Log($"[BattleManager] 라운드 {roundCount} 클리어");
                Destroy(currentEnemy.gameObject);
                yield return new WaitForSeconds(roundInterval);
                // 다음 라운드 자동 시작
            }
            else
            {
                Debug.Log("[BattleManager] 플레이어 사망 — 배틀 종료");
                yield break;
            }
        }
    }
}
