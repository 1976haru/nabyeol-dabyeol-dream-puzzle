using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Customization;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 커스터마이징 기본값 복구 전 확인 팝업.
    /// - 부모 모드에서만 열린다.
    /// - 사용자가 "되돌리기"를 명시적으로 누른 경우에만 CustomizationResetManager가 실행된다.
    /// - 진행도 데이터는 절대 삭제되지 않음을 명확히 안내.
    /// </summary>
    public class ResetConfirmUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject confirmPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Parent Mode Link")]
        [SerializeField] private ParentModeUI parentModeUI;

        [Header("Strings")]
        [SerializeField] private string titleString = "기본값으로 되돌릴까요?";
        [TextArea(2, 4)]
        [SerializeField] private string messageString = "캐릭터 이름, 대표 대사, 바꾼 스토리 문장이 처음 상태로 돌아가요. 스테이지 진행도는 지워지지 않아요.";
        [SerializeField] private string confirmButtonLabel = "되돌리기";
        [SerializeField] private string cancelButtonLabel = "취소";
        [SerializeField] private string resultDoneMessage = "기본값으로 돌아갔어요.";
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";
        [SerializeField] private string resetManagerMissingMessage = "복구 매니저를 찾지 못했어요.";

        private void Awake()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);

            ApplyStaticTexts();

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
                confirmButton.onClick.AddListener(OnConfirmClicked);
                ApplyButtonLabel(confirmButton, confirmButtonLabel);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
                cancelButton.onClick.AddListener(OnCancelClicked);
                ApplyButtonLabel(cancelButton, cancelButtonLabel);
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        private void ApplyStaticTexts()
        {
            if (titleText != null) titleText.text = titleString ?? string.Empty;
            if (messageText != null) messageText.text = messageString ?? string.Empty;
            if (resultText != null) resultText.text = string.Empty;
            if (titleText == null) Debug.LogWarning("ResetConfirmUI: titleText is not assigned.");
            if (messageText == null) Debug.LogWarning("ResetConfirmUI: messageText is not assigned.");
        }

        private void ApplyButtonLabel(Button button, string label)
        {
            if (button == null) return;
            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = label ?? string.Empty;
        }

        /// <summary>외부(LockedMenuButton 등)에서 호출. 부모 모드 가드 후 팝업 표시.</summary>
        public void OpenResetConfirm()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("ResetConfirmUI: Parent mode required. Redirecting to parent check.");
                if (resultText != null) resultText.text = parentModeOnlyMessage;
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                return;
            }

            if (resultText != null) resultText.text = string.Empty;
            if (confirmPanel != null) confirmPanel.SetActive(true);
            Debug.Log("ResetConfirmUI: Confirm panel opened.");
        }

        public void CloseResetConfirm()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            Debug.Log("ResetConfirmUI: Confirm panel closed.");
        }

        private void OnConfirmClicked()
        {
            if (CustomizationResetManager.Instance == null)
            {
                Debug.LogWarning("ResetConfirmUI: CustomizationResetManager not found.");
                if (resultText != null) resultText.text = resetManagerMissingMessage;
                return;
            }

            bool ok = CustomizationResetManager.Instance.ResetCustomizationToDefaults();
            if (!ok)
            {
                Debug.LogWarning("ResetConfirmUI: Reset blocked by manager (likely parent mode lost mid-flow).");
                if (resultText != null) resultText.text = parentModeOnlyMessage;
                return;
            }
            Debug.Log("ResetConfirmUI: Confirm clicked. Reset executed.");
            if (resultText != null) resultText.text = resultDoneMessage;
            // 팝업은 결과 메시지 확인 후 사용자가 닫도록 유지. 자동 닫기를 원하면 CloseResetConfirm 호출.
        }

        private void OnCancelClicked()
        {
            Debug.Log("ResetConfirmUI: Cancel clicked. No data deleted.");
            CloseResetConfirm();
        }
    }
}
