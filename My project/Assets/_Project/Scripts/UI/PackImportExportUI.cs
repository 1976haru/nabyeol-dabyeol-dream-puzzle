using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Customization;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 커스터마이징 팩 내보내기/가져오기 UI.
    /// - 내보내기: Application.persistentDataPath에 JSON 저장 + 경로 메시지
    /// - 가져오기: 경로 입력 → 파일 존재 확인 → confirm 패널 → ImportCustomizationFromFile
    /// - 부모 모드에서만 동작.
    /// TODO: Add native file picker for import path selection.
    /// TODO: Add native share sheet for exported pack file.
    /// TODO: Show last exported file path / list of exported packs.
    /// </summary>
    public class PackImportExportUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button exportButton;
        [SerializeField] private Button importButton;
        [SerializeField] private TMP_InputField importPathInput;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;

        [Header("Import Confirm Panel")]
        [SerializeField] private GameObject confirmImportPanel;
        [SerializeField] private TextMeshProUGUI confirmTitleText;
        [SerializeField] private TextMeshProUGUI confirmMessageText;
        [SerializeField] private Button confirmImportButton;
        [SerializeField] private Button cancelImportButton;

        [Header("Parent Mode Link")]
        [SerializeField] private ParentModeUI parentModeUI;

        [Header("Strings")]
        [SerializeField] private string confirmTitleString = "팩을 가져올까요?";
        [TextArea(2, 4)]
        [SerializeField] private string confirmMessageString = "현재 캐릭터 이름, 대표 대사, 바꾼 스토리 문장이 가져온 팩 내용으로 바뀌어요. 스테이지 진행도는 지워지지 않아요.";
        [SerializeField] private string exportSuccessMessageFormat = "팩을 저장했어요.\n저장 위치: {0}";
        [SerializeField] private string exportFailedMessage = "팩 저장에 실패했어요. 부모 모드인지 확인해 주세요.";
        [SerializeField] private string importNoPathMessage = "가져올 파일 경로를 입력해 주세요.";
        [SerializeField] private string importFileNotFoundMessageFormat = "파일을 찾지 못했어요: {0}";
        [SerializeField] private string importSuccessMessage = "팩을 가져왔어요.";
        [SerializeField] private string importFailedMessage = "팩을 가져오지 못했어요. 파일 형식을 확인해 주세요.";
        [SerializeField] private string parentModeOnlyMessage = "보호자 확인이 필요해요.";

        private string pendingImportPath = string.Empty;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (confirmImportPanel != null) confirmImportPanel.SetActive(false);

            if (confirmTitleText != null) confirmTitleText.text = confirmTitleString;
            if (confirmMessageText != null) confirmMessageText.text = confirmMessageString;

            if (exportButton != null)
            {
                exportButton.onClick.RemoveListener(OnExportClicked);
                exportButton.onClick.AddListener(OnExportClicked);
            }
            if (importButton != null)
            {
                importButton.onClick.RemoveListener(OnImportClicked);
                importButton.onClick.AddListener(OnImportClicked);
            }
            if (confirmImportButton != null)
            {
                confirmImportButton.onClick.RemoveListener(OnConfirmImportClicked);
                confirmImportButton.onClick.AddListener(OnConfirmImportClicked);
            }
            if (cancelImportButton != null)
            {
                cancelImportButton.onClick.RemoveListener(OnCancelImportClicked);
                cancelImportButton.onClick.AddListener(OnCancelImportClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
                closeButton.onClick.AddListener(ClosePanel);
            }
        }

        private void OnDestroy()
        {
            if (exportButton != null) exportButton.onClick.RemoveListener(OnExportClicked);
            if (importButton != null) importButton.onClick.RemoveListener(OnImportClicked);
            if (confirmImportButton != null) confirmImportButton.onClick.RemoveListener(OnConfirmImportClicked);
            if (cancelImportButton != null) cancelImportButton.onClick.RemoveListener(OnCancelImportClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
        }

        public void OpenPanel()
        {
            if (ParentModeManager.Instance == null || !ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("PackImportExportUI: Parent mode required. Redirecting to parent check.");
                ShowMessage(parentModeOnlyMessage);
                if (parentModeUI != null) parentModeUI.OpenParentCheck();
                return;
            }
            if (panel != null) panel.SetActive(true);
            ShowMessage(string.Empty);
            Debug.Log("PackImportExportUI: Panel opened.");
        }

        public void ClosePanel()
        {
            if (confirmImportPanel != null) confirmImportPanel.SetActive(false);
            if (panel != null) panel.SetActive(false);
            Debug.Log("PackImportExportUI: Panel closed.");
        }

        private void OnExportClicked()
        {
            if (PackExportImportManager.Instance == null)
            {
                ShowMessage(exportFailedMessage);
                Debug.LogWarning("PackImportExportUI: PackExportImportManager not found.");
                return;
            }
            bool ok = PackExportImportManager.Instance.ExportCustomizationToFile(out string path);
            if (!ok)
            {
                ShowMessage(exportFailedMessage);
                return;
            }
            ShowMessage(string.Format(exportSuccessMessageFormat, path));
            Debug.Log($"PackImportExportUI: Export completed. path='{path}'.");
        }

        private void OnImportClicked()
        {
            string path = importPathInput != null ? importPathInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                ShowMessage(importNoPathMessage);
                return;
            }
            if (!File.Exists(path))
            {
                ShowMessage(string.Format(importFileNotFoundMessageFormat, path));
                Debug.LogWarning($"PackImportExportUI: Import file not found at '{path}'.");
                return;
            }
            pendingImportPath = path;
            if (confirmImportPanel != null) confirmImportPanel.SetActive(true);
            Debug.Log($"PackImportExportUI: Import confirmation requested for '{path}'.");
        }

        private void OnConfirmImportClicked()
        {
            if (confirmImportPanel != null) confirmImportPanel.SetActive(false);
            if (PackExportImportManager.Instance == null)
            {
                ShowMessage(importFailedMessage);
                return;
            }
            bool ok = PackExportImportManager.Instance.ImportCustomizationFromFile(pendingImportPath);
            if (!ok)
            {
                ShowMessage(importFailedMessage);
                pendingImportPath = string.Empty;
                return;
            }
            ShowMessage(importSuccessMessage);
            pendingImportPath = string.Empty;
            Debug.Log("PackImportExportUI: Import completed.");
        }

        private void OnCancelImportClicked()
        {
            if (confirmImportPanel != null) confirmImportPanel.SetActive(false);
            pendingImportPath = string.Empty;
            Debug.Log("PackImportExportUI: Import cancelled.");
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }
    }
}
