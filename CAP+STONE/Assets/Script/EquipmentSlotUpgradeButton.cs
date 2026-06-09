using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EquipmentSlotUpgradeButton : MonoBehaviour
{
    private const string FastApiBaseUrl = "https://perennial-steadier-budding.ngrok-free.dev";

    private Button button;
    private Sprite equipmentSprite;
    private int userItemId;

    [System.Serializable]
    private class UserItemResponse
    {
        public int user_item_id;
        public int user_id;
        public int item_id;
        public int quantity;
        public int enhance_level;
        public bool is_equipped;
    }

    public void Configure(Sprite sprite, int userItemId)
    {
        equipmentSprite = sprite;
        this.userItemId = userItemId;

        EnsureButton();
        RefreshInteractable();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Upgrade);
        }
    }

    private void EnsureButton()
    {
        if (button != null)
        {
            return;
        }

        button = GetComponent<Button>();

        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        button.onClick.RemoveListener(Upgrade);
        button.onClick.AddListener(Upgrade);
    }

    private void Upgrade()
    {
        if (equipmentSprite == null)
        {
            Debug.LogWarning("강화 실패: equipmentSprite가 null입니다.");
            return;
        }

        if (userItemId <= 0)
        {
            Debug.LogWarning(
                "강화 실패: userItemId가 없습니다. " +
                "DB에서 받아온 user_item_id가 EquipmentInventoryRecord에 저장되어야 합니다."
            );
            return;
        }

        StartCoroutine(UpgradeOnServer());
    }

    private IEnumerator UpgradeOnServer()
    {
        string url = $"{FastApiBaseUrl}/user-items/{userItemId}/enhance/";

        Debug.Log("강화 요청 URL: " + url);

        UnityWebRequest request = new UnityWebRequest(url, "PATCH");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("강화 응답 코드: " + request.responseCode);
        Debug.Log("강화 응답 내용: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("강화 실패: " + request.error);
            yield break;
        }

        UserItemResponse response = null;

        try
        {
            response = JsonUtility.FromJson<UserItemResponse>(request.downloadHandler.text);
        }
        catch
        {
            Debug.LogError("강화 응답 JSON 파싱 실패");
            yield break;
        }

        if (response == null || response.user_item_id <= 0)
        {
            Debug.LogError("강화 응답 데이터가 올바르지 않습니다.");
            yield break;
        }

        EquipmentInventory.ApplyServerUserItem(
            equipmentSprite,
            response.user_item_id,
            response.item_id,
            response.quantity,
            response.enhance_level
        );

        EquipmentInventoryView.RefreshAll();

        BattleManager battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager != null)
        {
            battleManager.RefreshPlayerStats();
        }
    }

    private void RefreshInteractable()
    {
        if (button == null)
        {
            return;
        }

        EquipmentInventoryRecord record = EquipmentInventory.GetRecord(equipmentSprite);

        button.interactable =
            record != null &&
            record.IsOwned &&
            record.CanUpgrade &&
            record.UserItemId > 0;
    }
}