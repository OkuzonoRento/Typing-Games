using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Slider _myHPSlider;
    [SerializeField] private Slider _opponentHPSlider;

    private NetworkPlayer _myPlayer;
    private NetworkPlayer _opponentPlayer;

    private void Start()
    {
        FindPlayers();
    }

    private void OnDestroy()
    {
        UnregisterHPEvents();
    }

    /// <summary>
    /// 自分と相手のNetworkPlayerを取得する
    /// </summary>
    private void FindPlayers()
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

            if (player.IsOwner)
            {
                _myPlayer = player;
            }
            else
            {
                _opponentPlayer = player;
            }
        }

        if (_myPlayer == null)
        {
            Debug.LogError(
                "自分のNetworkPlayerが見つかりません。"
            );

            return;
        }

        if (_opponentPlayer == null)
        {
            Debug.LogError(
                "相手のNetworkPlayerが見つかりません。"
            );

            return;
        }

        RegisterHPEvents();

        InitializeHPSliders();
    }

    /// <summary>
    /// HP変更イベントを登録する
    /// </summary>
    private void RegisterHPEvents()
    {
        _myPlayer.HPChanged +=
            UpdateMyHP;

        _opponentPlayer.HPChanged +=
            UpdateOpponentHP;
    }

    /// <summary>
    /// HP変更イベントを解除する
    /// </summary>
    private void UnregisterHPEvents()
    {
        if (_myPlayer != null)
        {
            _myPlayer.HPChanged -=
                UpdateMyHP;
        }

        if (_opponentPlayer != null)
        {
            _opponentPlayer.HPChanged -=
                UpdateOpponentHP;
        }
    }

    /// <summary>
    /// InGame開始時のSliderを設定する
    /// </summary>
    private void InitializeHPSliders()
    {
        UpdateMyHP(
            _myPlayer.CurrentHP,
            _myPlayer.MaxHP
        );

        UpdateOpponentHP(
            _opponentPlayer.CurrentHP,
            _opponentPlayer.MaxHP
        );
    }

    /// <summary>
    /// 自分のHP Sliderを更新する
    /// </summary>
    private void UpdateMyHP(
        int currentHP,
        int maxHP
    )
    {
        _myHPSlider.minValue = 0;
        _myHPSlider.maxValue = maxHP;

        _myHPSlider.SetValueWithoutNotify(
            currentHP
        );
    }

    /// <summary>
    /// 相手のHP Sliderを更新する
    /// </summary>
    private void UpdateOpponentHP(
        int currentHP,
        int maxHP
    )
    {
        _opponentHPSlider.minValue = 0;
        _opponentHPSlider.maxValue = maxHP;

        _opponentHPSlider.SetValueWithoutNotify(
            currentHP
        );
    }
}