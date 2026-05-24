using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// 부모가 선택할 수 있는 안전한 스토리 대사 후보 묶음 ScriptableObject.
    /// StoryMakerAgent가 본 자산에서 speakerId/dialogueType을 기준으로 후보를 필터링한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoryCandidatePack",
        menuName = "NabyeolDabyeol/Story Candidate Pack",
        order = 170)]
    public class StoryCandidatePack : ScriptableObject
    {
        [SerializeField] private List<StoryCandidateData> candidates = new List<StoryCandidateData>();

        public IReadOnlyList<StoryCandidateData> Candidates => candidates;
        public int Count => candidates == null ? 0 : candidates.Count;

        public bool Validate()
        {
            if (candidates == null) return false;
            bool ok = true;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < candidates.Count; i++)
            {
                StoryCandidateData c = candidates[i];
                if (c == null)
                {
                    Debug.LogWarning($"StoryCandidatePack: candidates[{i}] is null.");
                    ok = false; continue;
                }
                if (!c.IsValid())
                {
                    Debug.LogWarning($"StoryCandidatePack: candidates[{i}] failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(c.Id) && !seen.Add(c.Id))
                {
                    Debug.LogWarning($"StoryCandidatePack: Duplicate id '{c.Id}' at candidates[{i}].");
                    ok = false;
                }
            }
            return ok;
        }
    }
}
