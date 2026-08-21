using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineManager : MonoBehaviour
{
    public static OnlineManager Instance { get; private set; }

    private const int CONNECTION_TIMEOUT_MS = 10000;

    private bool _isInitialized;
    private bool _isProcessing;
    private bool _isLeaving;

    private bool _joinedAsHost;

    private int _selectedCharacterID = -1;

    private ISession _currentSession;

    public bool IsInitialized => _isInitialized;
    public bool IsProcessing => _isProcessing;

    public ISession CurrentSession => _currentSession;

    public bool IsHost => _joinedAsHost;

    public int SelectedCharacterID => _selectedCharacterID;

    public string RoomID
    {
        get
        {
            if (_currentSession == null)
            {
                return "";
            }

            return _currentSession.Code;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        RegisterNetworkEvents();

        await InitializeOnlineAsync();
    }

    private void OnDestroy()
    {
        UnregisterNetworkEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// NGOのネットワークイベントを登録
    /// </summary>
    private void RegisterNetworkEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManagerが存在しません。");
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback +=
            OnClientDisconnected;
    }

    /// <summary>
    /// NGOのネットワークイベントを解除
    /// </summary>
    private void UnregisterNetworkEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }

    /// <summary>
    /// Unity Gaming Servicesを初期化して匿名認証
    /// </summary>
    private async Task InitializeOnlineAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _isInitialized = true;

            Debug.Log(
                $"Online Initialize Success / " +
                $"PlayerID : {AuthenticationService.Instance.PlayerId}"
            );
        }
        catch (Exception exception)
        {
            _isInitialized = false;

            Debug.LogError("Online Initialize Failed");
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// Create / Joinを押した時点の選択キャラクターを確定する
    /// </summary>
    public void SetSelectedCharacter(int characterID)
    {
        _selectedCharacterID = characterID;

        Debug.Log(
            $"Selected Character Fixed / ID : {characterID}"
        );
    }

    /// <summary>
    /// 2人用Roomを作成してHostになる
    /// </summary>
    public async void CreateRoom()
    {
        if (!CanStartOnlineProcess())
        {
            return;
        }

        if (_selectedCharacterID < 0)
        {
            Debug.LogError(
                "選択キャラクターが確定していません。"
            );

            return;
        }

        _isProcessing = true;

        try
        {
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = 2
            }.WithRelayNetwork();

            _currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            _joinedAsHost = true;

            Debug.Log("Create Room Success");
            Debug.Log($"Room ID : {_currentSession.Code}");
            Debug.Log($"Session ID : {_currentSession.Id}");
            Debug.Log(
                $"Character ID : {_selectedCharacterID}"
            );

            NetworkManager.Singleton.SceneManager.LoadScene(
                "Lobby",
                LoadSceneMode.Single
            );
        }
        catch (Exception exception)
        {
            _currentSession = null;
            _joinedAsHost = false;

            Debug.LogError("Create Room Failed");
            Debug.LogException(exception);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// RoomIDを使ってClientとしてRoomへ参加
    /// </summary>
    public async void JoinRoom(string roomID)
    {
        if (!CanStartOnlineProcess())
        {
            return;
        }

        if (_selectedCharacterID < 0)
        {
            Debug.LogError(
                "選択キャラクターが確定していません。"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(roomID))
        {
            Debug.LogWarning("Room IDが入力されていません。");
            return;
        }

        _isProcessing = true;

        roomID = roomID.Trim().ToUpper();

        try
        {
            _currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(roomID);

            _joinedAsHost = false;

            bool connected = await WaitForClientConnectionAsync();

            if (!connected)
            {
                Debug.LogError(
                    $"Join Room Failed / " +
                    $"Hostに接続できませんでした。Room ID : {roomID}"
                );

                await CleanupFailedJoinAsync();

                return;
            }

            Debug.Log("Join Room Success");
            Debug.Log($"Room ID : {_currentSession.Code}");
            Debug.Log($"Session ID : {_currentSession.Id}");
            Debug.Log(
                $"Character ID : {_selectedCharacterID}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Join Room Failed / Room ID : {roomID}"
            );

            Debug.LogException(exception);

            await CleanupFailedJoinAsync();
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// LobbyのExitボタンから呼ぶ
    /// </summary>
    public async void ExitRoom()
    {
        if (_isProcessing || _isLeaving)
        {
            return;
        }

        if (_currentSession == null)
        {
            Debug.LogWarning("現在Roomに参加していません。");

            ResetRoomData();

            ReturnToTitle();

            return;
        }

        _isProcessing = true;
        _isLeaving = true;

        try
        {
            if (_joinedAsHost)
            {
                Debug.Log("Host Exit Room");

                await _currentSession
                    .AsHost()
                    .DeleteAsync();
            }
            else
            {
                Debug.Log("Client Exit Room");

                await _currentSession.LeaveAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Room退出処理でエラーが発生しました: " +
                $"{exception.Message}"
            );
        }

        ShutdownNetwork();

        ResetRoomData();

        _isLeaving = false;
        _isProcessing = false;

        ReturnToTitle();
    }

    /// <summary>
    /// NGO ClientがHostへ接続するまで待つ
    /// </summary>
    private async Task<bool> WaitForClientConnectionAsync()
    {
        int elapsed = 0;

        while (elapsed < CONNECTION_TIMEOUT_MS)
        {
            if (NetworkManager.Singleton == null)
            {
                return false;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                return true;
            }

            await Task.Delay(100);

            elapsed += 100;
        }

        return false;
    }

    /// <summary>
    /// Join失敗時の後始末
    /// </summary>
    private async Task CleanupFailedJoinAsync()
    {
        _isLeaving = true;

        try
        {
            if (_currentSession != null)
            {
                await _currentSession.LeaveAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Join失敗後のSession退出処理でエラー: " +
                $"{exception.Message}"
            );
        }

        ShutdownNetwork();

        ResetRoomData();

        _isLeaving = false;
    }

    /// <summary>
    /// NGOからClientが切断されたとき
    /// </summary>
    private void OnClientDisconnected(ulong clientID)
    {
        if (_isLeaving)
        {
            return;
        }

        if (_joinedAsHost)
        {
            Debug.Log(
                $"Client Disconnected : {clientID}"
            );

            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (clientID == NetworkManager.Singleton.LocalClientId)
        {
            _ = HandleHostDisconnectedAsync();
        }
    }

    /// <summary>
    /// Client側でHost消失を処理
    /// </summary>
    private async Task HandleHostDisconnectedAsync()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        _isProcessing = true;

        Debug.LogWarning(
            "Hostとの接続が切断されました。Titleへ戻ります。"
        );

        try
        {
            if (_currentSession != null)
            {
                await _currentSession.LeaveAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Session退出処理に失敗しました: " +
                $"{exception.Message}"
            );
        }

        ShutdownNetwork();

        ResetRoomData();

        _isLeaving = false;
        _isProcessing = false;

        ReturnToTitle();
    }

    /// <summary>
    /// NGOの通信を終了
    /// </summary>
    private void ShutdownNetwork()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        NetworkManager.Singleton.Shutdown();
    }

    /// <summary>
    /// 現在のRoom情報と今回の選択情報をリセット
    /// </summary>
    private void ResetRoomData()
    {
        _currentSession = null;
        _joinedAsHost = false;
        _selectedCharacterID = -1;
    }

    /// <summary>
    /// Titleへ戻る
    /// </summary>
    private void ReturnToTitle()
    {
        if (SceneManager.GetActiveScene().name == "Title")
        {
            return;
        }

        SceneManager.LoadScene(
            "Title",
            LoadSceneMode.Single
        );
    }

    /// <summary>
    /// オンライン処理を開始できる状態か確認
    /// </summary>
    private bool CanStartOnlineProcess()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning(
                "Online Servicesの初期化が完了していません。"
            );

            return false;
        }

        if (_isProcessing)
        {
            return false;
        }

        if (_currentSession != null)
        {
            Debug.LogWarning(
                "すでにRoomに参加しています。"
            );

            return false;
        }

        return true;
    }
}