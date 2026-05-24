using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Skill;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>
    /// CharacterPackDatabase에 대한 런타임 접근점.
    /// Singleton + static Get/GetBySkill 헬퍼로 어디서든 캐릭터 메타데이터 조회 가능.
    /// CharacterUIManager가 캐릭터별 UI를 갱신하거나 SkillManager가 사용자 캐릭터 정보를 알아야 할 때 사용.
    /// </summary>
    public class CharacterPackManager : MonoBehaviour
    {
        public static CharacterPackManager Instance { get; private set; }

        [SerializeField] private CharacterPackDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CharacterPackManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (database != null && !database.ValidateCharacters())
            {
                Debug.LogWarning("CharacterPackManager: Database failed ValidateCharacters() at Awake.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public CharacterPackData GetById(string characterId)
        {
            if (database == null)
            {
                Debug.LogWarning("CharacterPackManager: database is not assigned.");
                return null;
            }
            return database.FindById(characterId);
        }

        public CharacterPackData GetBySkillType(SkillType skillType)
        {
            if (database == null)
            {
                Debug.LogWarning("CharacterPackManager: database is not assigned.");
                return null;
            }
            return database.FindBySkillType(skillType);
        }

        public CharacterPackDatabase Database => database;

        /// <summary>등록된 모든 캐릭터 목록을 새 List로 반환. CharacterAliasUI 드롭다운 등에서 사용.</summary>
        public List<CharacterPackData> GetAllCharacters()
        {
            if (database == null)
            {
                Debug.LogWarning("CharacterPackManager: database is not assigned.");
                return new List<CharacterPackData>();
            }
            return database.GetAllCharacters();
        }

        // ───────── 정적 헬퍼 ─────────

        public static CharacterPackData Get(string characterId)
        {
            if (Instance == null) return null;
            return Instance.GetById(characterId);
        }

        public static CharacterPackData GetBySkill(SkillType skillType)
        {
            if (Instance == null) return null;
            return Instance.GetBySkillType(skillType);
        }
    }
}
