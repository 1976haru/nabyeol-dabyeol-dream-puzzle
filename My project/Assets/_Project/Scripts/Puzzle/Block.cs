using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 퍼즐판의 한 칸에 놓이는 단일 블록의 데이터 상태를 관리하는 컴포넌트.
    /// 블록 타입, 보드 좌표(row, column), 선택 상태만 보유한다.
    /// 시각 표현, 입력 처리, 교환/매치/낙하/점수 로직은 별도 컴포넌트에서 다룬다.
    /// </summary>
    public class Block : MonoBehaviour
    {
        [SerializeField] private BlockType type = BlockType.Empty;
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private bool isSelected;

        /// <summary>현재 블록 타입.</summary>
        public BlockType Type => type;

        /// <summary>보드 좌표의 행(row).</summary>
        public int Row => row;

        /// <summary>보드 좌표의 열(column).</summary>
        public int Column => column;

        /// <summary>선택 상태 여부.</summary>
        public bool IsSelected => isSelected;

        /// <summary>비어 있는 칸인지 여부.</summary>
        public bool IsEmpty => type == BlockType.Empty;

        /// <summary>장난구름/방해 블록인지 여부.</summary>
        public bool IsNoise => type == BlockType.Noise;

        /// <summary>매치 판정 대상이 될 수 있는 블록인지 여부 (Empty와 Noise 제외).</summary>
        public bool IsMatchable => !IsEmpty && !IsNoise;

        /// <summary>
        /// 블록을 초기화한다. 타입, 좌표를 설정하고 선택 상태는 해제한다.
        /// </summary>
        public void Initialize(BlockType type, int row, int column)
        {
            SetType(type);
            SetGridPosition(row, column);
            SetSelected(false);
        }

        /// <summary>
        /// 블록 타입을 설정한다.
        /// </summary>
        public void SetType(BlockType type)
        {
            this.type = type;
        }

        /// <summary>
        /// 보드 좌표를 설정한다.
        /// </summary>
        public void SetGridPosition(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        /// <summary>
        /// 선택 상태를 설정한다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
        }

        /// <summary>
        /// 다른 블록과 같은 타입인지 확인한다.
        /// other가 null이거나, 둘 중 하나가 Empty/Noise인 경우 false.
        /// </summary>
        public bool HasSameType(Block other)
        {
            if (other == null)
            {
                return false;
            }
            if (!IsMatchable || !other.IsMatchable)
            {
                return false;
            }
            return type == other.Type;
        }

        /// <summary>
        /// 디버깅용 문자열을 반환한다.
        /// 예: Block(row=0, column=1, type=DreamBubble, selected=False)
        /// </summary>
        public string GetDebugLabel()
        {
            return $"Block(row={row}, column={column}, type={type}, selected={isSelected})";
        }
    }
}
