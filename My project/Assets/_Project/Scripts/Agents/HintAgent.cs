using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;

namespace NabyeolDabyeolDreamPuzzle.Agents
{
    /// <summary>
    /// 힌트 에이전트 v1.
    /// - 현재 보드 상태를 읽기만 해서 한 번 스왑으로 매치가 생기는 후보를 찾는다.
    /// - 실제 블록 Transform/배열을 영구히 바꾸지 않는다. WouldCreateMatch가 임시 스왑 후 원상복구를 책임진다.
    /// - 점수/이동수/목표 진행도/상태 플래그는 일절 건드리지 않는다.
    /// - 기존 나별/포포링 힌트 스킬과 BoardManager.TryFindHintMove 경로를 공유한다.
    /// TODO: Refactor NabyeolHint and PoporingBubbleHint to reuse HintAgent unified entry points.
    /// TODO: Auto-hint after N seconds of no input.
    /// TODO: Connect no-move case to shuffle agent.
    /// </summary>
    public class HintAgent : MonoBehaviour
    {
        /// <summary>52번에서 사용 중인 단순 결과 구조. 보드 객체 참조를 그대로 반환.</summary>
        public struct HintResult
        {
            public bool hasHint;
            public Block firstBlock;
            public Block secondBlock;
            public int expectedMatchCount;

            public static HintResult None => new HintResult();

            public HintResult(Block a, Block b, int n)
            {
                firstBlock = a;
                secondBlock = b;
                expectedMatchCount = n;
                hasHint = a != null && b != null && n > 0;
            }
        }

        /// <summary>
        /// v1 에이전트가 외부(BoardManager.ShowAgentHint)와 주고받는 좌표 기반 후보 구조.
        /// from/to는 (x=column, y=row) 컨벤션. Block 직접 참조 대신 좌표로 표현해 후보 직렬화/우선순위 정렬에 유리.
        /// </summary>
        public struct HintMove
        {
            public Vector2Int from;
            public Vector2Int to;
            public int expectedMatchCount;
            public int priorityScore;

            public bool IsValid => expectedMatchCount > 0;
        }

        [Header("Dependencies")]
        [SerializeField] private MatchFinder matchFinder;
        [Tooltip("v1 진입점(TryFindBestHint/ShowHint)에서 사용. 비워두면 같은 GameObject에서 검색.")]
        [SerializeField] private BoardManager boardManager;

        [Header("Search")]
        [SerializeField, Min(1)] private int maxCheckedSwaps = 200;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void EnsureDependencies()
        {
            if (matchFinder == null) matchFinder = GetComponent<MatchFinder>();
            if (matchFinder == null) matchFinder = gameObject.AddComponent<MatchFinder>();
            if (boardManager == null) boardManager = GetComponent<BoardManager>();
            if (boardManager == null) boardManager = FindAnyObjectByType<BoardManager>();
        }

        // ───────── 기존 v0 API (52번 나별/56번 포포링 호환) ─────────

        public HintResult FindHint(Block[,] blocks, int rows, int columns)
        {
            List<HintResult> all = FindAllHints(blocks, rows, columns);
            return all.Count > 0 ? all[0] : HintResult.None;
        }

