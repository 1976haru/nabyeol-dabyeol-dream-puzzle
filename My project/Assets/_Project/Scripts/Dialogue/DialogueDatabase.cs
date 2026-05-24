using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Dialogue
{
    /// <summary>
    /// 캐릭터 기본 대사·스킬 성공/실패 대사·스킬 튜토리얼 문구·시스템 안내 문구를
    /// 한 곳에서 관리하는 ScriptableObject. StoryNode는 스테이지 흐름 대사 전용으로 유지.
    /// TODO: Add JSON import/export for dialogue database (StreamingAssets or Resources).
    /// </summary>
    [CreateAssetMenu(
        fileName = "DialogueDatabase",
        menuName = "NabyeolDabyeol/Dialogue Database",
        order = 150)]
    public class DialogueDatabase : ScriptableObject
    {
        [SerializeField] private List<DialogueEntry> entries = new List<DialogueEntry>();

        // 런타임 캐시. Awake에 의존하지 않고 첫 조회 시 lazy 구축.
        private Dictionary<string, DialogueEntry> cache;

        public IReadOnlyList<DialogueEntry> Entries => entries;
        public int Count => entries == null ? 0 : entries.Count;

        public DialogueEntry GetEntry(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            EnsureCache();
            return cache.TryGetValue(key, out DialogueEntry entry) ? entry : null;
        }

        /// <summary>
        /// key에 해당하는 텍스트를 반환. 키 누락 시 fallback이 있으면 fallback, 없으면 key 자체.
        /// </summary>
        public string GetText(string key, string fallback = null)
        {
            DialogueEntry entry = GetEntry(key);
            if (entry != null && entry.IsValid()) return entry.Text;
            return fallback ?? key;
        }

        public bool HasKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            EnsureCache();
            return cache.ContainsKey(key);
        }

        public bool ValidateEntries()
        {
            if (entries == null) return false;
            bool ok = true;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                DialogueEntry e = entries[i];
                if (e == null)
                {
                    Debug.LogWarning($"DialogueDatabase: entries[{i}] is null.");
                    ok = false; continue;
                }
                if (!e.IsValid())
                {
                    Debug.LogWarning($"DialogueDatabase: entries[{i}] key='{e.Key}' failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(e.Key) && !seen.Add(e.Key))
                {
                    Debug.LogWarning($"DialogueDatabase: Duplicate key '{e.Key}' at entries[{i}].");
                    ok = false;
                }
            }
            return ok;
        }

        /// <summary>Editor에서 entries를 수정한 뒤 캐시를 비우고 다시 빌드하도록 강제.</summary>
        public void InvalidateCache()
        {
            cache = null;
        }

        private void EnsureCache()
        {
            if (cache != null) return;
            cache = new Dictionary<string, DialogueEntry>();
            if (entries == null) return;
            for (int i = 0; i < entries.Count; i++)
            {
                DialogueEntry e = entries[i];
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.Key)) continue;
                if (!cache.ContainsKey(e.Key)) cache[e.Key] = e;
            }
        }
    }
}
