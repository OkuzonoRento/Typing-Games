using UnityEngine;
using UnityEngine.InputSystem;

public class BattleDebugController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private InGameManager _inGameManager;

    private NetworkPlayer _myPlayer;

    private void Start()
    {
        FindMyPlayer();
    }

    private void Update()
    {
        if (_inGameManager == null)
        {
            return;
        }

        // カウントダウン終了前は攻撃できない
        if (!_inGameManager.CanInput)
        {
            return;
        }

        if (_myPlayer == null)
        {
            FindMyPlayer();

            if (_myPlayer == null)
            {
                return;
            }
        }

        if (Keyboard.current == null)
        {
            return;
        }

        // 動作確認用
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _myPlayer.RequestDebugAttack();
        }
    }

    /// <summary>
    /// このPCが所有しているNetworkPlayerを取得する
    /// </summary>
    private void FindMyPlayer()
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

            if (!player.IsOwner)
            {
                continue;
            }

            _myPlayer = player;

            return;
        }
    }
}