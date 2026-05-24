// AgentLogUI.cs
// Task 91 — Parent-mode-only viewer for AgentLogManager.
//
// Shows only the aggregated, anonymous summary built by AgentLogManager.
// This UI must never display personally identifiable info — the underlying
// manager already refuses to store it, but this UI also limits itself to
// the strings returned by BuildLocalSummary() and GetPrivacySummary().
//
// Defensive design:
//   - AgentLogManager.Instance missing -> show fallback text, never throw.
//   - parentModeUI reference missing  -> block open with a Debug.LogWarning,
//                                        do not silently expose the panel.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Agent;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class AgentLogUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject logPanel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI privacyText;

        [Header("Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Header("Parent mode")]
        [Tooltip("ParentModeUI component reference. Held as MonoBehaviour so this " +
                 "UI does not hard-depend on the ParentModeUI type at compile time.")]
        [SerializeField] private MonoBehaviour parentModeUI;

        [Tooltip("If true, skip the parent-mode check (debug builds only).")]
        [SerializeField] private bool bypassParentModeForDebug;

        // Tracks the click-twice confirmation state for the reset button.
        // Task 21: full confirmation panel is a TODO; for v1 we require a
        // second click within the same session to confirm.
        private bool resetArmed;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (logPanel != null) logPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
            if (resetButton   != null) resetButton.onClick.AddListener(OnResetClicked);
            if (closeButton   != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(OnRefreshClicked);
            if (resetButton   != null) resetButton.onClick.RemoveListener(OnResetClicked);
            if (closeButton   != null) closeButton.onClick.RemoveListener(Close);
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>Open the panel. Blocks unless parent mode is active.</summary>
        public void Open()
        {
            if (!bypassParentModeForDebug && !IsParentModeActive())
            {
                Debug.LogWarning("[AgentLogUI] blocked: parent mode not active. Opening parent check.");
                RequestParentCheck();
                return;
            }

            if (logPanel != null) logPanel.SetActive(true);
            resetArmed = false;
            Refresh();
        }

        /// <summary>Close the panel.</summary>
        public void Close()
        {
            if (logPanel != null) logPanel.SetActive(false);
            resetArmed = false;
        }

        /// <summary>Rebuild the summary text from AgentLogManager.</summary>
        public void Refresh()
        {
            var manager = AgentLogManager.Instance;

            if (summaryText != null)
            {
                summaryText.text = manager != null
                    ? manager.BuildLocalSummary()
                    : "로그 시스템이 활성화되지 않았습니다.";
            }

            if (privacyText != null)
            {
                privacyText.text = manager != null
                    ? manager.GetPrivacySummary()
                    : "이 로그는 숫자만 기기에 저장합니다. 개인정보는 저장하지 않습니다.";
            }

            Debug.Log("[AgentLogUI] summary refreshed.");
        }

        // ---- Button handlers --------------------------------------------------------------

        private void OnRefreshClicked() => Refresh();

        private void OnResetClicked()
        {
            // v1 confirmation: two-step click. A dedicated confirm panel can replace this later.
            // TODO: integrate with ResetConfirmUI for a clearer modal confirmation flow.
            if (!resetArmed)
            {
                resetArmed = true;
                if (summaryText != null)
                {
                    summaryText.text =
                        "정말 모든 에이전트 로그를 초기화할까요?\n" +
                        "초기화 버튼을 한 번 더 누르면 삭제됩니다.\n" +
                        "(다른 게임 데이터는 영향을 받지 않습니다.)";
                }
                Debug.LogWarning("[AgentLogUI] reset armed; awaiting confirmation click.");
                return;
            }

            var manager = AgentLogManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[AgentLogUI] reset requested but AgentLogManager.Instance is null.");
                resetArmed = false;
                Refresh();
                return;
            }

            manager.ResetAllAgentLogs();
            resetArmed = false;
            Refresh();
        }

        // ---- Parent-mode integration ------------------------------------------------------

        private bool IsParentModeActive()
        {
            if (parentModeUI == null) return false;

            // Loose coupling: ParentModeUI is expected to expose an "IsParentMode"
            // method (bool). We check via SendMessage so this script compiles even
            // before ParentModeUI is added in another task.
            var go = parentModeUI.gameObject;
            try
            {
                // Try property-style: bool IsParentMode { get; }
                var type = parentModeUI.GetType();
                var prop = type.GetProperty("IsParentMode");
                if (prop != null && prop.PropertyType == typeof(bool))
                    return (bool)prop.GetValue(parentModeUI, null);

                var field = type.GetField("IsParentMode");
                if (field != null && field.FieldType == typeof(bool))
                    return (bool)field.GetValue(parentModeUI);

                var method = type.GetMethod("IsParentMode");
                if (method != null && method.ReturnType == typeof(bool))
                    return (bool)method.Invoke(parentModeUI, null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AgentLogUI] parent-mode probe failed: {e.Message}");
            }

            // Fall back to a child convention: a GameObject named "ParentModeActive"
            // toggled by ParentModeUI. If neither path is available, assume not active.
            var marker = go.transform.Find("ParentModeActive");
            return marker != null && marker.gameObject.activeInHierarchy;
        }

        private void RequestParentCheck()
        {
            if (parentModeUI == null)
            {
                Debug.LogWarning("[AgentLogUI] no ParentModeUI reference assigned.");
                return;
            }

            // SendMessage keeps the dependency loose; ParentModeUI implements
            // OpenParentCheck() as a public void method.
            parentModeUI.SendMessage("OpenParentCheck", SendMessageOptions.DontRequireReceiver);
        }
    }
}
