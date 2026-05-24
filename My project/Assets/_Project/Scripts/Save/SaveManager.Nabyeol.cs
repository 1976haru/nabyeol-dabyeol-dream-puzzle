using System.Collections.Generic;
using UnityEngine;
namespace NabyeolDabyeolDreamPuzzle.Save
{
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private List<int> clearedStageNumbers = new List<int>();
        [SerializeField] private List<string> unlockedLearningCardIds = new List<string>();
        [SerializeField, Min(0)] private int sparklePieces;
        [SerializeField, Min(0)] private int highestScore;
        [SerializeField, Min(0)] private int lastRemainingMoves;
        public IReadOnlyList<int> ClearedStageNumbers => clearedStageNumbers;
        public IReadOnlyList<string> UnlockedLearningCardIds => unlockedLearningCardIds;
        public int SparklePieces => sparklePieces;
        public int HighestScore => highestScore;
        public int LastRemainingMoves => lastRemainingMoves;
        public void MarkStageClearedAndSave(int stageNumber, int score, int remainingMoves)
        {
            if (stageNumber < 1) return;
            if (!clearedStageNumbers.Contains(stageNumber)) clearedStageNumbers.Add(stageNumber);
            if (score > highestScore) highestScore = score;
            lastRemainingMoves = Mathf.Max(0, remainingMoves);
            Persist();
        }
        public void UnlockLearningCardAndSave(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return;
            if (!unlockedLearningCardIds.Contains(cardId)) unlockedLearningCardIds.Add(cardId);
            Persist();
        }
        public void AddSparklePiecesAndSave(int amount)
        {
            if (amount <= 0) return;
            sparklePieces += amount;
            Persist();
        }
        public bool IsStageCleared(int stageNumber) => clearedStageNumbers.Contains(stageNumber);
        public bool IsLearningCardUnlocked(string cardId) => !string.IsNullOrEmpty(cardId) && unlockedLearningCardIds.Contains(cardId);
        protected virtual void Persist() { }
    }
}
