using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Dialogue;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 캐릭터 대표 대사 변경 UI (템플릿 선택형).
    /// - 부모 모드에서만 접근 가능. 미진입 시 ParentModeUI.OpenParentCheck()로 우회.
    /// - 자유 입력이 아닌 CharacterPackData.RepresentativeDialogueTemplates 중에서 선택.
    /// - 선택값은 CharacterRepresentativeDialogueManager 경유로 PlayerPrefs에 저장.
    /// - 별칭 변경 UI(CharacterAliasUI)와 충돌하지 않음. 두 메뉴는 독립적인 PlayerPrefs prefix 사용.
    /// TODO: Add character profile sprite preview next to dropdown.
    /// TODO: Add per-template emoji label for younger players who cannot read yet.
    /// </summary>
    public class RepresentativeDialogueUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Button closeButton;

        [Header("Character Selector")]
        [SerializeField] private TMP_Dropdown characterDropdown;

        [Header("Template Selector")]
        [SerializeField] private TMP_Dropdown templateDropdown;
        [SerializeField] private TextMeshProUGUI previewText;

        [Header("Actions")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Parent Mode Link")]
        [SerializeField] private ParentModeUI parentModeUI;

        [Header("Messages")]
        [SerializeField] private string emptyTemplateMessage = "고를 수 있는 대사가 없어요.";
        [SerializeField] private string noCharacterMessage = "캐릭터를 먼저 골라 주세요.";
        [SerializeField] private string savedMessage = "대표 대사를 저장했어요.";
        [SerializeField] private string resetMessage = "기본 대표 대사로 돌아갔어요.";
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";
        [SerializeField] private string previewMissingMessage = "(미리보기 없음)";

        private readonly List<CharacterPackData> characterCache = new List<CharacterPackData>();
        private readonly List<CharacterDialogueTemplate> templateCache = new List<CharacterDialogueTemplate>();

        private void Awake()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(SaveSelectedDialogue);
                saveButton.onClick.AddListener(SaveSelectedDialogue);
            }
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetSelectedDialogue);
                resetButton.onClick.AddListener(ResetSelectedDialogue);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseDialogueMenu);
                closeButton.onClick.AddListener(CloseDialogueMenu);
            }
            if (characterDropdown != null)
            {
                characterDropdown.onValueChanged.RemoveListener(OnCharacterDropdownChanged);
                characterDropdown.onValueChanged.AddListener(OnCharacterDropdownChanged);
            }
            if (templateDropdown != null)
            {
                templateDropdown.onValueChanged.RemoveListener(OnTemplateDropdownChanged);
                templateDropdown.onValueChanged.AddListener(OnTemplateDropdownChanged);
            }
        }

        private void OnDestroy()
        {
            if (saveButton != null) saveButton.onClick.RemoveListener(SaveSelectedDialogue);
            if (resetButton != null) resetButton.onClick.RemoveListener(ResetSelectedDialogue);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseDialogueMenu);
            if (characterDropdown != null) characterDropdown.onValueChanged.RemoveListener(OnCharacterDropdownChanged);
            if (templateDropdown != null) templateDropdown.onValueChanged.RemoveListener(OnTemplateDropdownChanged);
        }

        /// <summary>외부 LockedMenuButton에서 호출. 부모 모드 가드 후 패널 표시.</summary>
        public void OpenDialogueMenu()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("RepresentativeDialogueUI: Parent mode required. Redirecting to parent check.");
                ShowMessage(parentModeOnlyMessage);
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                return;
            }

            PopulateCharacterDropdown();
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (messageText != null) messageText.text = string.Empty;
            Debug.Log("RepresentativeDialogueUI: Dialogue panel opened.");
        }

        public void CloseDialogueMenu()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            Debug.Log("RepresentativeDialogueUI: Dialogue panel closed.");
        }

        public void SaveSelectedDialogue()
        {
            CharacterPackData pack = GetSelectedCharacter();
            if (pack == null) { ShowMessage(noCharacterMessage); return; }
            if (CharacterRepresentativeDialogueManager.Instance == null)
            {
                Debug.LogWarning("RepresentativeDialogueUI: CharacterRepresentativeDialogueManager.Instance not found.");
                return;
            }

            CharacterDialogueTemplate template = GetSelectedTemplate();
            if (template == null)
            {
                ShowMessage(emptyTemplateMessage);
                return;
            }

            bool ok = CharacterRepresentativeDialogueManager.Instance.SetRepresentativeDialogue(pack.CharacterId, template.DialogueKey);
            if (!ok)
            {
                Debug.LogWarning($"RepresentativeDialogueUI: Failed to save representative dialogue for '{pack.CharacterId}'.");
                return;
            }
            ShowMessage(savedMessage);
            RefreshPreview();
        }

        public void ResetSelectedDialogue()
        {
            CharacterPackData pack = GetSelectedCharacter();
            if (pack == null) { ShowMessage(noCharacterMessage); return; }
            if (CharacterRepresentativeDialogueManager.Instance == null) return;

            CharacterRepresentativeDialogueManager.Instance.ClearRepresentativeDialogue(pack.CharacterId);
            ShowMessage(resetMessage);
            // 기본값(representativeDialogueKey)에 해당하는 템플릿이 있으면 드롭다운을 그 위치로 옮긴다.
            SelectTemplateMatchingKey(pack.RepresentativeDialogueKey);
            RefreshPreview();
        }

        private void PopulateCharacterDropdown()
        {
            characterCache.Clear();
            if (CharacterPackManager.Instance != null)
            {
                characterCache.AddRange(CharacterPackManager.Instance.GetAllCharacters());
            }
            else
            {
                Debug.LogWarning("RepresentativeDialogueUI: CharacterPackManager.Instance not found.");
            }

            if (characterDropdown != null)
            {
                characterDropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < characterCache.Count; i++)
                {
                    CharacterPackData c = characterCache[i];
                    if (c == null) { options.Add("(null)"); continue; }
                    // 표시명은 별칭이 있으면 별칭, 없으면 원본 이름.
                    string display = CharacterAliasManager.Instance != null
                        ? CharacterAliasManager.Instance.GetDisplayName(c)
                        : c.CharacterName;
                    options.Add(display);
                }
                characterDropdown.AddOptions(options);
                characterDropdown.value = 0;
                characterDropdown.RefreshShownValue();
            }

            PopulateTemplateDropdown();
        }

        private void PopulateTemplateDropdown()
        {
            templateCache.Clear();
            CharacterPackData pack = GetSelectedCharacter();

            if (templateDropdown != null) templateDropdown.ClearOptions();
            if (pack == null)
            {
                if (templateDropdown != null) templateDropdown.RefreshShownValue();
                RefreshPreview();
                return;
            }

            if (pack.RepresentativeDialogueTemplates != null)
            {
                for (int i = 0; i < pack.RepresentativeDialogueTemplates.Count; i++)
                {
                    CharacterDialogueTemplate t = pack.RepresentativeDialogueTemplates[i];
                    if (t == null || !t.IsValid()) continue;
                    templateCache.Add(t);
                }
            }

            List<string> options = new List<string>();
            for (int i = 0; i < templateCache.Count; i++)
            {
                options.Add(BuildTemplateLabel(templateCache[i]));
            }
            // 템플릿이 하나도 없으면 placeholder 옵션을 보여준다.
            if (options.Count == 0)
            {
                options.Add(emptyTemplateMessage);
            }

            if (templateDropdown != null)
            {
                templateDropdown.AddOptions(options);
                // 현재 저장된 dialogueKey에 일치하는 위치로 설정
                int initialIdx = FindTemplateIndexForKey(CharacterRepresentativeDialogueManager.Instance != null
                    ? CharacterRepresentativeDialogueManager.Instance.ResolveDialogueKey(pack)
                    : pack.RepresentativeDialogueKey);
                templateDropdown.value = Mathf.Clamp(initialIdx, 0, Mathf.Max(0, options.Count - 1));
                templateDropdown.RefreshShownValue();
            }
            RefreshPreview();
        }

        private string BuildTemplateLabel(CharacterDialogueTemplate t)
        {
            if (t == null) return "(null)";
            // 라벨은 미리보기 텍스트 (선택 후에도 어떤 대사인지 알기 쉽도록). 비어 있으면 description / templateId fallback.
            string text = !string.IsNullOrWhiteSpace(t.PreviewText) ? t.PreviewText
                : (!string.IsNullOrWhiteSpace(t.Description) ? t.Description : t.TemplateId);
            return text;
        }

        private int FindTemplateIndexForKey(string dialogueKey)
        {
            if (string.IsNullOrWhiteSpace(dialogueKey)) return 0;
            for (int i = 0; i < templateCache.Count; i++)
            {
                if (templateCache[i] != null && templateCache[i].DialogueKey == dialogueKey) return i;
            }
            return 0;
        }

        private void SelectTemplateMatchingKey(string dialogueKey)
        {
            if (templateDropdown == null) return;
            int idx = FindTemplateIndexForKey(dialogueKey);
            templateDropdown.value = Mathf.Clamp(idx, 0, Mathf.Max(0, templateDropdown.options.Count - 1));
            templateDropdown.RefreshShownValue();
        }

        private void OnCharacterDropdownChanged(int idx)
        {
            PopulateTemplateDropdown();
            if (messageText != null) messageText.text = string.Empty;
        }

        private void OnTemplateDropdownChanged(int idx)
        {
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (previewText == null) return;
            CharacterDialogueTemplate t = GetSelectedTemplate();
            if (t == null)
            {
                previewText.text = previewMissingMessage;
                return;
            }
            // 1차: DialogueDatabase에서 실제 문장 조회
            string text = DialogueManager.Get(t.DialogueKey, null);
            if (string.IsNullOrWhiteSpace(text) || text == t.DialogueKey)
            {
                // 2차: previewText fallback
                if (!string.IsNullOrWhiteSpace(t.PreviewText))
                {
                    previewText.text = t.PreviewText;
                    Debug.LogWarning($"RepresentativeDialogueUI: dialogueKey '{t.DialogueKey}' missing from DialogueDatabase. Using previewText fallback.");
                    return;
                }
                // 3차: key 자체
                previewText.text = t.DialogueKey;
                Debug.LogWarning($"RepresentativeDialogueUI: dialogueKey '{t.DialogueKey}' missing and no previewText. Showing raw key.");
                return;
            }
            previewText.text = text;
        }

        private CharacterPackData GetSelectedCharacter()
        {
            if (characterDropdown == null) return null;
            int idx = characterDropdown.value;
            if (idx < 0 || idx >= characterCache.Count) return null;
            return characterCache[idx];
        }

        private CharacterDialogueTemplate GetSelectedTemplate()
        {
            if (templateDropdown == null) return null;
            int idx = templateDropdown.value;
            if (idx < 0 || idx >= templateCache.Count) return null;
            return templateCache[idx];
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
