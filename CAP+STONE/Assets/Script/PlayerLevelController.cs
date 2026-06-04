using System.Collections;
using UnityEngine;

public class PlayerLevelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStatusApi characterStatusApi;
    [SerializeField] private PlayerLevelUI playerLevelUI;

    private IEnumerator Start()
    {
        // 다른 오브젝트 Awake/Start 실행 대기
        yield return null;

        LoadLevelFromDb();
    }

    public void LoadLevelFromDb()
    {
        if (characterStatusApi == null)
        {
            characterStatusApi = FindFirstObjectByType<CharacterStatusApi>();
        }

        if (playerLevelUI == null)
        {
            playerLevelUI = FindFirstObjectByType<PlayerLevelUI>();
        }

        if (characterStatusApi == null)
        {
            Debug.LogError(
                "[PlayerLevelController] CharacterStatusApi를 찾지 못했습니다. " +
                "씬 안에 CharacterStatusApi가 붙은 오브젝트가 있어야 합니다."
            );
            return;
        }

        if (playerLevelUI == null)
        {
            Debug.LogError(
                "[PlayerLevelController] PlayerLevelUI를 찾지 못했습니다. " +
                "LevelPanel에 PlayerLevelUI가 붙어 있는지 확인하세요."
            );
            return;
        }

        Debug.Log("[PlayerLevelController] 유저 레벨 정보 로드 시작");

        characterStatusApi.LoadUserStatus((success, response) =>
        {
            if (!success || response == null)
            {
                Debug.LogError("[PlayerLevelController] 유저 상태 로드 실패");
                return;
            }

           playerLevelUI.SetStatus(
                response.level,
                response.exp,
                response.required_exp,
                response.gem
            );

            CurrencyUIManager.Instance?.SetGem(response.gem);

            Debug.Log(
                $"[PlayerLevelController] 유저 상태 적용 완료: " +
                $"Lv.{response.level}, EXP {response.exp}, GEM {response.gem}"
            );
        });
    }
}