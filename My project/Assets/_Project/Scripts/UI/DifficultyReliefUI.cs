using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 반복 실패한 스테이지 시작 시 표시되는 부드러운 난이도 완화 안내.
    /// - "실패해서 낮춰줬다"가 아닌 "조금 더 도와줄게" 톤으로 작성.
    /// - reliefPanel/필드가 일부 비어 있어도 NullReferenceException 없이 안전 동작.
    /// </summary>
    public class DifficultyReliefUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject reliefPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI detailText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Strings")]
        [SerializeField] private string titleString = "조금 더 쉽게 도와줄게요";
        [TextArea(2, 4)]
        [SerializeField] private string messageWithMovesString = "이번 도전은 이동 횟수가 조금 늘어났어요.";
        [TextArea(2, 4)]
        [SerializeField] private string messageNeutralString = "이번 도전은 조금 더 쉽게 다시 해볼 수 있어요.";
        [SerializeField] private string extraMovesFormat = "추가 이동 횟수 +{0}";
        [SerializeField] private string targetScoreFormat = "목표 점수: {0}점";
        [SerializeField] private string targetBlockFormat = "목표 수집: {0}개";

        private void Awake()
        {
            if (reliefPanel != null) reliefPanel.SetActive(false);
            if (titleText != null) titleText.text = titleString;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
        }

        /// <summary>
        /// 완화가 적용된 스테이지 시작 시 호출. adjScore/adjBlock이 -1이면 해당 라인은 표시하지 않는다.
        /// </summary>
        public void ShowReliefInfo(int stageId, int extraMoves, int adjustedTargetScore, int adjustedTargetBlockCount)
        {
            if (reliefPanel != null) reliefPanel.SetActive(true);
            if (titleText != null) titleText.text = titleString;
            if (messageText != null)
            {
                messageText.text = extraMoves > 0 ? messageWithMovesString : messageNeutralString;
            }

            if (detailText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (extraMoves > 0)
                {
                    sb.AppendLine(string.Format(extraMovesFormat, extraMoves));
                }
                if (adjustedTargetScore > 0)
                {
                    sb.AppendLine(string.Format(targetScoreFormat, adjustedTargetScore));
                }
                if (adjustedTargetBlockCount > 0)
                {
                    sb.AppendLine(string.Format(targetBlockFormat, adjustedTargetBlockCount));
                }
                detailText.text = sb.ToString().TrimEnd();
            }
            Debug.Log($"DifficultyReliefUI: Shown stage={stageId}, extra=+{extraMoves}, adjScore={adjustedTargetScore}, adjBlock={adjustedTargetBlockCount}.");
        }

        public void ClosePanel()
        {
            if (reliefPanel != null) reliefPanel.SetActive(false);
        }
    }
}
