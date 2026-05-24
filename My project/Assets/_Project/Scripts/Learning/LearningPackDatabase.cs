using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.Learning
{
    /// <summary>
    /// 모든 LearningPackData를 모아 보관하는 ScriptableObject.
    /// KnowledgeCardDatabase가 cardId 기준 단일 카드 검색을 담당하고,
    /// 본 데이터베이스는 학습 단위 카드 묶음 + 학습 목표 검색을 담당한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LearningPackDatabase",
        menuName = "NabyeolDabyeol/Learning Pack Database",
        order = 181)]
    public class LearningPackDatabase : ScriptableObject
    {
        [SerializeField] private List<LearningPackData> learningPacks = new List<LearningPackData>();

        public IReadOnlyList<LearningPackData> LearningPacks => learningPacks;
        public int Count => learningPacks == null ? 0 : learningPacks.Count;

        public LearningPackData FindByPackId(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId) || learningPacks == null) return null;
            for (int i = 0; i < learningPacks.Count; i++)
            {
                if (learningPacks[i] != null && learningPacks[i].PackId == packId) return learningPacks[i];
            }
            return null;
        }

        public LearningPackData FindPackByStageId(int stageId)
        {
            if (learningPacks == null) return null;
            for (int i = 0; i < learningPacks.Count; i++)
            {
                if (learningPacks[i] != null && learningPacks[i].ContainsStage(stageId)) return learningPacks[i];
            }
            return null;
        }

        public LearningPackData FindPackByCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) || learningPacks == null) return null;
            for (int i = 0; i < learningPacks.Count; i++)
            {
                if (learningPacks[i] != null && learningPacks[i].FindCardByCardId(cardId) != null) return learningPacks[i];
            }
            return null;
        }

        public bool ValidatePacks()
        {
            if (learningPacks == null) return false;
            bool ok = true;
            HashSet<string> seenPackIds = new HashSet<string>();
            Dictionary<string, List<string>> cardIdToPackIds = new Dictionary<string, List<string>>();

            for (int p = 0; p < learningPacks.Count; p++)
            {
                LearningPackData pack = learningPacks[p];
                if (pack == null)
                {
                    Debug.LogWarning($"LearningPackDatabase: learningPacks[{p}] is null.");
                    ok = false; continue;
                }
                if (!pack.IsValid())
                {
                    Debug.LogWarning($"LearningPackDatabase: learningPacks[{p}] '{pack.name}' failed IsValid().");
                    ok = false;
                }
                if (!string.IsNullOrWhiteSpace(pack.PackId) && !seenPackIds.Add(pack.PackId))
                {
                    Debug.LogWarning($"LearningPackDatabase: Duplicate packId '{pack.PackId}' at learningPacks[{p}].");
                    ok = false;
                }

                // 카드 중복 추적 (경고만, 오류 아님)
                if (pack.Cards != null)
                {
                    for (int c = 0; c < pack.Cards.Count; c++)
                    {
                        KnowledgeCardData card = pack.Cards[c];
                        if (card == null) continue;
                        if (!cardIdToPackIds.TryGetValue(card.CardId, out List<string> list))
                        {
                            list = new List<string>();
                            cardIdToPackIds[card.CardId] = list;
                        }
                        list.Add(pack.PackId);
                    }
                }

                // 학습 목표 검증
                if (pack.LearningGoals != null)
                {
                    HashSet<string> seenGoalIds = new HashSet<string>();
                    for (int g = 0; g < pack.LearningGoals.Count; g++)
                    {
                        LearningGoalData goal = pack.LearningGoals[g];
                        if (goal == null) continue;
                        if (!goal.IsValid())
                        {
                            Debug.LogWarning($"LearningPackDatabase: pack '{pack.PackId}' goal[{g}] failed IsValid().");
                            ok = false;
                        }
                        if (!string.IsNullOrWhiteSpace(goal.GoalId) && !seenGoalIds.Add(goal.GoalId))
                        {
                            Debug.LogWarning($"LearningPackDatabase: pack '{pack.PackId}' Duplicate goalId '{goal.GoalId}' at index {g}.");
                            ok = false;
                        }
                        // linkedCardId가 비어 있지 않다면 pack.cards에 존재해야 한다
                        if (!string.IsNullOrWhiteSpace(goal.LinkedCardId) && pack.FindCardByCardId(goal.LinkedCardId) == null)
                        {
                            Debug.LogWarning($"LearningPackDatabase: pack '{pack.PackId}' goal '{goal.GoalId}' linkedCardId '{goal.LinkedCardId}' not found in pack.cards.");
                            ok = false;
                        }
                        // linkedStageId가 pack stage 범위 안인지 확인
                        if (!pack.ContainsStage(goal.LinkedStageId))
                        {
                            Debug.LogWarning($"LearningPackDatabase: pack '{pack.PackId}' goal '{goal.GoalId}' linkedStageId {goal.LinkedStageId} outside pack range [{pack.StartStageId}..{pack.EndStageId}].");
                            ok = false;
                        }
                    }
                }
            }

            // 카드 중복 경고 (오류 아님)
            foreach (KeyValuePair<string, List<string>> kv in cardIdToPackIds)
            {
                if (kv.Value.Count > 1)
                {
                    Debug.LogWarning($"LearningPackDatabase: cardId '{kv.Key}' appears in multiple packs [{string.Join(", ", kv.Value)}]. (정책상 허용되지만 기본 데이터에서는 중복이 없어야 함)");
                }
            }

            return ok;
        }
    }
}