        public List<HintResult> FindAllHints(Block[,] blocks, int rows, int columns)
        {
            EnsureDependencies();
            List<HintResult> list = new List<HintResult>();
            if (blocks == null || matchFinder == null) return list;

            int checkedSwaps = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (checkedSwaps >= maxCheckedSwaps) return list;
                    Block cur = blocks[r, c];
                    if (!IsHintable(cur)) continue;

                    if (c + 1 < columns)
                    {
                        checkedSwaps++;
                        HintResult h = EvalSwapBlocks(blocks, rows, columns, cur, blocks[r, c + 1]);
                        if (h.hasHint) list.Add(h);
                    }
                    if (r + 1 < rows)
                    {
                        checkedSwaps++;
                        HintResult h = EvalSwapBlocks(blocks, rows, columns, cur, blocks[r + 1, c]);
                        if (h.hasHint) list.Add(h);
                    }
                }
            }
            return list;
        }

        public bool HasAvailableMove(Block[,] blocks, int rows, int columns)
        {
            return FindHint(blocks, rows, columns).hasHint;
        }

        // ───────── v1 API (Agent 진입점) ─────────

        /// <summary>
        /// 현재 보드에서 가장 좋은 한 수를 찾는다.
        /// 우선순위: expectedMatchCount * 100 + from.y → 매치 개수 많을수록, 동률이면 row 큰(아래쪽) 후보 우선.
        /// 임시 스왑은 BoardManager.WouldCreateMatch가 책임지며 보드 배열은 즉시 원상복구된다.
        /// </summary>
        public bool TryFindBestHint(out HintMove bestHint)
        {
            bestHint = default;
            EnsureDependencies();
            if (boardManager == null)
            {
                Debug.LogWarning("HintAgent: boardManager not assigned.");
                return false;
            }
            Block[,] blocks = boardManager.Blocks;
            if (blocks == null)
            {
                Debug.LogWarning("HintAgent: boardManager.Blocks is null.");
                return false;
            }

            int rows = blocks.GetLength(0);
            int columns = blocks.GetLength(1);

            List<HintMove> candidates = new List<HintMove>();
            int checkedSwaps = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    if (checkedSwaps >= maxCheckedSwaps) break;
                    Block cur = blocks[r, c];
                    if (!IsHintable(cur)) continue;

                    if (c + 1 < columns)
                    {
                        checkedSwaps++;
                        if (TryEvalSwapCoord(new Vector2Int(c, r), new Vector2Int(c + 1, r), out HintMove m1))
                            candidates.Add(m1);
                    }
                    if (r + 1 < rows)
                    {
                        checkedSwaps++;
                        if (TryEvalSwapCoord(new Vector2Int(c, r), new Vector2Int(c, r + 1), out HintMove m2))
                            candidates.Add(m2);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                Debug.Log("HintAgent: no possible moves found.");
                return false;
            }

            // priorityScore 내림차순. 동률이면 먼저 찾은 후보 사용 (Sort는 stable하지 않으나 priority가 동률이면 의도 동일).
            candidates.Sort((a, b) => b.priorityScore.CompareTo(a.priorityScore));
            bestHint = candidates[0];
            Debug.Log($"HintAgent: {candidates.Count} candidates. Best ({bestHint.from})↔({bestHint.to}), matchCount={bestHint.expectedMatchCount}, priority={bestHint.priorityScore}.");
            return true;
        }

        /// <summary>
        /// 외부에서 호출하는 메인 진입점. CanUseHintAgent 가드 후 후보 탐색 → BoardManager에 반짝 표시 요청.
        /// 자동 플레이가 아니라 시각 표시만 한다.
        /// </summary>
        public bool ShowHint()
        {
            EnsureDependencies();
            if (boardManager == null)
            {
                Debug.LogWarning("HintAgent: boardManager not assigned.");
                return false;
            }
            if (!boardManager.CanUseHintAgent())
            {
                Debug.Log("HintAgent: cannot use agent hint right now (board busy / cleared / failed / another skill).");
                return false;
            }
            if (!TryFindBestHint(out HintMove best))
            {
                return false;
            }
            return boardManager.ShowAgentHint(best);
        }

        [ContextMenu("Debug Show Agent Hint")]
        public void DebugShowAgentHint()
        {
            ShowHint();
        }

        // ───────── 내부 ─────────

        private bool TryEvalSwapCoord(Vector2Int from, Vector2Int to, out HintMove move)
        {
            move = default;
            if (boardManager == null) return false;
            if (!boardManager.WouldCreateMatch(from, to, out int matchCount)) return false;
            if (matchCount <= 0) return false;
            move = new HintMove
            {
                from = from,
                to = to,
                expectedMatchCount = matchCount,
                // 명세 #9: priorityScore = expectedMatchCount * 100 + from.y
                priorityScore = matchCount * 100 + from.y
            };
            return true;
        }

        private HintResult EvalSwapBlocks(Block[,] blocks, int rows, int cols, Block a, Block b)
        {
            if (!IsHintable(a) || !IsHintable(b) || a == b || !IsAdjacent(a, b)) return HintResult.None;
            int ar = a.Row, ac = a.Column, br = b.Row, bc = b.Column;
            blocks[ar, ac] = b;
            blocks[br, bc] = a;
            List<Block> matches = matchFinder.FindMatches(blocks, rows, cols);
            blocks[ar, ac] = a;
            blocks[br, bc] = b;
            int count = matches != null ? matches.Count : 0;
            return count > 0 ? new HintResult(a, b, count) : HintResult.None;
        }

        private static bool IsAdjacent(Block a, Block b)
        {
            return Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Column - b.Column) == 1;
        }

        private static bool IsHintable(Block b)
        {
            return b != null && !b.IsEmpty && !b.IsNoise;
        }
    }
}
