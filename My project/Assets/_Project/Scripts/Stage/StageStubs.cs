using System;
using UnityEngine;
namespace NabyeolDabyeolDreamPuzzle.Stage
{
    // 주의: StageData 자체는 ScriptableObject로 별도 파일(StageData.cs)에 정의된다.
    // 본 파일은 StageData 에셋을 참조해 사용하는 런타임 매니저들의 stub만 보관한다.

    public class ScoreManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int currentScore;
        [SerializeField, Min(1)] private int scorePerBlock = 10;
        public int CurrentScore => currentScore;
        public event Action<int> OnScoreChanged;
        public void AddMatchScore(int removedBlockCount, int cascadeIndex)
        {
            int gained = Mathf.Max(0, removedBlockCount) * Mathf.Max(1, scorePerBlock) * Mathf.Max(1, cascadeIndex);
            if (gained <= 0) return;
            currentScore += gained;
            OnScoreChanged?.Invoke(currentScore);
        }
        public void ResetScore() { currentScore = 0; OnScoreChanged?.Invoke(currentScore); }
    }

    public class MoveManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int remainingMoves = 25;
        public int RemainingMoves => remainingMoves;
        public bool IsOutOfMoves => remainingMoves <= 0;
        public event Action<int> OnMovesChanged;
        public bool UseMove()
        {
            if (remainingMoves <= 0) return false;
            remainingMoves--;
            OnMovesChanged?.Invoke(remainingMoves);
            return true;
        }
        public void AddMoves(int count)
        {
            if (count <= 0) return;
            remainingMoves += count;
            OnMovesChanged?.Invoke(remainingMoves);
        }
        public void SetMoves(int count)
        {
            remainingMoves = Mathf.Max(0, count);
            OnMovesChanged?.Invoke(remainingMoves);
        }
    }

    // 주의: StageManager는 별도 파일 StageManager.cs로 분리되었다.
    // 본 파일은 GoalManager 등 의존 stub들만 보관한다.

    public class GoalManager : MonoBehaviour
    {
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private MoveManager moveManager;
        [SerializeField] private StageManager stageManager;
        public event Action OnStageCleared;
        public event Action OnStageFailed;
        private bool resolved;
        private void Awake()
        {
            if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
            if (moveManager == null) moveManager = FindAnyObjectByType<MoveManager>();
            if (stageManager == null) stageManager = FindAnyObjectByType<StageManager>();
        }
        private void OnEnable() { if (scoreManager != null) scoreManager.OnScoreChanged += HandleScoreChanged; if (moveManager != null) moveManager.OnMovesChanged += HandleMovesChanged; }
        private void OnDisable() { if (scoreManager != null) scoreManager.OnScoreChanged -= HandleScoreChanged; if (moveManager != null) moveManager.OnMovesChanged -= HandleMovesChanged; }
        public void ResetGoalFlag() { resolved = false; }
        public void NotifyStageCleared() { if (resolved) return; resolved = true; OnStageCleared?.Invoke(); }
        public void NotifyStageFailed() { if (resolved) return; resolved = true; OnStageFailed?.Invoke(); }
        private void HandleScoreChanged(int score) { if (resolved || stageManager == null) return; if (stageManager.TargetScore > 0 && score >= stageManager.TargetScore) NotifyStageCleared(); }
        private void HandleMovesChanged(int moves) { if (resolved || moves > 0 || scoreManager == null || stageManager == null) return; if (scoreManager.CurrentScore < stageManager.TargetScore) NotifyStageFailed(); }
    }
}
