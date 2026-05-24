using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Puzzle
{
    /// <summary>
    /// 블록의 단일 클릭(또는 터치 탭) 입력을 BoardManager로 전달하는 컴포넌트.
    /// 드래그/스와이프는 후속 단계에서 추가한다.
    /// </summary>
    [RequireComponent(typeof(Block))]
    public class BlockInput : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        private Block block;

        private void Awake()
        {
            block = GetComponent<Block>();
            if (boardManager == null)
            {
                boardManager = Object.FindFirstObjectByType<BoardManager>();
            }
        }

        // OnMouseDown은 같은 GameObject에 Collider2D(또는 Collider)가 있어야 Unity가 호출한다.
        private void OnMouseDown()
        {
            if (block == null)
            {
                return;
            }
            if (block.IsEmpty)
            {
                return;
            }
            if (boardManager == null)
            {
                Debug.LogWarning("BlockInput: BoardManager reference is missing.");
                return;
            }
            boardManager.OnBlockClicked(block);
        }
    }
}
