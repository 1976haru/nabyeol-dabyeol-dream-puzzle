using System;
using System.Collections.Generic;
using UnityEngine;
namespace NabyeolDabyeolDreamPuzzle.Collection
{
    [Serializable]
    public class CharacterData
    {
        [SerializeField] private string characterId;
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [SerializeField, TextArea(2,4)] private string shortDescription;
        [SerializeField] private string skillName;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool unlockedByDefault;
        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public string Role => role;
        public string ShortDescription => shortDescription;
        public string SkillName => skillName;
        public Sprite Icon => icon;
        public bool UnlockedByDefault => unlockedByDefault;
    }

    public class CollectionManager : MonoBehaviour
    {
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
        [SerializeField] private List<string> unlockedCharacterIds = new List<string>();
        public event Action OnCollectionChanged;
        public List<CharacterData> GetAllCharacters() => characters;
        public bool IsCharacterUnlocked(CharacterData c)
        {
            if (c == null) return false;
            if (c.UnlockedByDefault) return true;
            return !string.IsNullOrEmpty(c.CharacterId) && unlockedCharacterIds.Contains(c.CharacterId);
        }
        public bool TryUnlockCharacter(CharacterData c)
        {
            if (c == null || string.IsNullOrEmpty(c.CharacterId)) return false;
            if (unlockedCharacterIds.Contains(c.CharacterId)) return false;
            unlockedCharacterIds.Add(c.CharacterId);
            OnCollectionChanged?.Invoke();
            return true;
        }
        public int TryUnlockAllAvailableCharacters()
        {
            int count = 0;
            foreach (var c in characters)
            {
                if (c == null || string.IsNullOrEmpty(c.CharacterId)) continue;
                if (unlockedCharacterIds.Contains(c.CharacterId)) continue;
                unlockedCharacterIds.Add(c.CharacterId);
                count++;
            }
            if (count > 0) OnCollectionChanged?.Invoke();
            return count;
        }
    }
}
