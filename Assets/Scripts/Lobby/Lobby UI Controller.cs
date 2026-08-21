using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyUIController : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private TMP_Text _roomIDText;

    private void Start()
    {
        StartCoroutine(UpdateRoomID());
    }

    /// <summary>
    /// Room情報が取得できるまで待ってRoomIDを表示する
    /// Host / Clientどちらでも同じRoomIDを表示する
    /// </summary>
    private IEnumerator UpdateRoomID()
    {
        _roomIDText.text = "ROOM ID : ------";

        while (OnlineManager.Instance == null)
        {
            yield return null;
        }

        while (string.IsNullOrEmpty(OnlineManager.Instance.RoomID))
        {
            yield return null;
        }

        _roomIDText.text =
            $"ROOM ID : {OnlineManager.Instance.RoomID}";
    }

    /// <summary>
    /// Lobbyから退出する
    /// </summary>
    public void ExitRoom()
    {
        if (OnlineManager.Instance == null)
        {
            Debug.LogError("OnlineManagerが存在しません。");
            return;
        }

        OnlineManager.Instance.ExitRoom();
    }
}