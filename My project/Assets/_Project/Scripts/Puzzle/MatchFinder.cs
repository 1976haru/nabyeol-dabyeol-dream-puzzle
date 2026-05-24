using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 보드의 블록 배열에서 3매치 이상의 연속 구간을 탐지하는 컴포넌트.
    /// 가로 방향과 세로 방향을 모두 검사하며, 교차 지점의 블록은 HashSet으로 중복 제거된다.
    /// 보드 상태를 절대 변경하지 않고, 매치된 블록 목록만 반환한다.
    /// </summary>
    public class MatchFinder : MonoBehaviour
    {
        /// <summary>
        /// blocks 배열을 검사하여 3개 이상 연속된 동일 타입의 블록을 매치로 탐지한다.
        /// Empty, Noise, null 블록은 매치 대상에서 제외한다.
        /// 보드 상태는 변경하지 않고 매치된 Block 목록을 반환한다.
        /// </summary>
        public List<Block> FindMatches(Block[,] blocks, int rows, int columns)
        {
            if (blocks == null)
            {
                return new List<Block>();
            }
            if (rows < 3 || columns < 3)
            {
                return new List<Block>();
            }

            HashSet<Block> matchedSet = new HashSet<Block>();

            FindHorizontalMatches(blocks, rows, columns, matchedSet);
            FindVerticalMatches(blocks, rows, columns, matchedSet);

            return new List<Block>(matchedSet);
        }

        private void FindHorizontalMatches(Block[,] blocks, int rows, int columns, HashSet<Block> matchedSet)
        {
            for (int row = 0; row < rows; row++)
            {
                int startColumn = 0;
                while (startColumn < columns)
                {
                    Block startBlock = blocks[row, startColumn];
                    if (!IsMatchable(startBlock))
                    {
                        startColumn++;
                        continue;
                    }

                    int endColumn = startColumn + 1;
                    while (endColumn < columns && IsSameType(startBlock, blocks[row, endColumn]))
                    {
                        endColumn++;
                    }

                    int runLength = endColumn - startColumn;
                    if (runLength >= 3)
                    {
                        for (int i = startColumn; i < endColumn; i++)
                        {
                            Block b = blocks[row, i];
                            if (b != null)
                            {
                                matchedSet.Add(b);
                            }
                        }
                    }

                    startColumn = endColumn;
                }
            }
        }

        private void FindVerticalMatches(Block[,] blocks, int rows, int columns, HashSet<Block> matchedSet)
        {
            for (int column = 0; column < columns; column++)
            {
                int startRow = 0;
                while (startRow < rows)
                {
                    Block startBlock = blocks[startRow, column];
                    if (!IsMatchable(startBlock))
                    {
                        startRow++;
                        continue;
                    }

                    int endRow = startRow + 1;
                    while (endRow < rows && IsSameType(startBlock, blocks[endRow, column]))
                    {
                        endRow++;
                    }

                    int runLength = endRow - startRow;
                    if (runLength >= 3)
                    {
                        for (int i = startRow; i < endRow; i++)
                        {
                            Block b = blocks[i, column];
                            if (b != null)
                            {
                                matchedSet.Add(b);
                            }
                        }
                    }

                    startRow = endRow;
                }
            }
        }

        private bool IsMatchable(Block block)
        {
            return block != null && block.IsMatchable;
        }

        private bool IsSameType(Block a, Block b)
        {
            if (!IsMatchable(a) || !IsMatchable(b))
            {
                return false;
            }
            return a.Type == b.Type;
        }
    }
}
