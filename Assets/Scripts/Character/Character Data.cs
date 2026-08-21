using UnityEngine;

[CreateAssetMenu(fileName = "Character_", menuName = "TypingBattle/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character")]
    [SerializeField] private int _characterID;
    [SerializeField] private string _characterName;
    [SerializeField] private GameObject _characterModel;

    [Header("ULT")]
    [SerializeField] private string _ultName;
    [SerializeField, Min(1)] private int _ultRequiredGauge;

    public int CharacterID => _characterID;
    public string CharacterName => _characterName;
    public GameObject CharacterModel => _characterModel;

    public string UltName => _ultName;
    public int UltRequiredGauge => _ultRequiredGauge;
}