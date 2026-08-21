using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Battle Status")]
    [SerializeField, Min(1)] private int _maxHP = 200;

    // =========================
    // Lobby
    // =========================

    private readonly NetworkVariable<int> _characterID =
        new(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<bool> _isReady =
        new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================
    // Battle
    // =========================

    private readonly NetworkVariable<int> _currentHP =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int> _combo =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int> _ultGauge =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // =========================
    // Result Status
    // =========================

    private readonly NetworkVariable<int> _totalKeystrokes =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int> _typeMisses =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int> _completedWords =
        new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public int CharacterID => _characterID.Value;
    public bool IsReady => _isReady.Value;

    public int MaxHP => _maxHP;
    public int CurrentHP => _currentHP.Value;
    public int Combo => _combo.Value;
    public int UltGauge => _ultGauge.Value;

    public int TotalKeystrokes => _totalKeystrokes.Value;
    public int TypeMisses => _typeMisses.Value;
    public int CompletedWords => _completedWords.Value;

    public bool IsDead => _currentHP.Value <= 0;

    public event Action<int, int> HPChanged;

    public override void OnNetworkSpawn()
    {
        _characterID.OnValueChanged += OnCharacterIDChanged;
        _isReady.OnValueChanged += OnReadyChanged;
        _currentHP.OnValueChanged += OnHPChanged;

        if (IsOwner)
        {
            if (OnlineManager.Instance != null)
            {
                SendCharacterIDToServerRpc(
                    OnlineManager.Instance.SelectedCharacterID
                );
            }
        }

        NotifyLobby();

        HPChanged?.Invoke(
            _currentHP.Value,
            _maxHP
        );
    }

    public override void OnNetworkDespawn()
    {
        _characterID.OnValueChanged -= OnCharacterIDChanged;
        _isReady.OnValueChanged -= OnReadyChanged;
        _currentHP.OnValueChanged -= OnHPChanged;

        NotifyLobby();
    }

    // =====================================================
    // Character
    // =====================================================

    [ServerRpc]
    private void SendCharacterIDToServerRpc(int characterID)
    {
        if (characterID < 0)
        {
            Debug.LogWarning(
                $"Client {OwnerClientId} から不正なCharacterIDが送信されました。"
            );

            return;
        }

        _characterID.Value = characterID;

        Debug.Log(
            $"Character Sync / " +
            $"ClientID : {OwnerClientId} / " +
            $"CharacterID : {characterID}"
        );
    }

    // =====================================================
    // Ready
    // =====================================================

    public void SetReady(bool isReady)
    {
        if (!IsOwner)
        {
            return;
        }

        SetReadyServerRpc(isReady);
    }

    [ServerRpc]
    private void SetReadyServerRpc(bool isReady)
    {
        _isReady.Value = isReady;
    }

    public void ResetReadyFromServer()
    {
        if (!IsServer)
        {
            return;
        }

        _isReady.Value = false;
    }

    // =====================================================
    // Battle
    // =====================================================

    /// <summary>
    /// 対戦開始時のステータスを初期化する
    /// </summary>
    public void InitializeBattleStatus()
    {
        if (!IsServer)
        {
            return;
        }

        _currentHP.Value = _maxHP;
        _combo.Value = 0;
        _ultGauge.Value = 0;

        _totalKeystrokes.Value = 0;
        _typeMisses.Value = 0;
        _completedWords.Value = 0;

        _isReady.Value = false;

        Debug.Log(
            $"Battle Status Initialize / " +
            $"ClientID : {OwnerClientId} / " +
            $"HP : {_currentHP.Value}"
        );
    }

    /// <summary>
    /// Host側でHPを減らす
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!IsServer)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        if (_currentHP.Value <= 0)
        {
            return;
        }

        _currentHP.Value =
            Mathf.Max(
                0,
                _currentHP.Value - damage
            );

        Debug.Log(
            $"Damage / " +
            $"ClientID : {OwnerClientId} / " +
            $"Damage : {damage} / " +
            $"HP : {_currentHP.Value}"
        );
    }

    /// <summary>
    /// 動作確認用。
    /// 自分からHostへテスト攻撃を要求する。
    /// </summary>
    public void RequestDebugAttack()
    {
        if (!IsOwner)
        {
            return;
        }

        DebugAttackServerRpc();
    }

    /// <summary>
    /// Hostが攻撃者以外のNetworkPlayerを探して
    /// 10ダメージ与える。
    /// </summary>
    [ServerRpc]
    private void DebugAttackServerRpc()
    {
        const int DEBUG_DAMAGE = 10;

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

            // 自分自身には攻撃しない
            if (player.OwnerClientId == OwnerClientId)
            {
                continue;
            }

            player.TakeDamage(DEBUG_DAMAGE);

            Debug.Log(
                $"Debug Attack / " +
                $"Attacker : {OwnerClientId} / " +
                $"Target : {player.OwnerClientId} / " +
                $"Damage : {DEBUG_DAMAGE}"
            );

            return;
        }
    }

    public void AddCombo()
    {
        if (!IsServer)
        {
            return;
        }

        _combo.Value++;
    }

    public void ResetCombo()
    {
        if (!IsServer)
        {
            return;
        }

        _combo.Value = 0;
    }

    public void AddUltGauge(int amount)
    {
        if (!IsServer)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        _ultGauge.Value += amount;
    }

    // =====================================================
    // Result Status
    // =====================================================

    public void AddKeystroke()
    {
        if (!IsServer)
        {
            return;
        }

        _totalKeystrokes.Value++;
    }

    public void AddTypeMiss()
    {
        if (!IsServer)
        {
            return;
        }

        _typeMisses.Value++;
    }

    public void AddCompletedWord()
    {
        if (!IsServer)
        {
            return;
        }

        _completedWords.Value++;
    }

    // =====================================================
    // NetworkVariable Events
    // =====================================================

    private void OnCharacterIDChanged(
        int previousValue,
        int newValue
    )
    {
        NotifyLobby();
    }

    private void OnReadyChanged(
        bool previousValue,
        bool newValue
    )
    {
        NotifyLobby();
    }

    private void OnHPChanged(
        int previousValue,
        int newValue
    )
    {
        HPChanged?.Invoke(
            newValue,
            _maxHP
        );
    }

    // =====================================================
    // Lobby
    // =====================================================

    private void NotifyLobby()
    {
        if (LobbyManager.Instance == null)
        {
            return;
        }

        LobbyManager.Instance.RefreshPlayers();
    }
}