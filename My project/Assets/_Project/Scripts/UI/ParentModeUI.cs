using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 부모 모드 UI 컨트롤러. 보호자 확인 패널과 부모 모드 패널 두 단계 흐름을 관리.
    /// OpenParentCheck → 문제 표시 → 답 입력 → SubmitParentCheck → 정답 시 EnterParentMode → 부모 모드 패널 표시.
    /// </summary>
    public class ParentModeUI : MonoBehaviour
    {
        [Header("Parent Check Panel (보호자 확인)")]
        [SerializeField] private GameObject parentCheckPanel;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TMP_InputField answerInput;
        [SerializeField] private Button submitAnswerButton;
        [SerializeField] private Button cancelCheckButton;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Parent Mode Panel (부모 모드 메뉴)")]
        [SerializeField] private GameObject parentModePanel;
        [SerializeField] private Button exitParentModeButton;
        [SerializeField] private TextMeshProUGUI parentModeTitleText;
        [SerializeField] private TextMeshProUGUI parentModeNoticeText;

        [Header("Parent Mode Hub (선택)")]
        [Tooltip("Assign to route successful authentication into the parent-mode hub. " +
                 "If null, the legacy parentModePanel is opened instead.")]
        [SerializeField] private ParentModeHubUI parentModeHubUI;

        [Header("Messages")]
        [SerializeField] private string emptyInputMessage = "숫자를 입력해 주세요.";
        [SerializeField] private string nonNumberMessage = "숫자로 입력해 주세요.";
        [SerializeField] private string wrongAnswerMessage = "정답이 아니에요. 보호자와 함께 다시 해주세요.";
        [SerializeField] private string parentModeTitle = "부모 모드";
        [SerializeField] private string parentModeNotice = "고급 설정은 보호자만 사용할 수 있어요.";

        private ParentModeManager.ParentCheckQuestion currentQuestion;

        private void Awake()
        {
            // 초기 상태: 두 패널 모두 숨김
            if (parentCheckPanel != null) parentCheckPanel.SetActive(false);
            if (parentModePanel != null) parentModePanel.SetActive(false);

            if (submitAnswerButton != null)
            {
                submitAnswerButton.onClick.RemoveListener(SubmitParentCheck);
                submitAnswerButton.onClick.AddListener(SubmitParentCheck);
            }
            if (cancelCheckButton != null)
            {
                cancelCheckButton.onClick.RemoveListener(CancelParentCheck);
                cancelCheckButton.onClick.AddListener(CancelParentCheck);
            }
            if (exitParentModeButton != null)
            {
                exitParentModeButton.onClick.RemoveListener(HandleExitButton);
                exitParentModeButton.onClick.AddListener(HandleExitButton);
            }

            if (answerInput != null)
            {
                answerInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            }
            if (parentModeTitleText != null) parentModeTitleText.text = parentModeTitle;
            if (parentModeNoticeText != null) parentModeNoticeText.text = parentModeNotice;
        }

        private void OnEnable()
        {
            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.OnExitParentMode += HandleParentModeExited;
            }
        }

        private void OnDisable()
        {
            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.OnExitParentMode -= HandleParentModeExited;
            }
        }

        private void OnDestroy()
        {
            if (submitAnswerButton != null) submitAnswerButton.onClick.RemoveListener(SubmitParentCheck);
            if (cancelCheckButton != null) cancelCheckButton.onClick.RemoveListener(CancelParentCheck);
            if (exitParentModeButton != null) exitParentModeButton.onClick.RemoveListener(HandleExitButton);
        }

        /// <summary>외부 잠금 버튼이 호출. 보호자 확인 시작.</summary>
        public void OpenParentCheck()
        {
            ParentModeManager mgr = ParentModeManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("ParentModeUI: ParentModeManager.Instance not found.");
                return;
            }

            // 이미 부모 모드 활성이거나 bypass면 바로 허브(또는 레거시 패널)
            if (mgr.CanAccessParentOnlyMenu())
            {
                if (!mgr.IsParentModeActive) mgr.EnterParentMode();
                OpenAuthorizedSurface();
                return;
            }

            currentQuestion = mgr.GenerateParentCheckQuestion();
            if (questionText != null) questionText.text = currentQuestion.questionText;
            if (answerInput != null) answerInput.text = string.Empty;
            if (messageText != null) messageText.text = string.Empty;
            if (parentCheckPanel != null) parentCheckPanel.SetActive(true);
            Debug.Log("ParentModeUI: Parent check opened.");
        }

        public void CancelParentCheck()
        {
            if (parentCheckPanel != null) parentCheckPanel.SetActive(false);
            if (messageText != null) messageText.text = string.Empty;
            Debug.Log("ParentModeUI: Parent check cancelled.");
        }

        public void SubmitParentCheck()
        {
            ParentModeManager mgr = ParentModeManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("ParentModeUI: ParentModeManager.Instance not found.");
                return;
            }

            string text = answerInput != null ? answerInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowMessage(emptyInputMessage);
                return;
            }
            if (!int.TryParse(text, out int userAnswer))
            {
                ShowMessage(nonNumberMessage);
                return;
            }

            if (mgr.ValidateAnswer(userAnswer, currentQuestion.answer))
            {
                Debug.Log("ParentModeUI: Parent check passed.");
                if (messageText != null) messageText.text = string.Empty;
                mgr.EnterParentMode();
                if (parentCheckPanel != null) parentCheckPanel.SetActive(false);
                OpenAuthorizedSurface();
            }
            else
            {
                Debug.Log("ParentModeUI: Parent check failed.");
                ShowMessage(wrongAnswerMessage);
                // 새 문제로 갱신 (재시도를 막지는 않지만 같은 문제 반복 방지)
                currentQuestion = mgr.GenerateParentCheckQuestion();
                if (questionText != null) questionText.text = currentQuestion.questionText;
                if (answerInput != null) answerInput.text = string.Empty;
            }
        }

        private void OpenParentModePanel()
        {
            if (parentModePanel != null) parentModePanel.SetActive(true);
            Debug.Log("ParentModeUI: Parent mode panel opened.");
        }

        /// <summary>
        /// 인증 통과 후 표시할 화면을 결정. 허브가 연결돼 있으면 허브를 우선 사용하고,
        /// 그렇지 않으면 레거시 parentModePanel을 연다. (기존 동작 호환)
        /// </summary>
        private void OpenAuthorizedSurface()
        {
            if (parentModeHubUI != null)
            {
                parentModeHubUI.OpenParentModeHub();
                return;
            }
            OpenParentModePanel();
        }

        private void HandleExitButton()
        {
            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.ExitParentMode();
            }
            // OnExitParentMode 이벤트가 HandleParentModeExited로 콜백되어 패널이 닫힘
        }

        private void HandleParentModeExited()
        {
            if (parentModePanel != null) parentModePanel.SetActive(false);
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
