using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Character Database")]
    [SerializeField] private CharacterDatabase _characterDatabase;

    [Header("My Character")]
    [SerializeField] private Transform _myCharacterModelPoint;
    [SerializeField] private TMP_Text _myCharacterNameText;
    [SerializeField] private TMP_Text _myUltNameText;

    [Header("Opponent Character")]
    [SerializeField] private Transform _opponentCharacterModelPoint;
    [SerializeField] private TMP_Text _opponentCharacterNameText;
    [SerializeField] private TMP_Text _opponentUltNameText;

    [Header("Ready")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private TMP_Text _readyButtonText;

    private GameObject _myCharacterModel;
    private GameObject _opponentCharacterModel;

    private int _currentMyCharacterID = -1;
    private int _currentOpponentCharacterID = -1;

    private NetworkPlayer _myPlayer;
    private NetworkPlayer _opponentPlayer;

    private bool _wasTwoPlayers;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ClearPlayerInformation();

        RefreshPlayers();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// NetworkPlayer一覧を確認して
    /// 自分・相手・Ready状態を更新する
    /// </summary>
    public void RefreshPlayers()
    {
        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        _myPlayer = null;
        _opponentPlayer = null;

        foreach (NetworkPlayer player in players)
        {
            if (!player.IsSpawned)
            {
                continue;
            }

            if (player.IsOwner)
            {
                _myPlayer = player;
            }
            else
            {
                _opponentPlayer = player;
            }
        }

        // 自分のキャラクター表示
        if (_myPlayer != null &&
            _myPlayer.CharacterID >= 0)
        {
            UpdateMyCharacter(
                _myPlayer.CharacterID
            );
        }

        // 相手のキャラクター表示
        if (_opponentPlayer != null &&
            _opponentPlayer.CharacterID >= 0)
        {
            UpdateOpponentCharacter(
                _opponentPlayer.CharacterID
            );
        }
        else
        {
            ClearOpponentCharacter();
        }

        bool hasTwoPlayers =
            _myPlayer != null &&
            _opponentPlayer != null;

        UpdateReadyUI(hasTwoPlayers);

        /*
         * 2人状態から1人状態になった場合、
         * Host側で残っているPlayerのReadyを解除する
         */
        if (_wasTwoPlayers &&
            !hasTwoPlayers &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer)
        {
            ResetAllReady();
        }

        _wasTwoPlayers = hasTwoPlayers;

        /*
         * ゲーム開始判定はHostだけが行う
         */
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer)
        {
            CheckGameStart();
        }
    }

    /// <summary>
    /// Readyボタンを押したとき
    /// </summary>
    public void ToggleReady()
    {
        if (_myPlayer == null)
        {
            return;
        }

        if (_opponentPlayer == null)
        {
            return;
        }

        _myPlayer.SetReady(
            !_myPlayer.IsReady
        );
    }

    /// <summary>
    /// Readyボタンの表示状態を更新
    /// </summary>
    private void UpdateReadyUI(bool hasTwoPlayers)
    {
        if (_readyButton == null)
        {
            return;
        }

        _readyButton.interactable =
            hasTwoPlayers;

        if (_readyButtonText == null)
        {
            return;
        }

        if (!hasTwoPlayers)
        {
            _readyButtonText.text = "READY";
            return;
        }

        if (_myPlayer != null &&
            _myPlayer.IsReady)
        {
            _readyButtonText.text = "CANCEL READY";
        }
        else
        {
            _readyButtonText.text = "READY";
        }
    }

    /// <summary>
    /// Hostが双方Readyか確認する
    /// </summary>
    private void CheckGameStart()
    {
        if (_myPlayer == null ||
            _opponentPlayer == null)
        {
            return;
        }

        if (!_myPlayer.IsReady ||
            !_opponentPlayer.IsReady)
        {
            return;
        }

        Debug.Log("Both Players Ready");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "InGame",
            LoadSceneMode.Single
        );
    }

    /// <summary>
    /// 参加人数が1人になったときReadyを解除
    /// </summary>
    private void ResetAllReady()
    {
        NetworkPlayer[] players =
            FindObjectsByType<NetworkPlayer>(
                FindObjectsSortMode.None
            );

        foreach (NetworkPlayer player in players)
        {
            if (!player.IsSpawned)
            {
                continue;
            }

            player.ResetReadyFromServer();
        }
    }

    /// <summary>
    /// 左側に自分のキャラクターを表示
    /// </summary>
    private void UpdateMyCharacter(int characterID)
    {
        if (_currentMyCharacterID == characterID)
        {
            return;
        }

        CharacterData character =
            _characterDatabase.GetCharacter(characterID);

        if (character == null)
        {
            return;
        }

        _currentMyCharacterID = characterID;

        if (_myCharacterModel != null)
        {
            Destroy(_myCharacterModel);
        }

        if (character.CharacterModel != null)
        {
            _myCharacterModel = Instantiate(
                character.CharacterModel,
                _myCharacterModelPoint
            );

            _myCharacterModel.transform.localPosition =
                Vector3.zero;

            _myCharacterModel.transform.localRotation =
                Quaternion.identity;
        }

        _myCharacterNameText.text =
            character.CharacterName;

        _myUltNameText.text =
            character.UltName;
    }

    /// <summary>
    /// 右側に相手のキャラクターを表示
    /// </summary>
    private void UpdateOpponentCharacter(int characterID)
    {
        if (_currentOpponentCharacterID == characterID)
        {
            return;
        }

        CharacterData character =
            _characterDatabase.GetCharacter(characterID);

        if (character == null)
        {
            return;
        }

        _currentOpponentCharacterID =
            characterID;

        if (_opponentCharacterModel != null)
        {
            Destroy(_opponentCharacterModel);
        }

        if (character.CharacterModel != null)
        {
            _opponentCharacterModel = Instantiate(
                character.CharacterModel,
                _opponentCharacterModelPoint
            );

            _opponentCharacterModel.transform.localPosition =
                Vector3.zero;

            _opponentCharacterModel.transform.localRotation =
                Quaternion.identity;
        }

        _opponentCharacterNameText.text =
            character.CharacterName;

        _opponentUltNameText.text =
            character.UltName;
    }

    /// <summary>
    /// Lobby開始時の表示を初期化
    /// </summary>
    private void ClearPlayerInformation()
    {
        _myCharacterNameText.text = "";
        _myUltNameText.text = "";

        ClearOpponentCharacter();

        if (_readyButton != null)
        {
            _readyButton.interactable = false;
        }

        if (_readyButtonText != null)
        {
            _readyButtonText.text = "READY";
        }
    }

    /// <summary>
    /// 相手がいない状態へ戻す
    /// </summary>
    private void ClearOpponentCharacter()
    {
        if (_opponentCharacterModel != null)
        {
            Destroy(_opponentCharacterModel);

            _opponentCharacterModel = null;
        }

        _currentOpponentCharacterID = -1;

        _opponentCharacterNameText.text = "";
        _opponentUltNameText.text = "";
    }
}