using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Skill;

namespace NabyeolDabyeolDreamPuzzle.Character
{
    /// <summary>
    /// 모든 CharacterPackData를 모아 보관/검색하는 ScriptableObject.
    /// 캐릭터 선택 UI/SkillManager/CharacterUIManager가 이 카탈로그를 통해 캐릭터를 찾는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterPackDatabase",
        menuName = "NabyeolDabyeol/Character Pack Database",
        order = 161)]
    public class CharacterPackDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterPackData> characters = new List<CharacterPackData>();

        public IReadOnlyList<CharacterPackData> Characters => characters;
        public int Count => characters == null ? 0 : characters.Count;

        /// <summary>등록된 모든 CharacterPackData를 새 리스트로 복사해 반환한다. null 항목은 제외.</summary>
        public List<CharacterPackData> GetAllCharacters()
        {
            List<CharacterPackData> result = new List<CharacterPackData>();
            if (characters == null) return result;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null) result.Add(characters[i]);
            }
            return result;
        }

        public CharacterPackData FindById(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || characters == null) return null;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && characters[i].CharacterId == characterId) return characters[i];
            }
            return null;
        }

        public CharacterPackData FindBySkillType(SkillType skillType)
        {
            if (characters == null) return null;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && characters[i].SkillType == skillType) return characters[i];
            }
            return null;
        }

        /// <summary>
        /// 데이터베이스 일관성 검사:
        /// - null 항목 없음
        /// - 각 캐릭터 IsValid 통과
        /// - characterId 중복 없음
        /// - skillType 중복 없음 (단 SkillType.None은 예외 — 노노 등 다수 허용)
        /// </summary>
        public bool ValidateCharacters()
        {
            if (characters == null) return false;
            bool ok = true;
            HashSet<string> seenIds = new HashSet<string>();
            HashSet<SkillType> seenSkillTypes = new HashSet<SkillType>();

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterPackData c = characters[i];
                if (c == null)
                {
                    Debug.LogWarning($"CharacterPackDatabase: characters[{i}] is null.");
                    ok = false; continue;
                }
                if (!c.IsValid())
                {
                    Debug.LogWarning($"CharacterPackDatabase: characters[{i}] '{c.name}' failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(c.CharacterId) && !seenIds.Add(c.CharacterId))
                {
                    Debug.LogWarning($"CharacterPackDatabase: Duplicate characterId '{c.CharacterId}' at characters[{i}].");
                    ok = false;
                }
                if (c.SkillType != SkillType.None && !seenSkillTypes.Add(c.SkillType))
                {
                    Debug.LogWarning($"CharacterPackDatabase: Duplicate skillType '{c.SkillType}' at characters[{i}] '{c.CharacterId}'.");
                    ok = false;
                }
            }
            return ok;
        }
    }
}
