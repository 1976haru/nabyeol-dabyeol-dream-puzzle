using System.Collections;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 두 블록의 배열 위치, 그리드 좌표(Row/Column), 월드 위치를 서로 바꾸는 컴포넌트.
    /// 즉시 교환(SwapInstant)과 짧은 보간 애니메이션 교환(SwapRoutine)을 제공한다.
    /// 인접 여부 판단·매치 판정·점수·이동수 처리에는 관여하지 않는다.
    /// </summary>
    public class BlockSwapper : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float swapDuration = 0.15f;

        /// <summary>한 번의 교환 애니메이션이 진행되는 시간(초).</summary>
        public float SwapDuration => swapDuration;

        /// <summary>
        /// 두 블록을 즉시 교환한다. 배열 위치, Block.Row/Column, transform.position을 모두 맞바꾼다.
        /// </summary>
        public bool SwapInstant(Block first, Block second, Block[,] blocks)
        {
            if (!CanSwap(first, second, blocks))
            {
                return false;
            }

            int firstRow = first.Row;
            int firstColumn = first.Column;
            int secondRow = second.Row;
            int secondColumn = second.Column;

            Vector3 firstPosition = first.transform.position;
            Vector3 secondPosition = second.transform.position;

            blocks[firstRow, firstColumn] = second;
            blocks[secondRow, secondColumn] = first;

            first.SetGridPosition(secondRow, secondColumn);
            second.SetGridPosition(firstRow, firstColumn);

            first.transform.position = secondPosition;
            second.transform.position = firstPosition;

            return true;
        }

        /// <summary>
        /// 두 블록을 swapDuration 동안 보간 이동으로 교환한다.
        /// 배열과 좌표는 즉시 교환하고, 월드 위치만 시간에 따라 부드럽게 이동한다.
        /// </summary>
        public IEnumerator SwapRoutine(Block first, Block second, Block[,] blocks)
        {
            if (!CanSwap(first, second, blocks))
            {
                yield break;
            }

            int firstRow = first.Row;
            int firstColumn = first.Column;
            int secondRow = second.Row;
            int secondColumn = second.Column;

            Vector3 firstStart = first.transform.position;
            Vector3 secondStart = second.transform.position;

            blocks[firstRow, firstColumn] = second;
            blocks[secondRow, secondColumn] = first;

            first.SetGridPosition(secondRow, secondColumn);
            second.SetGridPosition(firstRow, firstColumn);

            float duration = Mathf.Max(0.0001f, swapDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (first != null)
                {
                    first.transform.position = Vector3.Lerp(firstStart, secondStart, t);
                }
                if (second != null)
                {
                    second.transform.position = Vector3.Lerp(secondStart, firstStart, t);
                }

                yield return null;
            }

            if (first != null)
            {
                first.transform.position = secondStart;
            }
            if (second != null)
            {
                second.transform.position = firstStart;
            }
        }

        /// <summary>
        /// 교환이 안전하게 수행될 수 있는지 검사한다(null/동일 객체/배열 범위).
        /// 인접 여부는 검사하지 않는다.
        /// </summary>
        public bool CanSwap(Block first, Block second, Block[,] blocks)
        {
            if (first == null || second == null)
            {
                return false;
            }
            if (first == second)
            {
                return false;
            }
            if (blocks == null)
            {
                return false;
            }
            if (!IsInsideArray(first, blocks))
            {
                return false;
            }
            if (!IsInsideArray(second, blocks))
            {
                return false;
            }
            return true;
        }

        private bool IsInsideArray(Block block, Block[,] blocks)
        {
            if (block == null || blocks == null)
            {
                return false;
            }
            int rowCount = blocks.GetLength(0);
            int columnCount = blocks.GetLength(1);
            return block.Row >= 0 && block.Row < rowCount &&
                   block.Column >= 0 && block.Column < columnCount;
        }
    }
}
