using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 가상 플레이 시뮬레이터 v1.
    /// - 실제 GameObject Block을 만들지 않고 int[,] 가상 보드로만 처리.
    /// - StageData를 받아 N회 반복 시뮬레이션 후 clearRate/난이도/추천을 보고.
    /// - 원본 StageData asset과 PlayerPrefs 진행도는 절대 변경하지 않는다.
    /// - 좌표계: y=0 아래쪽, y=height-1 위쪽. 낙하는 y가 작은 방향(아래쪽)으로 떨어진다.
    /// - 전략 v1: 가능한 스왑 중 expectedMatchCount가 최대인 후보 선택, tie는 무작위.
    /// TODO: Connect no-move case to ShuffleAgent.
    /// TODO: Implement ClearObstacle goal simulation.
    /// TODO: Export simulation report to CSV.
    /// TODO: Replace v1 greedy strategy with cascade-aware search.
    /// </summary>
    public class VirtualPlaySimulator
    {
        public const int DefaultMaxCascadeCount = 12;
        public const int EmptyCell = -1;
        public const int FallbackBoardSize = 6;
        public const int InitialMatchRetryLimit = 16;

        private int maxCascadeCount = DefaultMaxCascadeCount;
        private System.Random rng;
        private int[] availableTypes;

        public VirtualPlaySimulator(int seed = 0)
        {
            rng = seed != 0 ? new System.Random(seed) : new System.Random();
        }

        public void SetMaxCascadeCount(int v)
        {
            if (v > 0) maxCascadeCount = v;
        }

        // ───────── Entry Points ─────────

        public VirtualStageReport SimulateStage(StageData stageData, int simulationCount)
        {
            VirtualStageReport report = new VirtualStageReport
            {
                stageId = stageData != null ? stageData.StageId : -1,
                stageName = stageData != null ? stageData.StageName : "(null)",
                simulations = Mathf.Max(1, simulationCount)
            };
            if (stageData == null)
            {
                report.recommendation = "StageData is null. Cannot simulate.";
                report.grade = "Invalid";
                return report;
            }
            if (!stageData.IsValid())
            {
                report.recommendation = "StageData failed IsValid(). Check moveLimit/targetScore/etc.";
                report.grade = "Invalid";
                return report;
            }

            PrepareAvailableTypes(stageData);

            int clearCount = 0;
            int sumRemainingMoves = 0;
            long sumFinalScore = 0;
            long sumCollected = 0;
            int sumMaxCascade = 0;

            for (int i = 0; i < report.simulations; i++)
            {
                VirtualPlayResult r = SimulateSingleRun(stageData);
                if (r.cleared) clearCount++;
                sumRemainingMoves += r.remainingMoves;
                sumFinalScore += r.finalScore;
                sumCollected += r.collectedCount;
                sumMaxCascade += r.maxCascadeReached;
                if (!r.cleared && !string.IsNullOrEmpty(r.failReason))
                {
                    if (!report.failReasonCounts.ContainsKey(r.failReason)) report.failReasonCounts[r.failReason] = 0;
                    report.failReasonCounts[r.failReason]++;
                }
            }

            report.clearCount = clearCount;
            report.clearRate = report.simulations > 0 ? (float)clearCount / report.simulations : 0f;
            report.avgRemainingMoves = (float)sumRemainingMoves / report.simulations;
            report.avgFinalScore = (float)sumFinalScore / report.simulations;
            report.avgCollectedCount = (float)sumCollected / report.simulations;
            report.avgMaxCascade = (float)sumMaxCascade / report.simulations;
            report.grade = GradeFromClearRate(report.clearRate);
            report.recommendation = BuildRecommendation(report);
            return report;
        }

        public List<VirtualStageReport> SimulateAllStages(StagePackDatabase database, int simulationCount)
        {
            List<VirtualStageReport> all = new List<VirtualStageReport>();
            if (database == null) return all;
            for (int p = 0; p < database.StagePacks.Count; p++)
            {
                StagePackData pack = database.StagePacks[p];
                if (pack == null || pack.Stages == null) continue;
                for (int s = 0; s < pack.Stages.Count; s++)
                {
                    StageData st = pack.Stages[s];
                    if (st == null) continue;
                    all.Add(SimulateStage(st, simulationCount));
                }
            }
            return all;
        }

        // ───────── Core Loop ─────────

        private VirtualPlayResult SimulateSingleRun(StageData stage)
        {
            VirtualPlayResult result = new VirtualPlayResult();
            int width  = stage.BoardWidth  > 0 ? stage.BoardWidth  : FallbackBoardSize;
            int height = stage.BoardHeight > 0 ? stage.BoardHeight : FallbackBoardSize;
            int[,] board = BuildInitialBoard(width, height);
            int moveLimit = Mathf.Max(1, stage.MoveLimit);
            int targetScore = Mathf.Max(0, stage.TargetScore);
            int targetBlockCount = Mathf.Max(0, stage.TargetBlockCount);
            int targetType = (int)stage.TargetBlockType;
            StageGoalType goalType = stage.GoalType;

            int score = 0;
            int collected = 0;
            int maxCascade = 0;
            int movesUsed = 0;

            for (int move = 0; move < moveLimit; move++)
            {
                if (IsGoalAchieved(goalType, score, targetScore, collected, targetBlockCount))
                {
                    result.cleared = true;
                    break;
                }
                List<VirtualMove> moves = FindPossibleMoves(board, width, height);
                if (moves.Count == 0)
                {
                    result.failReason = "No possible moves";
                    // TODO: Connect to ShuffleAgent (v1: simply fail).
                    break;
                }
                VirtualMove best = PickBestMove(moves);
                Swap(board, best.from, best.to);
                movesUsed++;

                int cascadeIndex = 0;
                while (true)
                {
                    HashSet<Vector2Int> matched = FindMatches(board, width, height);
                    if (matched.Count == 0) break;

                    int removedCount = matched.Count;
                    int removedTargetType = 0;
                    foreach (Vector2Int p in matched)
                    {
                        if (goalType == StageGoalType.CollectBlock && board[p.x, p.y] == targetType)
                        {
                            removedTargetType++;
                        }
                        board[p.x, p.y] = EmptyCell;
                    }
                    score += CalculateScore(removedCount, cascadeIndex);
                    collected += removedTargetType;
                    if (cascadeIndex > maxCascade) maxCascade = cascadeIndex;

                    Drop(board, width, height);
                    FillEmpty(board, width, height);

                    cascadeIndex++;
                    if (cascadeIndex >= maxCascadeCount)
                    {
                        Debug.LogWarning($"VirtualPlaySimulator: maxCascadeCount({maxCascadeCount}) hit on stage {stage.StageId}.");
                        break;
                    }
                }

                if (IsGoalAchieved(goalType, score, targetScore, collected, targetBlockCount))
                {
                    result.cleared = true;
                    break;
                }
            }

            result.movesUsed = movesUsed;
            result.remainingMoves = Mathf.Max(0, moveLimit - movesUsed);
            result.finalScore = score;
            result.collectedCount = collected;
            result.maxCascadeReached = maxCascade;
            if (!result.cleared && string.IsNullOrEmpty(result.failReason))
            {
                result.failReason = goalType == StageGoalType.ClearObstacle ? "ClearObstacle goal not implemented" : "Goal not reached within move limit";
            }
            return result;
        }

        // ───────── Goal ─────────

        private static bool IsGoalAchieved(StageGoalType type, int score, int targetScore, int collected, int targetBlockCount)
        {
            switch (type)
            {
                case StageGoalType.Score:        return targetScore > 0 && score >= targetScore;
                case StageGoalType.CollectBlock: return targetBlockCount > 0 && collected >= targetBlockCount;
                case StageGoalType.ClearObstacle: return false; // TODO
                default: return false;
            }
        }

        private static int CalculateScore(int removed, int cascadeIndex)
        {
            float multiplier = 1f + cascadeIndex * 0.5f;
            return Mathf.RoundToInt(removed * 10 * multiplier);
        }

        // ───────── Board Building ─────────

        private void PrepareAvailableTypes(StageData stage)
        {
            List<int> types = new List<int>();
            if (stage.AvailableBlockTypes != null)
            {
                for (int i = 0; i < stage.AvailableBlockTypes.Count; i++)
                {
                    BlockType bt = stage.AvailableBlockTypes[i];
                    if (bt == BlockType.Empty || bt == BlockType.Noise) continue;
                    types.Add((int)bt);
                }
            }
            if (types.Count == 0)
            {
                types.Add((int)BlockType.DreamBubble);
                types.Add((int)BlockType.MoonRiceCake);
                types.Add((int)BlockType.InkStar);
                types.Add((int)BlockType.WaveCloud);
            }
            availableTypes = types.ToArray();
        }

        private int[,] BuildInitialBoard(int width, int height)
        {
            int[,] board = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    board[x, y] = GetRandomTypeAvoidingInitialMatch(board, x, y);
                }
            }
            return board;
        }

        private int GetRandomTypeAvoidingInitialMatch(int[,] board, int x, int y)
        {
            for (int attempt = 0; attempt < InitialMatchRetryLimit; attempt++)
            {
                int t = availableTypes[rng.Next(availableTypes.Length)];
                // 가로 직전 2칸과 같으면 회피
                if (x >= 2 && board[x - 1, y] == t && board[x - 2, y] == t) continue;
                // 세로 직전 2칸과 같으면 회피
                if (y >= 2 && board[x, y - 1] == t && board[x, y - 2] == t) continue;
                return t;
            }
            return availableTypes[rng.Next(availableTypes.Length)];
        }

        // ───────── Move Search ─────────

        private List<VirtualMove> FindPossibleMoves(int[,] board, int width, int height)
        {
            List<VirtualMove> list = new List<VirtualMove>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y] == EmptyCell) continue;
                    if (x + 1 < width)
                    {
                        TryAddMove(board, width, height, new Vector2Int(x, y), new Vector2Int(x + 1, y), list);
                    }
                    if (y + 1 < height)
                    {
                        TryAddMove(board, width, height, new Vector2Int(x, y), new Vector2Int(x, y + 1), list);
                    }
                }
            }
            return list;
        }

        private static void TryAddMove(int[,] board, int width, int height, Vector2Int a, Vector2Int b, List<VirtualMove> outList)
        {
            int va = board[a.x, a.y];
            int vb = board[b.x, b.y];
            if (va == EmptyCell || vb == EmptyCell) return;
            // 임시 swap
            board[a.x, a.y] = vb;
            board[b.x, b.y] = va;
            HashSet<Vector2Int> matched = FindMatches(board, width, height);
            board[a.x, a.y] = va;
            board[b.x, b.y] = vb;
            if (matched.Count > 0)
            {
                outList.Add(new VirtualMove
                {
                    from = a,
                    to = b,
                    expectedMatchCount = matched.Count,
                    score = matched.Count * 10
                });
            }
        }

        private VirtualMove PickBestMove(List<VirtualMove> moves)
        {
            int best = 0;
            for (int i = 1; i < moves.Count; i++)
            {
                if (moves[i].expectedMatchCount > moves[best].expectedMatchCount) best = i;
            }
            // tie 후보 중 랜덤
            List<int> ties = new List<int>();
            int bestCount = moves[best].expectedMatchCount;
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].expectedMatchCount == bestCount) ties.Add(i);
            }
            return moves[ties[rng.Next(ties.Count)]];
        }

        private static void Swap(int[,] board, Vector2Int a, Vector2Int b)
        {
            int tmp = board[a.x, a.y];
            board[a.x, a.y] = board[b.x, b.y];
            board[b.x, b.y] = tmp;
        }

        // ───────── Matches ─────────

        private static HashSet<Vector2Int> FindMatches(int[,] board, int width, int height)
        {
            HashSet<Vector2Int> matched = new HashSet<Vector2Int>();
            // 가로
            for (int y = 0; y < height; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= width; x++)
                {
                    bool sameAsPrev = x < width
                                      && board[x, y] != EmptyCell
                                      && board[x, y] == board[runStart, y];
                    if (!sameAsPrev)
                    {
                        int runLen = x - runStart;
                        if (runLen >= 3 && board[runStart, y] != EmptyCell)
                        {
                            for (int k = runStart; k < x; k++) matched.Add(new Vector2Int(k, y));
                        }
                        runStart = x;
                    }
                }
            }
            // 세로
            for (int x = 0; x < width; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= height; y++)
                {
                    bool sameAsPrev = y < height
                                      && board[x, y] != EmptyCell
                                      && board[x, y] == board[x, runStart];
                    if (!sameAsPrev)
                    {
                        int runLen = y - runStart;
                        if (runLen >= 3 && board[x, runStart] != EmptyCell)
                        {
                            for (int k = runStart; k < y; k++) matched.Add(new Vector2Int(x, k));
                        }
                        runStart = y;
                    }
                }
            }
            return matched;
        }

        // ───────── Drop & Fill ─────────

        /// <summary>y=0이 아래쪽. 빈칸은 위로 떠 있고 블록은 아래로 떨어진다.</summary>
        private static void Drop(int[,] board, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y] != EmptyCell)
                    {
                        if (writeY != y)
                        {
                            board[x, writeY] = board[x, y];
                            board[x, y] = EmptyCell;
                        }
                        writeY++;
                    }
                }
            }
        }

        private void FillEmpty(int[,] board, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y] == EmptyCell)
                    {
                        board[x, y] = availableTypes[rng.Next(availableTypes.Length)];
                    }
                }
            }
        }

        // ───────── Grading & Recommendation ─────────

        private static string GradeFromClearRate(float r)
        {
            if (r >= 0.85f) return "Easy";
            if (r >= 0.55f) return "Normal";
            if (r >= 0.30f) return "Hard";
            return "TooHard";
        }

        private static string BuildRecommendation(VirtualStageReport report)
        {
            if (report.clearRate < 0.30f)
                return "이동 횟수 +2 또는 목표 점수 10% 완화를 권장합니다.";
            if (report.clearRate > 0.90f)
                return "너무 쉬울 수 있습니다. 목표 상향 또는 이동 횟수 -1을 검토하세요.";
            if (report.clearRate < 0.55f)
                return "조금 어렵습니다. 이동 +1 또는 목표 소폭 완화를 검토하세요.";
            return "현재 난이도는 참고상 적정합니다.";
        }
    }
}
