using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 캐릭터 별칭(별명) 변경 UI 패널.
    /// - 드롭다운으로 캐릭터 선택 → 현재 이름 표시 → 입력 → 저장/되돌리기/닫기.
    /// - 부모 모드가 아니면 패널이 열리지 않고 ParentModeUI.OpenParentCheck()로 우회한다.
    /// - 별칭은 CharacterPackData의 원본 characterName을 변경하지 않고 PlayerPrefs로 저장된다.
    /// - 스킬 이름은 별칭을 따르지 않는다(별자리 보기, 꿈결 움직이기 등은 그대로).
    /// TODO: Apply alias to story dialogue speakerName lookup.
    /// TODO: Add character preview sprite next to current name.
    /// </summary>
    public class CharacterAliasUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject aliasPanel;
        [SerializeField] private Button closeButton;

        [Header("Character Selector")]
        [SerializeField] private TMP_Dropdown characterDropdown;
        [SerializeField] private TextMeshProUGUI currentNameText;

        [Header("Alias Input")]
        [SerializeField] private TMP_InputField aliasInput;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;

        [Header("Parent Mode Link")]
        [Tooltip("부모 모드가 아닐 때 OpenAliasMenu가 자동으로 호출할 보호자 확인 UI.")]
        [SerializeField] private ParentModeUI parentModeUI;

        [Header("Messages")]
        [SerializeField] private string currentNameFormat = "현재 이름: {0}";
        [SerializeField] private string emptyMessage = "이름을 입력해 주세요.";
        [SerializeField] private string tooLongMessageFormat = "이름은 {0}자까지 가능해요.";
        [SerializeField] private string noCharacterMessage = "캐릭터를 먼저 골라 주세요.";
        [SerializeField] private string savedMessage = "이름이 바뀌었어요.";
        [SerializeField] private string resetMessage = "원래 이름으로 돌아왔어요.";
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";

        private readonly List<CharacterPackData> characterCache = new List<CharacterPackData>();

        private void Awake()
        {
            if (aliasPanel != null) aliasPanel.SetActive(false);

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveAlias);
                saveButton.onClick.AddListener(SaveAlias);
            }
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetAlias);
                resetButton.onClick.AddListener(ResetAlias);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseAliasMenu);
                closeButton.onClick.AddListener(CloseAliasMenu);
            }
            if (characterDropdown != null)
            {
                characterDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
                characterDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }
            if (aliasInput != null)
            {
                // 한글 입력 지원: ContentType.Standard.
                aliasInput.contentType = TMP_InputField.ContentType.Standard;
                if (CharacterAliasManager.Instance != null)
                {
                    aliasInput.characterLimit = CharacterAliasManager.Instance.MaxAliasLength;
                }
            }
        }

        private void OnDestroy()
        {
            if (saveButton != null) saveButton.onClick.RemoveListener(SaveAlias);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetAlias);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseAliasMenu);
            if (characterDropdown != null) characterDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }

        /// <summary>외부 LockedMenuButton 등에서 호출. 부모 모드 통과 시 패널 표시, 아니면 보호자 확인 UI로 우회.</summary>
        public void OpenAliasMenu()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("CharacterAliasUI: Parent mode required. Redirecting to parent check.");
                ShowMessage(parentModeOnlyMessage);
                if (parentModeUI != null)
                {
                    parentModeUI.OpenParentCheck();
                }
                return;
            }

            PopulateDropdown();
            if (aliasPanel != null) aliasPanel.SetActive(true);
            if (messageText != null) messageText.text = string.Empty;
            Debug.Log("CharacterAliasUI: Alias panel opened.");
        }

        public void CloseAliasMenu()
        {
            if (aliasPanel != null) aliasPanel.SetActive(false);
            Debug.Log("CharacterAliasUI: Alias panel closed.");
        }

        public void SaveAlias()
        {
            CharacterPackData pack = GetSelectedCharacter();
            if (pack == null)
            {
                ShowMessage(noCharacterMessage);
                return;
            }
            if (CharacterAliasManager.Instance == null)
            {
                Debug.LogWarning("CharacterAliasUI: CharacterAliasManager.Instance not found.");
                return;
            }

            string input = aliasInput != null ? aliasInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                ShowMessage(emptyMessage);
                return;
            }

            bool ok = CharacterAliasManager.Instance.SetAlias(pack.CharacterId, input);
            if (!ok)
            {
                int max = CharacterAliasManager.Instance.MaxAliasLength;
                ShowMessage(string.Format(tooLongMessageFormat, max));
                Debug.Log($"CharacterAliasUI: SetAlias rejected for '{pack.CharacterId}': '{input}'.");
                return;
            }

            ShowMessage(savedMessage);
            // OnAliasChanged 콜백으로 CharacterUIManager가 자동 갱신. 여기서는 자체 표시만 새로 한다.
            RefreshCurrentNameDisplay();
            if (aliasInput != null) aliasInput.text = string.Empty;
        }

        public void ResetAlias()
        {
            CharacterPackData pack = GetSelectedCharacter();
            if (pack == null)
            {
                ShowMessage(noCharacterMessage);
                return;
            }
            if (CharacterAliasManager.Instance == null)
            {
                Debug.LogWarning("CharacterAliasUI: CharacterAliasManager.Instance not found.");
                return;
            }

            CharacterAliasManager.Instance.ClearAlias(pack.CharacterId);
            ShowMessage(resetMessage);
            RefreshCurrentNameDisplay();
            if (aliasInput != null) aliasInput.text = string.Empty;
        }

        private void PopulateDropdown()
        {
            characterCache.Clear();
            if (CharacterPackManager.Instance != null)
            {
                characterCache.AddRange(CharacterPackManager.Instance.GetAllCharacters());
            }
            else
            {
                Debug.LogWarning("CharacterAliasUI: CharacterPackManager.Instance not found. Dropdown will be empty.");
            }

            if (characterDropdown != null)
            {
                characterDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < characterCache.Count; i++)
                {
                    CharacterPackData c = characterCache[i];
                    // 드롭다운 라벨은 원본 캐릭터 이름으로 (별칭이 있어도 어떤 캐릭터인지 알 수 있게).
                    options.Add(c != null ? c.CharacterName : "(null)");
                }
                characterDropdown.AddOptions(options);
                characterDropdown.value = 0;
                characterDropdown.RefreshShownValue();
            }

            RefreshCurrentNameDisplay();
        }

        private void OnDropdownChanged(int index)
        {
            RefreshCurrentNameDisplay();
            if (aliasInput != null) aliasInput.text = string.Empty;
            if (messageText != null) messageText.text = string.Empty;
        }

        private void RefreshCurrentNameDisplay()
        {
            CharacterPackData pack = GetSelectedCharacter();
            if (currentNameText == null) return;
            if (pack == null)
            {
                currentNameText.text = string.Empty;
                return;
            }
            string displayName = CharacterAliasManager.Instance != null
                ? CharacterAliasManager.Instance.GetDisplayName(pack)
                : pack.CharacterName;
            currentNameText.text = string.Format(currentNameFormat, displayName);
        }

        private CharacterPackData GetSelectedCharacter()
        {
            if (characterDropdown == null) return null;
            int idx = characterDropdown.value;
            if (idx < 0 || idx >= characterCache.Count) return null;
            return characterCache[idx];
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
