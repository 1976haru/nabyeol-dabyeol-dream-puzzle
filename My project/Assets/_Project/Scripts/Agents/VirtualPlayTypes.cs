using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>가상 스왑 후보 1건.</summary>
    public struct VirtualMove
    {
        public Vector2Int from;
        public Vector2Int to;
        public int expectedMatchCount;
        public int score;
    }

    /// <summary>가상 플레이 한 회 결과.</summary>
    public class VirtualPlayResult
    {
        public bool cleared;
        public int movesUsed;
        public int remainingMoves;
        public int finalScore;
        public int collectedCount;
        public int maxCascadeReached;
        public string failReason;
    }

    /// <summary>한 스테이지에 대한 N회 반복 결과 집계.</summary>
    public class VirtualStageReport
    {
        public int stageId;
        public string stageName;
        public int simulations;
        public int clearCount;
        public float clearRate;
        public float avgRemainingMoves;
        public float avgFinalScore;
        public float avgCollectedCount;
        public float avgMaxCascade;
        public string grade;            // Easy / Normal / Hard / TooHard
        public string recommendation;
        public Dictionary<string, int> failReasonCounts = new Dictionary<string, int>();

        public string Summary()
        {
            return $"Stage {stageId} / {stageName}\n" +
                   $"  Simulations: {simulations}\n" +
                   $"  Clear Rate: {clearRate * 100f:0.#}% ({clearCount}/{simulations})\n" +
                   $"  Avg Remaining Moves: {avgRemainingMoves:0.##}\n" +
                   $"  Avg Final Score: {avgFinalScore:0}\n" +
                   $"  Avg Collected: {avgCollectedCount:0.##}\n" +
                   $"  Avg Max Cascade: {avgMaxCascade:0.##}\n" +
                   $"  Grade: {grade}\n" +
                   $"  Recommendation: {recommendation}";
        }
    }
}
