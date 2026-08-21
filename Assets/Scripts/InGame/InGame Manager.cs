using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class InGameManager : NetworkBehaviour
{
    [Header("Countdown")]
    [SerializeField] private TMP_Text _countdownText;

    private const float COUNTDOWN_INTERVAL = 1.0f;
    private const float START_DISPLAY_TIME = 0.5f;

    // InGameまで到達したClient
    private readonly HashSet<ulong> _loadedClients = new();

    // カウントダウン二重開始防止
    private bool _countdownStarted;

    // ローカル側の入力許可
    private bool _canInput;

    // Hostがゲーム開始状態を管理する
    private readonly NetworkVariable<bool> _isGameStarted =
        new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool CanInput => _canInput;
    public bool IsGameStarted => _isGameStarted.Value;

    public override void OnNetworkSpawn()
    {
        _isGameStarted.OnValueChanged +=
            OnGameStartedChanged;

        _canInput = false;

        if (_countdownText != null)
        {
            _countdownText.text = "";
        }

        // 各プレイヤーがInGameへ到達したことをHostへ通知
        if (IsClient)
        {
            NotifyLoadedServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        _isGameStarted.OnValueChanged -=
            OnGameStartedChanged;
    }

    /// <summary>
    /// InGameへ到達したことをHostへ通知する
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void NotifyLoadedServerRpc(
        ServerRpcParams rpcParams = default
    )
    {
        ulong clientID =
            rpcParams.Receive.SenderClientId;

        if (_loadedClients.Contains(clientID))
        {
            return;
        }

        _loadedClients.Add(clientID);

        Debug.Log(
            $"InGame Loaded / ClientID : {clientID}"
        );

        CheckAllPlayersLoaded();
    }

    /// <summary>
    /// Hostが双方のロード完了を確認する
    /// </summary>
    private void CheckAllPlayersLoaded()
    {
        if (!IsServer)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count != 2)
        {
            return;
        }

        if (_loadedClients.Count != 2)
        {
            return;
        }

        if (_countdownStarted)
        {
            return;
        }

        _countdownStarted = true;

        // カウントダウン開始前に戦闘状態を初期化
        InitializePlayers();

        Debug.Log(
            "All Players Loaded / Countdown Start"
        );

        StartCountdownClientRpc();
    }

    /// <summary>
    /// Host側で双方の戦闘ステータスを初期化する
    /// </summary>
    private void InitializePlayers()
    {
        if (!IsServer)
        {
            return;
        }

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

            player.InitializeBattleStatus();
        }
    }

    /// <summary>
    /// 全Clientでカウントダウンを開始する
    /// </summary>
    [ClientRpc]
    private void StartCountdownClientRpc()
    {
        StartCoroutine(
            CountdownCoroutine()
        );
    }

    /// <summary>
    /// 3 → 2 → 1 → START
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        _canInput = false;

        if (_countdownText == null)
        {
            yield break;
        }

        _countdownText.text = "3";

        yield return new WaitForSeconds(
            COUNTDOWN_INTERVAL
        );

        _countdownText.text = "2";

        yield return new WaitForSeconds(
            COUNTDOWN_INTERVAL
        );

        _countdownText.text = "1";

        yield return new WaitForSeconds(
            COUNTDOWN_INTERVAL
        );

        _countdownText.text = "START";

        yield return new WaitForSeconds(
            START_DISPLAY_TIME
        );

        _countdownText.text = "";

        // ゲーム開始の確定はHostだけ
        if (IsServer)
        {
            _isGameStarted.Value = true;
        }
    }

    /// <summary>
    /// Hostがゲーム開始を確定したときに呼ばれる
    /// </summary>
    private void OnGameStartedChanged(
        bool previousValue,
        bool newValue
    )
    {
        _canInput = newValue;

        if (newValue)
        {
            Debug.Log(
                "GAME START / Input Enabled"
            );
        }
    }
}