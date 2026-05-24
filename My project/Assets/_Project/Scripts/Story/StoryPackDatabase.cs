using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 모든 StoryPackData를 모아 보관/검색하는 ScriptableObject.
    /// StoryManager가 fallback 경로로 이 데이터베이스를 통해 stageId로 StoryNode를 찾는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoryPackDatabase",
        menuName = "NabyeolDabyeol/Story Pack Database",
        order = 171)]
    public class StoryPackDatabase : ScriptableObject
    {
        [SerializeField] private List<StoryPackData> packs = new List<StoryPackData>();

        public IReadOnlyList<StoryPackData> Packs => packs;
        public int Count => packs == null ? 0 : packs.Count;

        public StoryPackData FindByPackId(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId) || packs == null) return null;
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i] != null && packs[i].PackId == packId) return packs[i];
            }
            return null;
        }

        /// <summary>지정된 stageId를 포함하는 첫 번째 팩을 반환.</summary>
        public StoryPackData FindPackByStageId(int stageId)
        {
            if (packs == null) return null;
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i] != null && packs[i].ContainsStage(stageId)) return packs[i];
            }
            return null;
        }

        /// <summary>모든 팩을 순회하며 linkedStageId가 매칭되는 첫 StoryNode를 반환.</summary>
        public StoryNode FindStoryNodeByStageId(int stageId)
        {
            if (packs == null) return null;
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i] == null) continue;
                StoryNode found = packs[i].FindStoryNodeByStageId(stageId);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>모든 팩을 순회하며 eventId가 매칭되는 첫 StoryEventTemplate을 반환.</summary>
        public StoryEventTemplate FindEventByEventId(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || packs == null) return null;
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i] == null) continue;
                StoryEventTemplate t = packs[i].FindEventByEventId(eventId);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>
        /// 팩 일관성 검사:
        /// - null 팩 없음, 각 팩 IsValid 통과
        /// - packId 중복 없음
        /// - 같은 stageId를 가진 StoryNode가 여러 팩에 등장하면 경고 (오류는 아님 — 프롤로그·노노 중복 의도적 허용)
        /// </summary>
        public bool ValidatePacks()
        {
            if (packs == null) return false;
            bool ok = true;
            HashSet<string> seenPackIds = new HashSet<string>();
            Dictionary<int, List<string>> stageIdToPackIds = new Dictionary<int, List<string>>();

            for (int i = 0; i < packs.Count; i++)
            {
                StoryPackData p = packs[i];
                if (p == null)
                {
                    Debug.LogWarning($"StoryPackDatabase: packs[{i}] is null.");
                    ok = false; continue;
                }
                if (!p.IsValid())
                {
                    Debug.LogWarning($"StoryPackDatabase: packs[{i}] '{p.name}' failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(p.PackId) && !seenPackIds.Add(p.PackId))
                {
                    Debug.LogWarning($"StoryPackDatabase: Duplicate packId '{p.PackId}' at packs[{i}].");
                    ok = false;
                }

                if (p.StoryNodes != null)
                {
                    for (int j = 0; j < p.StoryNodes.Count; j++)
                    {
                        StoryNode n = p.StoryNodes[j];
                        if (n == null) continue;
                        if (!stageIdToPackIds.TryGetValue(n.LinkedStageId, out List<string> list))
                        {
                            list = new List<string>();
                            stageIdToPackIds[n.LinkedStageId] = list;
                        }
                        list.Add(p.PackId);
                    }
                }
            }

            foreach (KeyValuePair<int, List<string>> kv in stageIdToPackIds)
            {
                if (kv.Value.Count > 1)
                {
                    Debug.LogWarning($"StoryPackDatabase: linkedStageId {kv.Key} appears in multiple packs [{string.Join(", ", kv.Value)}]. (의도적 중복일 수 있음 — 프롤로그/노노 등)");
                }
            }

            return ok;
        }
    }
}
