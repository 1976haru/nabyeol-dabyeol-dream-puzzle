using System;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Dialogue;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>
    /// 캐릭터별 "대표 대사" 선택값을 관리.
    /// - 부모 모드 UI에서 선택한 dialogueKey를 characterId별로 PlayerPrefs에 저장.
    /// - 선택값이 없으면 CharacterPackData.RepresentativeDialogueKey를 기본값으로 사용.
    /// - 실제 문장 조회는 DialogueManager(또는 DialogueDatabase)를 경유.
    /// - 별칭 변경(CharacterAliasManager)과 충돌하지 않음. 별칭은 표시 이름, 본 매니저는 대표 대사.
    /// TODO: Add per-profile representative dialogue support.
    /// TODO: Add bad word / template safety review pipeline (currently template-only, so safe by design).
    /// </summary>
    public class CharacterRepresentativeDialogueManager : MonoBehaviour
    {
        public static CharacterRepresentativeDialogueManager Instance { get; private set; }

        private const string SelectedDialogueKeyPrefix = "CharacterRepDialogue_";

        /// <summary>characterId, 선택된 dialogueKey(빈 문자열일 수 있음).</summary>
        public event Action<string, string> OnRepresentativeDialogueChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CharacterRepresentativeDialogueManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 대표 대사 선택값 저장. 캐릭터 pack에 등록된 템플릿 dialogueKey와 일치하지 않아도 저장은 허용하나 경고를 남긴다.
        /// </summary>
        public bool SetRepresentativeDialogue(string characterId, string dialogueKey)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("CharacterRepresentativeDialogueManager: SetRepresentativeDialogue called with empty characterId.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(dialogueKey))
            {
                Debug.LogWarning("CharacterRepresentativeDialogueManager: SetRepresentativeDialogue called with empty dialogueKey.");
                return false;
            }

            // 템플릿에 등록된 key인지 검증 (실패해도 저장은 진행. UI 측에서 잘못된 입력이 들어왔을 때 디버깅용 로그)
            if (CharacterPackManager.Instance != null)
            {
                CharacterPackData pack = CharacterPackManager.Get(characterId);
                if (pack != null && pack.RepresentativeDialogueTemplates != null)
                {
                    bool matched = false;
                    for (int i = 0; i < pack.RepresentativeDialogueTemplates.Count; i++)
                    {
                        CharacterDialogueTemplate t = pack.RepresentativeDialogueTemplates[i];
                        if (t != null && t.DialogueKey == dialogueKey)
                        {
                            matched = true; break;
                        }
                    }
                    if (!matched && pack.RepresentativeDialogueKey != dialogueKey)
                    {
                        Debug.LogWarning($"CharacterRepresentativeDialogueManager: dialogueKey '{dialogueKey}' is not in templates of '{characterId}'. Saving anyway.");
                    }
                }
            }

            PlayerPrefs.SetString(SelectedDialogueKeyPrefix + characterId, dialogueKey);
            PlayerPrefs.Save();
            Debug.Log($"CharacterRepresentativeDialogueManager: Representative dialogue set for '{characterId}' -> '{dialogueKey}'.");
            OnRepresentativeDialogueChanged?.Invoke(characterId, dialogueKey);
            return true;
        }

        /// <summary>저장된 dialogueKey를 반환. 없으면 빈 문자열.</summary>
        public string GetSelectedDialogueKey(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return string.Empty;
            return PlayerPrefs.GetString(SelectedDialogueKeyPrefix + characterId, string.Empty);
        }

        /// <summary>저장된 선택값이 있는지 여부.</summary>
        public bool HasSelectedDialogue(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return false;
            return PlayerPrefs.HasKey(SelectedDialogueKeyPrefix + characterId);
        }

        /// <summary>
        /// 표시용 dialogueKey를 결정.
        /// 1) PlayerPrefs 저장값 (있고 공백 아님)
        /// 2) pack.RepresentativeDialogueKey (기본 대표 대사)
        /// 3) pack.DefaultDialogueKey
        /// </summary>
        public string ResolveDialogueKey(CharacterPackData pack)
        {
            if (pack == null) return string.Empty;
            string selected = GetSelectedDialogueKey(pack.CharacterId);
            if (!string.IsNullOrWhiteSpace(selected)) return selected;
            if (!string.IsNullOrWhiteSpace(pack.RepresentativeDialogueKey)) return pack.RepresentativeDialogueKey;
            return pack.DefaultDialogueKey ?? string.Empty;
        }

        /// <summary>
        /// 표시용 대표 대사 텍스트 반환.
        /// dialogueKey가 DialogueDatabase에 없으면 매칭되는 template.previewText fallback,
        /// 그래도 없으면 pack.CharacterName 또는 빈 문자열 대신 기본 안내 문구를 반환한다.
        /// </summary>
        public string GetRepresentativeDialogueText(CharacterPackData pack)
        {
            if (pack == null) return string.Empty;
            string dialogueKey = ResolveDialogueKey(pack);

            // 1) DialogueManager가 있으면 직접 조회
            if (DialogueManager.Instance != null && DialogueManager.Instance.Database != null)
            {
                DialogueDatabase db = DialogueManager.Instance.Database;
                if (db.HasKey(dialogueKey))
                {
                    return db.GetText(dialogueKey);
                }
                // 누락 → template.previewText fallback
                string preview = FindTemplatePreview(pack, dialogueKey);
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    return preview;
                }
                Debug.LogWarning($"CharacterRepresentativeDialogueManager: dialogueKey '{dialogueKey}' missing in DialogueDatabase and no template preview.");
                return dialogueKey;
            }

            // 2) DialogueManager가 없을 때 — template.previewText 또는 key
            string fallback = FindTemplatePreview(pack, dialogueKey);
            return !string.IsNullOrWhiteSpace(fallback) ? fallback : dialogueKey;
        }

        private string FindTemplatePreview(CharacterPackData pack, string dialogueKey)
        {
            if (pack == null || pack.RepresentativeDialogueTemplates == null) return null;
            for (int i = 0; i < pack.RepresentativeDialogueTemplates.Count; i++)
            {
                CharacterDialogueTemplate t = pack.RepresentativeDialogueTemplates[i];
                if (t == null) continue;
                if (t.DialogueKey == dialogueKey) return t.PreviewText;
            }
            return null;
        }

        /// <summary>특정 캐릭터의 대표 대사 선택을 초기화. 기본 representativeDialogueKey로 돌아간다.</summary>
        public void ClearRepresentativeDialogue(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return;
            string key = SelectedDialogueKeyPrefix + characterId;
            if (!PlayerPrefs.HasKey(key)) return;
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"CharacterRepresentativeDialogueManager: Representative dialogue cleared for '{characterId}'.");
            OnRepresentativeDialogueChanged?.Invoke(characterId, string.Empty);
        }

        /// <summary>모든 캐릭터의 대표 대사 선택을 초기화.</summary>
        [ContextMenu("Clear All Representative Dialogues")]
        public void ClearAllRepresentativeDialogues()
        {
            int cleared = 0;
            if (CharacterPackManager.Instance != null)
            {
                System.Collections.Generic.List<CharacterPackData> chars = CharacterPackManager.Instance.GetAllCharacters();
                for (int i = 0; i < chars.Count; i++)
                {
                    CharacterPackData c = chars[i];
                    if (c == null) continue;
                    string key = SelectedDialogueKeyPrefix + c.CharacterId;
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                        cleared++;
                    }
                }
            }
            else
            {
                string[] knownIds = { "nabyeol", "dabyeol", "capymong", "poporing", "mochirun", "nono" };
                for (int i = 0; i < knownIds.Length; i++)
                {
                    string key = SelectedDialogueKeyPrefix + knownIds[i];
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                        cleared++;
                    }
                }
            }
            PlayerPrefs.Save();
            Debug.Log($"CharacterRepresentativeDialogueManager: Cleared {cleared} representative dialogues.");
            OnRepresentativeDialogueChanged?.Invoke(string.Empty, string.Empty);
        }
    }
}
