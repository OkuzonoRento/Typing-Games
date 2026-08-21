using TMPro;
using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterSelectController _characterSelectController;

    [Header("Room UI")]
    [SerializeField] private GameObject _roomButtons;
    [SerializeField] private GameObject _joinRoomPanel;

    [Header("Join Room")]
    [SerializeField] private TMP_InputField _roomIDInput;

    private void Start()
    {
        ShowRoomButtons();
    }

    /// <summary>
    /// Create Roomを押した時点のキャラクターを確定してRoomを作成する
    /// </summary>
    public void CreateRoom()
    {
        if (OnlineManager.Instance == null)
        {
            Debug.LogError("OnlineManagerが存在しません。");
            return;
        }

        if (_characterSelectController == null)
        {
            Debug.LogError("CharacterSelectControllerが設定されていません。");
            return;
        }

        int characterID =
            _characterSelectController.SelectedCharacterID;

        if (characterID < 0)
        {
            Debug.LogError("選択キャラクターが取得できません。");
            return;
        }

        OnlineManager.Instance.SetSelectedCharacter(characterID);

        OnlineManager.Instance.CreateRoom();
    }

    /// <summary>
    /// Join RoomのRoomID入力画面を開く
    /// </summary>
    public void OpenJoinRoomPanel()
    {
        _roomButtons.SetActive(false);
        _joinRoomPanel.SetActive(true);

        _roomIDInput.text = "";

        _roomIDInput.Select();
        _roomIDInput.ActivateInputField();
    }

    /// <summary>
    /// 入力したRoomIDでRoomへ参加する
    /// Joinを押した瞬間の選択キャラクターを確定する
    /// </summary>
    public void JoinRoom()
    {
        if (OnlineManager.Instance == null)
        {
            Debug.LogError("OnlineManagerが存在しません。");
            return;
        }

        if (_characterSelectController == null)
        {
            Debug.LogError("CharacterSelectControllerが設定されていません。");
            return;
        }

        string roomID = _roomIDInput.text;

        if (string.IsNullOrWhiteSpace(roomID))
        {
            Debug.LogWarning("Room IDを入力してください。");
            return;
        }

        int characterID =
            _characterSelectController.SelectedCharacterID;

        if (characterID < 0)
        {
            Debug.LogError("選択キャラクターが取得できません。");
            return;
        }

        OnlineManager.Instance.SetSelectedCharacter(characterID);

        OnlineManager.Instance.JoinRoom(roomID);
    }

    /// <summary>
    /// Join Roomをキャンセルして元の画面へ戻る
    /// </summary>
    public void CancelJoinRoom()
    {
        _roomIDInput.text = "";

        ShowRoomButtons();
    }

    /// <summary>
    /// 通常のRoomボタン画面を表示する
    /// </summary>
    private void ShowRoomButtons()
    {
        _roomButtons.SetActive(true);
        _joinRoomPanel.SetActive(false);
    }
}