using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Agents;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 같은 스테이지 3회 연속 실패 시 표시되는 부드러운 도움 패널.
    /// - 실패 횟수 표시 + 격려 문구 + "힌트 보기" 버튼 + 닫기.
    /// - 힌트 보기 버튼은 즉시 힌트가 어려운 상태(FailPopup 위 등)에서는 자동으로 다음 시작 시 표시 예약.
    /// - assistPanel/필드가 일부 비어 있어도 NullReferenceException 없이 안전 동작.
    /// </summary>
    public class FailureAssistUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject assistPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI failCountText;

        [Header("Buttons")]
        [SerializeField] private Button showHintButton;
        [SerializeField] private Button closeButton;

        [Header("Strings")]
        [SerializeField] private string titleString = "도움이 필요할까요?";
        [TextArea(2, 4)]
        [SerializeField] private string messageString = "괜찮아. 이번엔 힌트를 살짝 보여줄게!";
        [TextArea(2, 4)]
        [SerializeField] private string subMessage = "같은 블록 3개를 만들 수 있는 곳을 찾아보자.";
        [SerializeField] private string failCountFormat = "이번 스테이지 도전 실패 {0}회";
        [SerializeField] private string hintShownMessage = "잘 봐! 반짝이는 두 개를 움직여 보자.";
        [SerializeField] private string hintReservedMessage = "다시 시작한 뒤 힌트를 보여줄게.";

        private int currentStageId;

        private void Awake()
        {
            if (assistPanel != null) assistPanel.SetActive(false);
            if (titleText != null) titleText.text = titleString;

            if (showHintButton != null)
            {
                showHintButton.onClick.RemoveListener(OnShowHintClicked);
                showHintButton.onClick.AddListener(OnShowHintClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDestroy()
        {
            if (showHintButton != null) showHintButton.onClick.RemoveListener(OnShowHintClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>실패 3회 이상 시 FailureAssistAgent가 호출. 패널을 보이고 문구/카운트를 채운다.</summary>
        public void ShowAssist(int stageId, int failCount)
        {
            currentStageId = stageId;
            if (assistPanel != null) assistPanel.SetActive(true);
            if (titleText != null) titleText.text = titleString;
            // 첫 표시는 기본 격려 메시지 + 서브 메시지 한 줄.
            if (messageText != null)
            {
                string combined = string.IsNullOrEmpty(subMessage)
                    ? messageString
                    : (messageString + "\n" + subMessage);
                messageText.text = combined;
            }
            if (failCountText != null) failCountText.text = string.Format(failCountFormat, failCount);
            Debug.Log($"FailureAssistUI: Assist shown for stage {stageId} (count={failCount}).");
        }

        public void ClosePanel()
        {
            if (assistPanel != null) assistPanel.SetActive(false);
        }

        private void OnShowHintClicked()
        {
            if (FailureAssistAgent.Instance == null)
            {
                Debug.LogWarning("FailureAssistUI: FailureAssistAgent.Instance not found.");
                ClosePanel();
                return;
            }
            bool shown = FailureAssistAgent.Instance.TryShowHintNow(currentStageId);
            if (messageText != null)
            {
                messageText.text = shown ? hintShownMessage : hintReservedMessage;
            }
            Debug.Log($"FailureAssistUI: Show hint clicked. immediate={shown}, stage={currentStageId}.");
            // 패널은 자동으로 닫지 않는다. 사용자가 메시지를 읽고 닫기 또는 다시 도전을 누르도록.
        }

        private void OnCloseClicked()
        {
            ClosePanel();
        }
    }
}
