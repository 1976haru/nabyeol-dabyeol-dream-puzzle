using System;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Agents;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>
    /// characterId별 별칭(별명)을 PlayerPrefs에 저장/조회.
    /// CharacterPackData의 원본 characterName은 변경하지 않고, UI 표시 이름만 별칭으로 우선 표시.
    /// 별칭 변경 후 OnAliasChanged 이벤트를 발행해 CharacterUIManager가 즉시 갱신할 수 있게 한다.
    /// TODO: Add bad word filter for child-safe names.
    /// TODO: Apply alias to story dialogue speakerName.
    /// TODO: Apply alias to clear/fail popup dialogue.
    /// TODO: Add per-profile alias support.
    /// </summary>
    public class CharacterAliasManager : MonoBehaviour
    {
        public static CharacterAliasManager Instance { get; private set; }

        private const string AliasKeyPrefix = "CharacterAlias_";

        [Header("Validation")]
        [SerializeField, Min(1)] private int maxAliasLength = 8;

        public int MaxAliasLength => maxAliasLength;
        public event Action OnAliasChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CharacterAliasManager: Another instance already exists. Destroying this one.");
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
        /// 별칭을 저장. 길이/공백 검증을 통과한 경우에만 저장. 성공 시 OnAliasChanged 이벤트 발행.
        /// </summary>
        public bool SetAlias(string characterId, string aliasName)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("CharacterAliasManager: SetAlias called with empty characterId.");
                return false;
            }
            if (aliasName == null)
            {
                return false;
            }
            // 줄바꿈/탭 제거 + 앞뒤 공백 제거
            string cleaned = aliasName.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return false;
            }
            if (cleaned.Length > maxAliasLength)
            {
                return false;
            }

            // 87번 — 안전 필터 v1. 거부 시 SafetyFilterAgent.LastResult로 UI 메시지 조회 가능.
            if (SafetyFilterAgent.Instance != null)
            {
                SafetyFilterResult safety = SafetyFilterAgent.Instance.CheckAlias(cleaned);
                if (!safety.isSafe)
                {
                    Debug.LogWarning($"CharacterAliasManager: SetAlias blocked by safety filter (reason={safety.reason}).");
                    return false;
                }
            }

            PlayerPrefs.SetString(AliasKeyPrefix + characterId, cleaned);
            PlayerPrefs.Save();
            Debug.Log($"CharacterAliasManager: Alias set for '{characterId}': '{cleaned}'.");
            OnAliasChanged?.Invoke();
            return true;
        }

        /// <summary>저장된 별칭을 가져온다. 없으면 빈 문자열.</summary>
        public string GetAlias(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return string.Empty;
            return PlayerPrefs.GetString(AliasKeyPrefix + characterId, string.Empty);
        }

        public void ClearAlias(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return;
            if (!PlayerPrefs.HasKey(AliasKeyPrefix + characterId)) return;
            PlayerPrefs.DeleteKey(AliasKeyPrefix + characterId);
            PlayerPrefs.Save();
            Debug.Log($"CharacterAliasManager: Alias cleared for '{characterId}'.");
            OnAliasChanged?.Invoke();
        }

        /// <summary>CharacterPackData를 받아 최종 표시 이름을 반환. 별칭 우선, 없으면 원본 characterName.</summary>
        public string GetDisplayName(CharacterPackData pack)
        {
            if (pack == null) return string.Empty;
            string alias = GetAlias(pack.CharacterId);
            return string.IsNullOrWhiteSpace(alias) ? pack.CharacterName : alias;
        }

        /// <summary>characterId + fallback 문자열로 표시 이름 결정. CharacterPackData 참조 없이 빠른 조회.</summary>
        public string GetDisplayName(string characterId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return fallback ?? string.Empty;
            string alias = GetAlias(characterId);
            return string.IsNullOrWhiteSpace(alias) ? (fallback ?? string.Empty) : alias;
        }

        /// <summary>알려진 characterId 모두에 대해 별칭을 삭제한다. 부모 모드/디버그용.</summary>
        [ContextMenu("Clear All Character Aliases")]
        public void ClearAllAliases()
        {
            int cleared = 0;

            // 1차: CharacterPackManager가 있으면 등록된 id 순회
            if (CharacterPackManager.Instance != null)
            {
                System.Collections.Generic.List<CharacterPackData> chars = CharacterPackManager.Instance.GetAllCharacters();
                for (int i = 0; i < chars.Count; i++)
                {
                    CharacterPackData c = chars[i];
                    if (c == null) continue;
                    string key = AliasKeyPrefix + c.CharacterId;
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                        cleared++;
                    }
                }
            }
            else
            {
                // 2차 fallback: 알려진 캐릭터 id 하드코딩
                string[] knownIds = { "nabyeol", "dabyeol", "capymong", "poporing", "mochirun", "nono" };
                for (int i = 0; i < knownIds.Length; i++)
                {
                    string key = AliasKeyPrefix + knownIds[i];
                    if (PlayerPrefs.HasKey(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                        cleared++;
                    }
                }
            }

            PlayerPrefs.Save();
            Debug.Log($"CharacterAliasManager: Cleared {cleared} character aliases.");
            OnAliasChanged?.Invoke();
        }
    }
}
