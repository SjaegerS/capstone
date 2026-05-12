using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Hierarchy Objects")]
    public Transform playerSpawn; // 하이어라키의 'player spawn' 연결
    public Transform enemySpawn;  // 하이어라키의 'enemy spawn' 연결

    [Header("Prefabs")]
    public GameObject playerPrefab; // 프로젝트 창의 'player' 프리팹 연결
    public GameObject enemyPrefab;  // 프로젝트 창의 'enemy' 프리팹 연결

    private UnitController currentPlayer;
    private UnitController currentEnemy;

    void Start()
    {
        StartBattleRound();
    }

    void StartBattleRound()
    {
        // 1. 임의 수치 데이터 생성
        CharacterStats pStats = new CharacterStats("P1", "Hero", 100, 15, 1.2f, 3f, 5);
        CharacterStats eStats = new CharacterStats("E1", "Monster", 80, 10, 1f, 2f, 2);

        // 2. 스폰 및 초기화
        GameObject pObj = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        currentPlayer = pObj.GetComponent<UnitController>();
        currentPlayer.Initialize(pStats, playerSpawn.position);

        GameObject eObj = Instantiate(enemyPrefab, enemySpawn.position, Quaternion.identity);
        currentEnemy = eObj.GetComponent<UnitController>();
        currentEnemy.Initialize(eStats, enemySpawn.position);

        // 3. 중앙 지점 계산 및 이동
        Vector3 meetPos = (playerSpawn.position + enemySpawn.position) / 2f;

        currentPlayer.SetTarget(currentEnemy);
        currentEnemy.SetTarget(currentPlayer);

        currentPlayer.MoveTo(meetPos);
        currentEnemy.MoveTo(meetPos);

        StartCoroutine(WaitForCombat(meetPos));
    }

    IEnumerator WaitForCombat(Vector3 meetPos)
    {
        // 두 유닛이 중앙에 올 때까지 대기
        while (currentPlayer != null && Vector3.Distance(currentPlayer.transform.position, meetPos) > 0.5f)
        {
            yield return null;
        }

        if (currentPlayer != null && currentEnemy != null)
        {
            currentPlayer.StartCombat();
            currentEnemy.StartCombat();
        }
    }
}