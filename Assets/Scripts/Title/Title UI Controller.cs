using TMPro;
using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    [Header("Room UI")]
    [SerializeField] private GameObject _roomButtons;
    [SerializeField] private GameObject _joinRoomPanel;

    [Header("Join Room")]
    [SerializeField] private TMP_InputField _roomIDInput;

    private void Start()
    {
        ShowMain();
    }

    // Join Roomボタン
    public void OpenJoinRoom()
    {
        _roomButtons.SetActive(false);
        _joinRoomPanel.SetActive(true);

        if (_roomIDInput != null)
        {
            _roomIDInput.text = "";
            _roomIDInput.Select();
            _roomIDInput.ActivateInputField();
        }
    }

    // Cancelボタン
    public void CancelJoinRoom()
    {
        if (_roomIDInput != null)
        {
            _roomIDInput.text = "";
        }

        ShowMain();
    }

    private void ShowMain()
    {
        _roomButtons.SetActive(true);
        _joinRoomPanel.SetActive(false);
    }
}