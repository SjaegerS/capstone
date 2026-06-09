using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TestResetUserItemsButton : MonoBehaviour
{
    private const string FastApiBaseUrl = "https://perennial-steadier-budding.ngrok-free.dev";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveListener(OnClickResetButton);
        button.onClick.AddListener(OnClickResetButton);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickResetButton);
        }
    }

    private void OnClickResetButton()
    {
        Debug.Log("Test Reset 버튼 클릭됨");
        StartCoroutine(ResetUserItems());
    }

    private IEnumerator ResetUserItems()
    {
        string url = $"{FastApiBaseUrl}/test/user-items/reset";

        Debug.Log("user_item 초기화 요청 URL: " + url);

        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        Debug.Log("초기화 응답 코드: " + request.responseCode);
        Debug.Log("초기화 응답 내용: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("user_item 초기화 실패: " + request.error);
            yield break;
        }

        Debug.Log("user_item 초기화 성공");

        // Unity 로컬 인벤토리도 같이 초기화
        EquipmentInventory.ResetAll();

        // UI 새로고침
        EquipmentInventoryView.RefreshAll();

        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.RefreshPlayerStats();
        }
    }
}