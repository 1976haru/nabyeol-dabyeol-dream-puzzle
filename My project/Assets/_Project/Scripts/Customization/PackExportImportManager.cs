using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.Customization
{
    /// <summary>
    /// 커스터마이징 데이터(별칭/대표 대사/스토리 override)를 가족 기기 간 이동하기 위한
    /// 팩 내보내기/가져오기 매니저.
    /// - 원본 ScriptableObject asset은 절대 수정하지 않는다.
    /// - 진행도/앨범/지역 복구/카드/계정 정보는 export 대상이 아니다.
    /// - 부모 모드에서만 실행 가능.
    /// - JSON은 Unity JsonUtility 호환 (리스트 기반, Dictionary 미사용).
    /// TODO: Add .mallangpack extension support in v2.0.
    /// TODO: Add native share sheet for exported pack.
    /// TODO: Add file picker for import.
    /// TODO: Add QR transfer for family devices.
    /// TODO: Wire appVersion to Application.version (Unity Player Settings) before release.
    /// </summary>
    public class PackExportImportManager : MonoBehaviour
    {
        public static PackExportImportManager Instance { get; private set; }

        public const string CurrentFormatVersion = "1.0";
        private const string DefaultFilenamePrefix = "mallang_custom_pack_";
        private const string DefaultFileExtension = ".json";

        [Header("App / Pack Info")]
        [SerializeField] private string appVersion = "1.0-dev";

        [Header("Validation (Import 시 항목별 길이 제한)")]
        [SerializeField, Min(1)] private int maxAliasLength = 8;
        [SerializeField, Min(1)] private int maxStoryDialogueLength = 50;

        public event Action OnCustomizationImported;
        public event Action<string> OnExportCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("PackExportImportManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ───────── Export ─────────

        /// <summary>현재 PlayerPrefs 커스터마이징 데이터를 PackExportData로 수집한다.</summary>
        public PackExportData BuildExportData()
        {
            PackExportData data = new PackExportData
            {
                formatVersion = CurrentFormatVersion,
                appVersion = appVersion,
                exportedAt = DateTime.Now.ToString("o")
            };

            // 1) 캐릭터 별칭
            if (CharacterAliasManager.Instance != null && CharacterPackManager.Instance != null)
            {
                List<CharacterPackData> chars = CharacterPackManager.Instance.GetAllCharacters();
                for (int i = 0; i < chars.Count; i++)
                {
                    CharacterPackData c = chars[i];
                    if (c == null) continue;
                    string alias = CharacterAliasManager.Instance.GetAlias(c.CharacterId);
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        data.characterAliases.Add(new CharacterAliasExportData
                        {
                            characterId = c.CharacterId,
                            aliasName = alias
                        });
                    }
                }
            }

            // 2) 대표 대사 선택
            if (CharacterRepresentativeDialogueManager.Instance != null && CharacterPackManager.Instance != null)
            {
                List<CharacterPackData> chars = CharacterPackManager.Instance.GetAllCharacters();
                for (int i = 0; i < chars.Count; i++)
                {
                    CharacterPackData c = chars[i];
                    if (c == null) continue;
                    string key = CharacterRepresentativeDialogueManager.Instance.GetSelectedDialogueKey(c.CharacterId);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        data.representativeDialogues.Add(new RepresentativeDialogueExportData
                        {
                            characterId = c.CharacterId,
                            dialogueKey = key
                        });
                    }
                }
            }

            // 3) 스토리 대사 override
            if (StoryDialogueOverrideManager.Instance != null)
            {
                data.storyOverrides = StoryDialogueOverrideManager.Instance.GetAllOverrideExportData();
            }

            return data;
        }

        /// <summary>JsonUtility 직렬화 결과 반환. 부모 모드 가드 없음(파일 저장 단계에서 가드).</summary>
        public string ExportCustomizationToJson()
        {
            PackExportData data = BuildExportData();
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>persistentDataPath 아래 파일로 저장. 부모 모드 가드 적용.</summary>
        public bool ExportCustomizationToFile(out string savedPath)
        {
            savedPath = null;
            if (!RequireParentMode("ExportCustomizationToFile")) return false;
            try
            {
                string json = ExportCustomizationToJson();
                string filename = $"{DefaultFilenamePrefix}{DateTime.Now:yyyy-MM-dd}{DefaultFileExtension}";
                string path = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllText(path, json);
                savedPath = path;
                Debug.Log($"PackExportImportManager: Exported pack to '{path}'.");
                OnExportCompleted?.Invoke(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"PackExportImportManager: Export failed. {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        // ───────── Import ─────────

        /// <summary>JSON 문자열을 파싱해 PlayerPrefs에 적용한다. 부모 모드 가드 적용.</summary>
        public bool ImportCustomizationFromJson(string json)
        {
            if (!RequireParentMode("ImportCustomizationFromJson")) return false;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("PackExportImportManager: Import json is empty.");
                return false;
            }

            PackExportData data;
            try
            {
                data = JsonUtility.FromJson<PackExportData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PackExportImportManager: JSON parse failed. {ex.GetType().Name}: {ex.Message}");
                return false;
            }
            if (data == null)
            {
                Debug.LogWarning("PackExportImportManager: JSON parse returned null.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(data.formatVersion))
            {
                Debug.LogWarning("PackExportImportManager: formatVersion missing. Refusing import.");
                return false;
            }
            // null 안전화
            if (data.characterAliases == null) data.characterAliases = new List<CharacterAliasExportData>();
            if (data.representativeDialogues == null) data.representativeDialogues = new List<RepresentativeDialogueExportData>();
            if (data.storyOverrides == null) data.storyOverrides = new List<StoryDialogueOverrideExportData>();

            // 1) 기존 커스터마이징 초기화 (진행도 비건드림)
            ClearExistingCustomization();

            // 2) 적용
            int aliasApplied = ApplyAliases(data.characterAliases);
            int repApplied = ApplyRepresentativeDialogues(data.representativeDialogues);
            int storyApplied = StoryDialogueOverrideManager.Instance != null
                ? StoryDialogueOverrideManager.Instance.ApplyOverrideExportData(data.storyOverrides)
                : 0;

            PlayerPrefs.Save();
            Debug.Log($"PackExportImportManager: Import applied (aliases={aliasApplied}, rep={repApplied}, story={storyApplied}). formatVersion={data.formatVersion}, exportedAt={data.exportedAt}.");
            OnCustomizationImported?.Invoke();
            return true;
        }

        /// <summary>파일에서 JSON을 읽어 ImportCustomizationFromJson 호출.</summary>
        public bool ImportCustomizationFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogWarning("PackExportImportManager: Import file path is empty.");
                return false;
            }
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"PackExportImportManager: Import file not found: '{filePath}'.");
                return false;
            }
            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PackExportImportManager: File read failed. {ex.GetType().Name}: {ex.Message}");
                return false;
            }
            return ImportCustomizationFromJson(json);
        }

        private void ClearExistingCustomization()
        {
            // 진행도(StageCleared_/HighestUnlockedStageId/AlbumPageUnlocked_)는 미접근.
            // 각 매니저의 ClearAll만 호출 — CustomizationResetManager의 부모 모드 가드를 다시 거치지 않도록 직접 호출.
            if (CharacterAliasManager.Instance != null) CharacterAliasManager.Instance.ClearAllAliases();
            if (CharacterRepresentativeDialogueManager.Instance != null) CharacterRepresentativeDialogueManager.Instance.ClearAllRepresentativeDialogues();
            if (StoryDialogueOverrideManager.Instance != null) StoryDialogueOverrideManager.Instance.ClearAllOverrides();
            Debug.Log("PackExportImportManager: Existing customization cleared before import.");
        }

        private int ApplyAliases(List<CharacterAliasExportData> aliases)
        {
            if (aliases == null || CharacterAliasManager.Instance == null) return 0;
            int applied = 0;
            for (int i = 0; i < aliases.Count; i++)
            {
                var e = aliases[i];
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.characterId)) continue;
                if (string.IsNullOrWhiteSpace(e.aliasName)) continue;
                if (e.aliasName.Length > maxAliasLength) continue;
                bool ok = CharacterAliasManager.Instance.SetAlias(e.characterId, e.aliasName);
                if (ok) applied++;
            }
            return applied;
        }

        private int ApplyRepresentativeDialogues(List<RepresentativeDialogueExportData> reps)
        {
            if (reps == null || CharacterRepresentativeDialogueManager.Instance == null) return 0;
            int applied = 0;
            for (int i = 0; i < reps.Count; i++)
            {
                var e = reps[i];
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.characterId)) continue;
                if (string.IsNullOrWhiteSpace(e.dialogueKey)) continue;
                bool ok = CharacterRepresentativeDialogueManager.Instance.SetRepresentativeDialogue(e.characterId, e.dialogueKey);
                if (ok) applied++;
            }
            return applied;
        }

        // ───────── 부모 모드 가드 ─────────

        private bool RequireParentMode(string op)
        {
            if (ParentModeManager.Instance == null)
            {
                Debug.LogWarning($"PackExportImportManager: {op} blocked. ParentModeManager not found.");
                return false;
            }
            if (!ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.LogWarning($"PackExportImportManager: {op} blocked. Parent mode is not active.");
                return false;
            }
            return true;
        }
    }
}
