using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterDatabase _characterDatabase;
    [SerializeField] private Transform _characterModelPoint;

    [Header("UI")]
    [SerializeField] private TMP_Text _characterNameText;
    [SerializeField] private TMP_Text _ultNameText;

    [Header("Input")]
    [SerializeField] private InputActionReference _selectLeft;
    [SerializeField] private InputActionReference _selectRight;

    private int _selectIndex;
    private GameObject _currentCharacterModel;

    public CharacterData SelectedCharacter
    {
        get
        {
            if (_characterDatabase == null)
            {
                return null;
            }

            if (_characterDatabase.Characters.Count == 0)
            {
                return null;
            }

            return _characterDatabase.Characters[_selectIndex];
        }
    }

    public int SelectedCharacterID
    {
        get
        {
            CharacterData character = SelectedCharacter;

            if (character == null)
            {
                return -1;
            }

            return character.CharacterID;
        }
    }

    private void Awake()
    {
        _selectIndex = 0;
    }

    private void OnEnable()
    {
        _selectLeft.action.Enable();
        _selectRight.action.Enable();

        _selectLeft.action.performed += OnSelectLeftInput;
        _selectRight.action.performed += OnSelectRightInput;
    }

    private void OnDisable()
    {
        _selectLeft.action.performed -= OnSelectLeftInput;
        _selectRight.action.performed -= OnSelectRightInput;

        _selectLeft.action.Disable();
        _selectRight.action.Disable();
    }

    private void Start()
    {
        UpdateCharacter();
    }

    private void OnSelectLeftInput(InputAction.CallbackContext context)
    {
        SelectLeft();
    }

    private void OnSelectRightInput(InputAction.CallbackContext context)
    {
        SelectRight();
    }

    public void SelectLeft()
    {
        if (!CanSelect())
        {
            return;
        }

        _selectIndex--;

        if (_selectIndex < 0)
        {
            _selectIndex = _characterDatabase.Characters.Count - 1;
        }

        UpdateCharacter();
    }

    public void SelectRight()
    {
        if (!CanSelect())
        {
            return;
        }

        _selectIndex++;

        if (_selectIndex >= _characterDatabase.Characters.Count)
        {
            _selectIndex = 0;
        }

        UpdateCharacter();
    }

    private bool CanSelect()
    {
        if (_characterDatabase == null)
        {
            Debug.LogError("CharacterDatabaseが設定されていません。");
            return false;
        }

        if (_characterDatabase.Characters.Count == 0)
        {
            Debug.LogWarning("CharacterDataが登録されていません。");
            return false;
        }

        return true;
    }

    private void UpdateCharacter()
    {
        CharacterData character = SelectedCharacter;

        if (character == null)
        {
            Debug.LogWarning("選択できるCharacterDataがありません。");
            return;
        }

        UpdateCharacterModel(character);
        UpdateCharacterInformation(character);

        Debug.Log(
            $"Selected Character : " +
            $"{character.CharacterName} / ID {character.CharacterID}"
        );
    }

    private void UpdateCharacterModel(CharacterData character)
    {
        if (_characterModelPoint == null)
        {
            Debug.LogError("CharacterModelPointが設定されていません。");
            return;
        }

        if (_currentCharacterModel != null)
        {
            Destroy(_currentCharacterModel);
        }

        if (character.CharacterModel == null)
        {
            Debug.LogWarning(
                $"{character.CharacterName} にCharacterModelが設定されていません。"
            );

            return;
        }

        _currentCharacterModel = Instantiate(
            character.CharacterModel,
            _characterModelPoint
        );

        _currentCharacterModel.transform.localPosition = Vector3.zero;
        _currentCharacterModel.transform.localRotation = Quaternion.identity;
    }

    private void UpdateCharacterInformation(CharacterData character)
    {
        if (_characterNameText != null)
        {
            _characterNameText.text = character.CharacterName;
        }

        if (_ultNameText != null)
        {
            _ultNameText.text = character.UltName;
        }
    }
}
