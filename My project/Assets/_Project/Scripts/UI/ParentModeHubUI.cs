// ParentModeHubUI.cs
// Task 98 — Parent-mode settings hub (Character / Story / Pack / Logs / AI policy).
//
// Design:
//   - Acts as the post-authentication hub for parent mode. ParentModeUI runs the
//     numeric guardian-check; on success it calls OpenParentModeHub() here. The
//     hub does not authenticate by itself, but refuses to open unless
//     ParentModeManager.CanAccessParentOnlyMenu() is true so a stray call can
//     never expose parent-only menus.
//   - Each menu button maps to one existing parent-only UI script. Typed
//     references are used for UIs that live in this project; AgentLogUI and
//     AIIntegrationPolicyUI may not exist yet, so they are wired through a
//     loose MonoBehaviour reference + reflection. Missing pieces simply show
//     "준비 중" in the message line instead of throwing.
//   - Auto-closes when ParentModeManager exits (timeout or manual exit), via
//     both the OnExitParentMode event and a defensive Update poll.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class ParentModeHubUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject parentModeHubPanel;

        // ---- Header / status --------------------------------------------------------------

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI parentModeStatusText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;

        // ---- Menu buttons -----------------------------------------------------------------

        [Header("Menu Buttons")]
        [SerializeField] private Button characterAliasButton;
        [SerializeField] private Button representativeDialogueButton;
        [SerializeField] private Button storyMakerButton;
        [SerializeField] private Button storyDialogueEditButton;
        [SerializeField] private Button customStoryPreviewButton;
        [SerializeField] private Button resetConfirmButton;
        [SerializeField] private Button packImportExportButton;
        [SerializeField] private Button agentLogButton;
        [SerializeField] private Button aiPolicyButton;
        [SerializeField] private Button safetyFilterButton;
        [SerializeField] private Button exitParentModeButton;

        // ---- Linked UIs -------------------------------------------------------------------

        [Header("Linked UIs (optional, in-project)")]
        [SerializeField] private CharacterAliasUI characterAliasUI;
        [SerializeField] private RepresentativeDialogueUI representativeDialogueUI;
        [SerializeField] private StoryMakerUI storyMakerUI;
        [SerializeField] private StoryDialogueEditUI storyDialogueEditUI;
        [SerializeField] private CustomStoryPreviewUI customStoryPreviewUI;
        [SerializeField] private ResetConfirmUI resetConfirmUI;
        [SerializeField] private PackImportExportUI packImportExportUI;

        [Header("Linked UIs (optional, loose-coupled)")]
        [Tooltip("AgentLogUI MonoBehaviour. Hub calls Open() via reflection.")]
        [SerializeField] private MonoBehaviour agentLogUI;
        [Tooltip("AIIntegrationPolicyUI MonoBehaviour. Hub calls Open() via reflection.")]
        [SerializeField] private MonoBehaviour aiIntegrationPolicyUI;

        // ---- Strings ----------------------------------------------------------------------

        [Header("Messages")]
        [SerializeField] private string titleMessage = "부모 모드";
        [SerializeField] private string descriptionMessage = "스토리와 캐릭터 설정을 보호자가 관리할 수 있어요.";
        [SerializeField] private string statusActiveMessage = "부모 모드가 켜져 있어요.";
        [SerializeField] private string notReadyMessage = "이 기능은 아직 준비 중이에요.";
        [SerializeField] private string safetyFilterMessage = "안전 필터는 금칙어, 개인정보, 무서운 표현을 막아줘요.";
        [SerializeField] private string aiPolicyFallbackMessage = "현재 버전은 외부 AI를 사용하지 않아요.";
        [SerializeField] private string authRequiredMessage = "부모 모드 인증이 필요해요.";

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (parentModeHubPanel != null) parentModeHubPanel.SetActive(false);

            if (titleText != null)       titleText.text = titleMessage;
            if (descriptionText != null) descriptionText.text = descriptionMessage;
            if (parentModeStatusText != null) parentModeStatusText.text = statusActiveMessage;
            if (messageText != null) messageText.text = string.Empty;
        }

        private void OnEnable()
        {
            BindButton(closeButton,                  OnCloseClicked);
            BindButton(characterAliasButton,         OnCharacterAliasClicked);
            BindButton(representativeDialogueButton, OnRepresentativeDialogueClicked);
            BindButton(storyMakerButton,             OnStoryMakerClicked);
            BindButton(storyDialogueEditButton,      OnStoryDialogueEditClicked);
            BindButton(customStoryPreviewButton,     OnCustomStoryPreviewClicked);
            BindButton(resetConfirmButton,           OnResetConfirmClicked);
            BindButton(packImportExportButton,       OnPackImportExportClicked);
            BindButton(agentLogButton,               OnAgentLogClicked);
            BindButton(aiPolicyButton,               OnAIPolicyClicked);
            BindButton(safetyFilterButton,           OnSafetyFilterClicked);
            BindButton(exitParentModeButton,         OnExitParentModeClicked);

            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.OnExitParentMode += HandleParentModeExited;
            }
        }

        private void OnDisable()
        {
            UnbindButton(closeButton,                  OnCloseClicked);
            UnbindButton(characterAliasButton,         OnCharacterAliasClicked);
            UnbindButton(representativeDialogueButton, OnRepresentativeDialogueClicked);
            UnbindButton(storyMakerButton,             OnStoryMakerClicked);
            UnbindButton(storyDialogueEditButton,      OnStoryDialogueEditClicked);
            UnbindButton(customStoryPreviewButton,     OnCustomStoryPreviewClicked);
            UnbindButton(resetConfirmButton,           OnResetConfirmClicked);
            UnbindButton(packImportExportButton,       OnPackImportExportClicked);
            UnbindButton(agentLogButton,               OnAgentLogClicked);
            UnbindButton(aiPolicyButton,               OnAIPolicyClicked);
            UnbindButton(safetyFilterButton,           OnSafetyFilterClicked);
            UnbindButton(exitParentModeButton,         OnExitParentModeClicked);

            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.OnExitParentMode -= HandleParentModeExited;
            }
        }

        private void Update()
        {
            if (parentModeHubPanel == null || !parentModeHubPanel.activeSelf) return;
            if (ParentModeManager.Instance != null && !ParentModeManager.Instance.IsParentModeActive)
            {
                Debug.Log("ParentModeHubUI: ParentMode no longer active — closing hub.");
                CloseParentModeHub();
            }
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>
        /// Open the hub panel. Requires ParentModeManager to currently allow parent-only
        /// access (active or debug-bypass). Otherwise logs and ignores the call.
        /// </summary>
        public void OpenParentModeHub()
        {
            ParentModeManager mgr = ParentModeManager.Instance;
            if (mgr == null || !mgr.CanAccessParentOnlyMenu())
            {
                Debug.LogWarning("ParentModeHubUI: OpenParentModeHub denied — parent mode not active.");
                ShowMessage(authRequiredMessage);
                return;
            }

            if (parentModeHubPanel != null) parentModeHubPanel.SetActive(true);
            if (parentModeStatusText != null) parentModeStatusText.text = statusActiveMessage;
            ShowMessage(string.Empty);
            Debug.Log("ParentModeHubUI: Hub opened.");
        }

        /// <summary>Close the hub panel. Does NOT exit parent mode itself.</summary>
        public void CloseParentModeHub()
        {
            if (parentModeHubPanel != null) parentModeHubPanel.SetActive(false);
            Debug.Log("ParentModeHubUI: Hub closed.");
        }

        /// <summary>Display a short message in the hub's message row. Null-safe.</summary>
        public void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg ?? string.Empty;
        }

        // ---- Button handlers --------------------------------------------------------------

        private void OnCloseClicked() => CloseParentModeHub();

        private void OnCharacterAliasClicked()
        {
            if (characterAliasUI == null) { ShowNotReady("CharacterAliasUI"); return; }
            ShowMessage(string.Empty);
            characterAliasUI.OpenAliasMenu();
        }

        private void OnRepresentativeDialogueClicked()
        {
            if (representativeDialogueUI == null) { ShowNotReady("RepresentativeDialogueUI"); return; }
            ShowMessage(string.Empty);
            representativeDialogueUI.OpenDialogueMenu();
        }

        private void OnStoryMakerClicked()
        {
            if (storyMakerUI == null) { ShowNotReady("StoryMakerUI"); return; }
            ShowMessage(string.Empty);
            storyMakerUI.OpenPanel();
        }

        private void OnStoryDialogueEditClicked()
        {
            if (storyDialogueEditUI == null) { ShowNotReady("StoryDialogueEditUI"); return; }
            ShowMessage(string.Empty);
            storyDialogueEditUI.OpenEditMenu();
        }

        private void OnCustomStoryPreviewClicked()
        {
            if (customStoryPreviewUI == null) { ShowNotReady("CustomStoryPreviewUI"); return; }
            ShowMessage(string.Empty);
            customStoryPreviewUI.OpenPreviewPanel();
        }

        private void OnResetConfirmClicked()
        {
            if (resetConfirmUI == null) { ShowNotReady("ResetConfirmUI"); return; }
            ShowMessage(string.Empty);
            resetConfirmUI.OpenResetConfirm();
        }

        private void OnPackImportExportClicked()
        {
            if (packImportExportUI == null) { ShowNotReady("PackImportExportUI"); return; }
            ShowMessage(string.Empty);
            packImportExportUI.OpenPanel();
        }

        private void OnAgentLogClicked()
        {
            if (!TryInvokeOpen(agentLogUI, "Open", "OpenPanel", "Show"))
            {
                ShowNotReady("AgentLogUI");
            }
            else
            {
                ShowMessage(string.Empty);
            }
        }

        private void OnAIPolicyClicked()
        {
            if (!TryInvokeOpen(aiIntegrationPolicyUI, "Open", "OpenPanel", "Show"))
            {
                ShowMessage(aiPolicyFallbackMessage);
                Debug.Log("ParentModeHubUI: AIIntegrationPolicyUI not wired — showing fallback message.");
            }
            else
            {
                ShowMessage(string.Empty);
            }
        }

        private void OnSafetyFilterClicked()
        {
            ShowMessage(safetyFilterMessage);
            Debug.Log("ParentModeHubUI: Safety filter info shown.");
        }

        private void OnExitParentModeClicked()
        {
            if (ParentModeManager.Instance != null)
            {
                ParentModeManager.Instance.ExitParentMode();
            }
            CloseParentModeHub();
        }

        // ---- Event handlers ---------------------------------------------------------------

        private void HandleParentModeExited()
        {
            CloseParentModeHub();
        }

        // ---- Helpers ----------------------------------------------------------------------

        private void ShowNotReady(string uiName)
        {
            ShowMessage(notReadyMessage);
            Debug.LogWarning($"ParentModeHubUI: {uiName} not wired — showing 준비 중 message.");
        }

        private bool TryInvokeOpen(MonoBehaviour target, params string[] methodNames)
        {
            if (target == null || methodNames == null) return false;
            try
            {
                var type = target.GetType();
                foreach (var name in methodNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    var method = type.GetMethod(name, Type.EmptyTypes);
                    if (method == null) continue;
                    method.Invoke(target, null);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ParentModeHubUI: Invoking open on {target.GetType().Name} failed: {e.Message}");
            }
            return false;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
        }
    }
}
