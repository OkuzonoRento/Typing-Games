using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Database", menuName = "TypingBattle/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private List<CharacterData> _characters = new();

    public IReadOnlyList<CharacterData> Characters => _characters;

    /// <summary>
    /// CharacterID‚©‚çCharacterData‚ğæ“¾‚·‚éB
    /// Œ©‚Â‚©‚ç‚È‚©‚Á‚½ê‡‚Ínull‚ğ•Ô‚·B
    /// </summary>
    public CharacterData GetCharacter(int characterID)
    {
        foreach (CharacterData character in _characters)
        {
            if (character == null)
            {
                continue;
            }

            if (character.CharacterID == characterID)
            {
                return character;
            }
        }

        Debug.LogError(
            $"CharacterID {characterID} ‚ÌCharacterData‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñB"
        );

        return null;
    }
}