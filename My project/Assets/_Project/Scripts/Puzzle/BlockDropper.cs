using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// blocks 배열에서 매치 제거로 생긴 null 빈칸을 채우기 위해
    /// 같은 column의 위쪽 블록을 아래로 떨어뜨리는 컴포넌트.
    /// row=0이 위쪽, row=rows-1이 아래쪽이라는 좌표 규칙을 따른다.
    /// 새 블록 생성, 매치 검사, 점수/이동 횟수 처리는 이 컴포넌트의 책임이 아니다.
    /// </summary>
    public class BlockDropper : MonoBehaviour
    {
        [Header("Drop Animation")]
        [SerializeField, Min(0.01f)] private float dropDuration = 0.2f;

        private int lastDroppedCount;

        /// <summary>가장 최근 낙하 호출에서 이동한 블록 수.</summary>
        public int LastDroppedCount => lastDroppedCount;

        private struct DropMove
        {
            public Block block;
            public Vector3 startPosition;
            public Vector3 targetPosition;
        }

        /// <summary>
        /// 즉시(보간 없이) 한 번에 모든 빈칸을 채우는 낙하를 수행한다.
        /// 배열, Block.Row/Column, transform.position을 모두 일관되게 갱신한다.
        /// 이동한 블록 수를 반환한다.
        /// </summary>
        public int DropBlocksInstant(Block[,] blocks, int rows, int columns, float cellSize, Transform boardRoot)
        {
            lastDroppedCount = 0;

            if (!ValidateArgs(blocks, rows, columns))
            {
                return 0;
            }

            int moved = 0;

            for (int column = 0; column < columns; column++)
            {
                for (int targetRow = rows - 1; targetRow >= 0; targetRow--)
                {
                    if (blocks[targetRow, column] != null)
                    {
                        continue;
                    }

                    int sourceRow = FindNearestBlockAbove(blocks, targetRow, column);
                    if (sourceRow < 0)
                    {
                        continue;
                    }

                    Block falling = blocks[sourceRow, column];
                    blocks[targetRow, column] = falling;
                    blocks[sourceRow, column] = null;
                    falling.SetGridPosition(targetRow, column);

                    Vector3 worldPosition = GetWorldPosition(targetRow, column, rows, columns, cellSize, boardRoot);
                    falling.transform.position = worldPosition;
                    moved++;
                }
            }

            lastDroppedCount = moved;
            return moved;
        }

        /// <summary>
        /// dropDuration 동안 모든 낙하 대상 블록의 transform.position을 동시에 Lerp 한다.
        /// 배열·Row/Column 갱신은 애니메이션 시작 전에 일괄 처리한다.
        /// </summary>
        public IEnumerator DropBlocksRoutine(Block[,] blocks, int rows, int columns, float cellSize, Transform boardRoot)
        {
            lastDroppedCount = 0;

            if (!ValidateArgs(blocks, rows, columns))
            {
                yield break;
            }

            List<DropMove> moves = new List<DropMove>();

            for (int column = 0; column < columns; column++)
            {
                for (int targetRow = rows - 1; targetRow >= 0; targetRow--)
                {
                    if (blocks[targetRow, column] != null)
                    {
                        continue;
                    }

                    int sourceRow = FindNearestBlockAbove(blocks, targetRow, column);
                    if (sourceRow < 0)
                    {
                        continue;
                    }

                    Block falling = blocks[sourceRow, column];
                    Vector3 startWorld = falling.transform.position;
                    Vector3 targetWorld = GetWorldPosition(targetRow, column, rows, columns, cellSize, boardRoot);

                    blocks[targetRow, column] = falling;
                    blocks[sourceRow, column] = null;
                    falling.SetGridPosition(targetRow, column);

                    moves.Add(new DropMove
                    {
                        block = falling,
                        startPosition = startWorld,
                        targetPosition = targetWorld
                    });
                }
            }

            lastDroppedCount = moves.Count;

            if (moves.Count == 0)
            {
                yield break;
            }

            float duration = Mathf.Max(0.0001f, dropDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < moves.Count; i++)
                {
                    DropMove m = moves[i];
                    if (m.block == null)
                    {
                        continue;
                    }
                    m.block.transform.position = Vector3.Lerp(m.startPosition, m.targetPosition, t);
                }

                yield return null;
            }

            for (int i = 0; i < moves.Count; i++)
            {
                DropMove m = moves[i];
                if (m.block == null)
                {
                    continue;
                }
                m.block.transform.position = m.targetPosition;
            }

            Debug.Log($"BlockDropper: Dropped {moves.Count} block(s).");
        }

        private bool ValidateArgs(Block[,] blocks, int rows, int columns)
        {
            if (blocks == null)
            {
                return false;
            }
            if (rows <= 0 || columns <= 0)
            {
                return false;
            }
            if (blocks.GetLength(0) < rows || blocks.GetLength(1) < columns)
            {
                return false;
            }
            return true;
        }

        private int FindNearestBlockAbove(Block[,] blocks, int targetRow, int column)
        {
            for (int r = targetRow - 1; r >= 0; r--)
            {
                if (blocks[r, column] != null)
                {
                    return r;
                }
            }
            return -1;
        }

        private Vector3 GetLocalPosition(int row, int column, int rows, int columns, float cellSize)
        {
            float startX = -(columns - 1) * cellSize * 0.5f;
            float startY = (rows - 1) * cellSize * 0.5f;

            float x = startX + column * cellSize;
            float y = startY - row * cellSize;

            return new Vector3(x, y, 0f);
        }

        private Vector3 GetWorldPosition(int row, int column, int rows, int columns, float cellSize, Transform boardRoot)
        {
            Vector3 local = GetLocalPosition(row, column, rows, columns, cellSize);

            if (boardRoot != null)
            {
                return boardRoot.TransformPoint(local);
            }

            return local;
        }
    }
}
