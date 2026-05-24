// FailPopupUI.cs
// Task 96 — Gentle, Capymong-led stage fail popup.
//
// Design goals:
//   - The fail popup should never feel punishing. Capymong is the speaker and
//     the title/dialogue use a soft, encouraging tone ("다시 도전해 볼까요?",
//     "괜찮아. 천천히 다시 해보면 돼.").
//   - The reason line is short and neutral ("이동 횟수를 모두 사용했어요.") —
//     never "실패", "패배", "못했어".
//   - Repeat-failure assistance: if FailureAssistAgent reports the current
//     stage has failed 3+ times, render a soft assist message. If
//     DifficultyReliefAgent reports extraMoves > 0, append a one-line note
//     about the relief.
//   - Buttons: Retry reloads the current stage; Hint reserves a hint for the
//     next start (the board is locked while this popup is open, so an immediate
//     hint isn't safe); StageSelect opens StageSelectUI.
//
// Defensive design:
//   - StageManager / FailureAssistAgent / DifficultyReliefAgent / HintAgent /
//     CharacterPackManager / CharacterAliasManager / GameHUDUI / StageSelectUI
//     are loose-coupled MonoBehaviour fields, accessed by reflection.
//   - StageData fields read by reflection so this UI doesn't depend on a
//     concrete data class.
//   - Every UI reference and every external manager is optional. Missing
//     references log a warning but never throw.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Sound;
using NabyeolDabyeolDreamPuzzle.Animation;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class FailPopupUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject failPopup;

        // ---- Header ----------------------------------------------------------------------

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI stageNameText;

        [Tooltip("Title text shown on the fail popup. Avoid strong negative wording.")]
        [SerializeField] private string titleMessage = "다시 도전해 볼까요?";

        // ---- Capymong dialogue -----------------------------------------------------------

        [Header("Capymong dialogue")]
        [SerializeField] private Image capymongImage;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Tooltip("Fallback display name shown when CharacterAliasManager is not available.")]
        [SerializeField] private string capymongDefaultName = "카피몽";

        [Tooltip("Internal id used to look up Capymong's character data / alias.")]
        [SerializeField] private string capymongCharacterId = "capymong";

        [Tooltip("Default encouragement line shown when no DialogueDatabase entry is available.")]
        [SerializeField] private string defaultEncouragement = "괜찮아. 천천히 다시 해보면 돼.";

        [Tooltip("DialogueDatabase key for Capymong's default fail dialogue (optional).")]
        [SerializeField] private string capymongFailDialogueKey = "character.capymong.fail.default";

        // ---- Reason / assist -------------------------------------------------------------

        [Header("Reason / assist")]
        [SerializeField] private TextMeshProUGUI reasonText;
        [SerializeField] private TextMeshProUGUI assistText;

        [Tooltip("Reason shown when remaining moves reached zero.")]
        [SerializeField] private string reasonOutOfMoves = "이동 횟수를 모두 사용했어요.";

        [Tooltip("Reason shown when the goal is still far away (fallback wording).")]
        [SerializeField] private string reasonGenericClose = "목표까지 조금 남았어요.";

        [Tooltip("Assist message after repeated failures (>= assistFailThreshold).")]
        [SerializeField] private string assistRepeatedFail = "이번엔 힌트를 준비해 둘게요.";

        [Tooltip("Assist message format for a queued next-start hint. '{0}' = stageId.")]
        [SerializeField] private string assistHintQueued = "다시 시작하면 힌트를 보여줄게!";

        [Tooltip("Assist message format for relief extra moves. '{0}' = extra move count.")]
        [SerializeField] private string assistReliefFormat = "다음 도전은 이동 횟수가 {0}번 늘어나요.";

        [Tooltip("Fail count at which assist messages start to appear.")]
        [SerializeField, Min(1)] private int assistFailThreshold = 3;

        // ---- Buttons ---------------------------------------------------------------------

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button stageSelectButton;

        // ---- External managers / UIs (loose coupling) ------------------------------------

        [Header("External managers (optional)")]
        [SerializeField] private MonoBehaviour stageManager;
        [SerializeField] private MonoBehaviour failureAssistAgent;
        [SerializeField] private MonoBehaviour difficultyReliefAgent;
        [SerializeField] private MonoBehaviour hintAgent;
        [SerializeField] private MonoBehaviour characterPackManager;
        [SerializeField] private MonoBehaviour characterAliasManager;
        [SerializeField] private MonoBehaviour dialogueDatabase;

        [Header("External UIs (optional)")]
        [SerializeField] private MonoBehaviour stageSelectUI;
        [SerializeField] private MonoBehaviour gameHUDUI;

        // ---- Runtime ---------------------------------------------------------------------

        private int currentStageId;
        private bool isShowing;
        private bool hintQueuedForNextStart;

        // ---- Lifecycle -------------------------------------------------------------------

        private void Awake()
        {
            if (failPopup != null) failPopup.SetActive(false);
        }

        private void OnEnable()
        {
            BindButton(retryButton,       OnRetryClicked);
            BindButton(hintButton,        OnHintClicked);
            BindButton(stageSelectButton, OnStageSelectClicked);
        }

        private void OnDisable()
        {
            UnbindButton(retryButton,       OnRetryClicked);
            UnbindButton(hintButton,        OnHintClicked);
            UnbindButton(stageSelectButton, OnStageSelectClicked);
        }

        // ---- Public API ------------------------------------------------------------------

        /// <summary>
        /// Show the fail popup. Safe to call with stageData == null. Duplicate
        /// calls while already visible are ignored — board input must stay
        /// locked while the popup is open, so showing twice is a no-op.
        /// </summary>
        public void ShowFailPopup(object stageData, int score, int remainingMoves)
        {
            if (isShowing)
            {
                Debug.Log("[FailPopup] already showing — duplicate ShowFailPopup ignored.");
                return;
            }
            isShowing = true;
            hintQueuedForNextStart = false;

            if (failPopup != null) failPopup.SetActive(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SfxType.PopupOpen);
                SoundManager.Instance.PlaySfx(SfxType.Fail);
            }

            int    stageId   = TryGetIntMember(stageData, "stageId", 0);
            string stageName = TryGetStringMember(stageData, "stageName");

            currentStageId = stageId;

            // ---- Header ----
            if (titleText != null)     titleText.text     = titleMessage;
            if (stageNameText != null) stageNameText.text = string.IsNullOrEmpty(stageName)
                ? (stageId > 0 ? $"Stage {stageId}" : string.Empty)
                : stageName;

            // ---- Capymong speaker + dialogue ----
            ApplyCapymongSpeaker();
            ApplyCapymongDialogue();

            // ---- Reason (gentle wording) ----
            ApplyReasonText(remainingMoves);

            // ---- Assist messages (repeated fail / relief) ----
            ApplyAssistText(stageId);

            // ---- Hint button state ----
            if (hintButton != null) hintButton.interactable = true;

            Debug.Log(
                $"[FailPopup] shown stageId={stageId} score={score} " +
                $"remainingMoves={remainingMoves}.");

            PlayShowAnimations();

            // TODO: Add gentle fail sound.
            // TODO: Add soft popup animation.
            // TODO: Add Capymong comforting voice.
            // TODO: When StoryNode failDialogues are added, prefer
            //       StoryManager.GetStageDisplayDialogues(StageFail) over the
            //       default encouragement line.
        }

        private void PlayShowAnimations()
        {
            SimpleAnimationManager mgr = SimpleAnimationManager.Instance;
            if (mgr == null || !mgr.AnimationsEnabled) return;

            if (failPopup != null)
            {
                RectTransform panelRect = failPopup.transform as RectTransform;
                if (panelRect != null) StartCoroutine(mgr.PlayPopupOpen(panelRect));
            }
            if (capymongImage != null)
            {
                RectTransform rect = capymongImage.rectTransform;
                if (rect != null) StartCoroutine(mgr.PlayCharacterBounce(rect));
            }
        }

        /// <summary>Hide the fail popup. Safe to call when already hidden.</summary>
        public void HideFailPopup()
        {
            bool wasShowing = isShowing;
            if (failPopup != null) failPopup.SetActive(false);
            isShowing = false;
            if (wasShowing && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SfxType.PopupClose);
            }
            Debug.Log("[FailPopup] hidden.");
        }

        // ---- Capymong speaker / dialogue -------------------------------------------------

        private void ApplyCapymongSpeaker()
        {
            string displayName = TryGetCharacterAlias(capymongCharacterId);
            if (string.IsNullOrEmpty(displayName)) displayName = capymongDefaultName;
            if (speakerNameText != null) speakerNameText.text = displayName;

            Sprite portrait = TryGetCharacterPortrait(capymongCharacterId);
            if (capymongImage != null)
            {
                if (portrait != null)
                {
                    capymongImage.sprite = portrait;
                    capymongImage.enabled = true;
                }
                // Leave the prefab's default sprite if none was found —
                // do not blank it, that would show a magenta box.
            }
        }

        private void ApplyCapymongDialogue()
        {
            string line = TryGetDialogueByKey(capymongFailDialogueKey);
            if (string.IsNullOrEmpty(line)) line = defaultEncouragement;
            if (dialogueText != null) dialogueText.text = line;
        }

        private string TryGetCharacterAlias(string characterId)
        {
            if (characterAliasManager == null || string.IsNullOrEmpty(characterId)) return null;
            try
            {
                var type = characterAliasManager.GetType();
                foreach (var name in new[] { "GetAlias", "GetCharacterAlias", "GetDisplayName" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null || method.ReturnType != typeof(string)) continue;
                    string s = method.Invoke(characterAliasManager, new object[] { characterId }) as string;
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] character alias lookup failed: {e.Message}");
            }
            return null;
        }

        private Sprite TryGetCharacterPortrait(string characterId)
        {
            if (characterPackManager == null || string.IsNullOrEmpty(characterId)) return null;
            try
            {
                var type = characterPackManager.GetType();
                foreach (var name in new[] { "GetCharacterData", "GetCharacter", "GetById" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null) continue;
                    object data = method.Invoke(characterPackManager, new object[] { characterId });
                    if (data == null) continue;

                    foreach (var spriteField in new[] { "portrait", "image", "characterSprite", "sprite" })
                    {
                        var sprite = TryGetMember(data, spriteField) as Sprite;
                        if (sprite != null) return sprite;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] character portrait lookup failed: {e.Message}");
            }
            return null;
        }

        private string TryGetDialogueByKey(string key)
        {
            if (dialogueDatabase == null || string.IsNullOrEmpty(key)) return null;
            try
            {
                var type = dialogueDatabase.GetType();
                foreach (var name in new[] { "GetDialogue", "GetLine", "GetText" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null || method.ReturnType != typeof(string)) continue;
                    return method.Invoke(dialogueDatabase, new object[] { key }) as string;
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[FailPopup] dialogue lookup skipped: {e.Message}");
            }
            return null;
        }

        // ---- Reason / assist text -------------------------------------------------------

        private void ApplyReasonText(int remainingMoves)
        {
            if (reasonText == null) return;
            reasonText.text = remainingMoves <= 0
                ? reasonOutOfMoves
                : reasonGenericClose;
        }

        private void ApplyAssistText(int stageId)
        {
            if (assistText == null) return;

            int  failCount  = TryGetStageFailCount(stageId);
            int  extraMoves = TryGetExtraMoves(stageId);
            bool repeated   = failCount >= assistFailThreshold;

            string line = null;
            if (repeated) line = assistRepeatedFail;
            if (extraMoves > 0)
            {
                string reliefLine = string.Format(assistReliefFormat, extraMoves);
                line = string.IsNullOrEmpty(line) ? reliefLine : reliefLine;
                // Keep the message short — show relief over repeated-fail when both apply.
            }

            if (string.IsNullOrEmpty(line))
            {
                assistText.text = string.Empty;
                assistText.gameObject.SetActive(false);
                return;
            }

            assistText.gameObject.SetActive(true);
            assistText.text = line;
            Debug.Log($"[FailPopup] assist text: '{line}' (fails={failCount} extraMoves={extraMoves}).");
        }

        private int TryGetStageFailCount(int stageId)
        {
            if (failureAssistAgent == null || stageId <= 0) return 0;
            try
            {
                var type = failureAssistAgent.GetType();
                foreach (var name in new[] { "GetFailCount", "GetStageFailCount", "GetFailures" })
                {
                    var method = type.GetMethod(name, new[] { typeof(int) });
                    if (method == null || method.ReturnType != typeof(int)) continue;
                    return (int)method.Invoke(failureAssistAgent, new object[] { stageId });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] fail-count lookup failed: {e.Message}");
            }
            return 0;
        }

        private int TryGetExtraMoves(int stageId)
        {
            if (difficultyReliefAgent == null || stageId <= 0) return 0;
            try
            {
                var type = difficultyReliefAgent.GetType();
                foreach (var name in new[] { "GetExtraMoves", "GetReliefExtraMoves", "GetExtraMovesForStage" })
                {
                    var method = type.GetMethod(name, new[] { typeof(int) });
                    if (method == null || method.ReturnType != typeof(int)) continue;
                    return (int)method.Invoke(difficultyReliefAgent, new object[] { stageId });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] extra-moves lookup failed: {e.Message}");
            }
            return 0;
        }

        // ---- Button handlers ------------------------------------------------------------

        private void OnRetryClicked()
        {
            int retryId = ResolveCurrentStageId();
            if (retryId <= 0)
            {
                Debug.LogWarning("[FailPopup] retry clicked but currentStageId unknown.");
                return;
            }
            if (!TryInvokeStageLoad(retryId))
            {
                Debug.LogWarning($"[FailPopup] retry: LoadStageById({retryId}) failed.");
                return;
            }
            Debug.Log($"[FailPopup] retry loaded stage {retryId}.");

            // The hint reservation (if any) is handed off to HintAgent here.
            if (hintQueuedForNextStart) TryConsumeQueuedHint(retryId);

            TryShowHUD();
            HideFailPopup();
        }

        private void OnHintClicked()
        {
            int stageId = ResolveCurrentStageId();
            if (stageId <= 0)
            {
                Debug.LogWarning("[FailPopup] hint clicked but currentStageId unknown.");
                return;
            }

            bool reserved = TryRequestHintOnNextStart(stageId);
            hintQueuedForNextStart = true;

            if (assistText != null)
            {
                assistText.gameObject.SetActive(true);
                assistText.text = assistHintQueued;
            }
            if (hintButton != null) hintButton.interactable = false;

            Debug.Log($"[FailPopup] hint queued for next start (reserved={reserved}, stageId={stageId}).");
        }

        private void OnStageSelectClicked()
        {
            if (stageSelectUI == null)
            {
                Debug.LogWarning("[FailPopup] stageSelectUI not assigned — closing popup only.");
                HideFailPopup();
                return;
            }
            try
            {
                var method = stageSelectUI.GetType().GetMethod("OpenStageSelect", Type.EmptyTypes);
                if (method != null)
                {
                    method.Invoke(stageSelectUI, null);
                }
                else
                {
                    stageSelectUI.SendMessage("OpenStageSelect", SendMessageOptions.DontRequireReceiver);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] OpenStageSelect failed: {e.Message}");
            }
            TryHideHUD();
            HideFailPopup();
        }

        // ---- Hint reservation -----------------------------------------------------------

        private bool TryRequestHintOnNextStart(int stageId)
        {
            // Preferred: FailureAssistAgent.RequestHintOnNextStart(int).
            if (failureAssistAgent != null)
            {
                try
                {
                    var method = failureAssistAgent.GetType().GetMethod(
                        "RequestHintOnNextStart", new[] { typeof(int) });
                    if (method != null)
                    {
                        method.Invoke(failureAssistAgent, new object[] { stageId });
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FailPopup] RequestHintOnNextStart failed: {e.Message}");
                }
            }

            // Fallback: HintAgent.QueueHintForStage(int).
            if (hintAgent != null)
            {
                try
                {
                    var method = hintAgent.GetType().GetMethod(
                        "QueueHintForStage", new[] { typeof(int) });
                    if (method != null)
                    {
                        method.Invoke(hintAgent, new object[] { stageId });
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FailPopup] QueueHintForStage failed: {e.Message}");
                }
            }

            Debug.Log("[FailPopup] no hint queue API available — hint reservation is UI-only.");
            return false;
        }

        private void TryConsumeQueuedHint(int stageId)
        {
            if (hintAgent == null) return;
            try
            {
                var method = hintAgent.GetType().GetMethod("ShowHint", new[] { typeof(int) });
                if (method != null) method.Invoke(hintAgent, new object[] { stageId });
            }
            catch (Exception e)
            {
                Debug.Log($"[FailPopup] queued hint show skipped: {e.Message}");
            }
        }

        // ---- Stage id / loading ---------------------------------------------------------

        private int ResolveCurrentStageId()
        {
            if (currentStageId > 0) return currentStageId;
            object data = TryGetCurrentStageData();
            return TryGetIntMember(data, "stageId", 0);
        }

        private object TryGetCurrentStageData()
        {
            if (stageManager == null) return null;
            try
            {
                var type = stageManager.GetType();
                var prop = type.GetProperty("CurrentStageData");
                if (prop != null) return prop.GetValue(stageManager, null);
                var field = type.GetField("CurrentStageData");
                if (field != null) return field.GetValue(stageManager);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] CurrentStageData lookup failed: {e.Message}");
            }
            return null;
        }

        private bool TryInvokeStageLoad(int stageId)
        {
            if (stageManager == null || stageId <= 0) return false;
            try
            {
                var method = stageManager.GetType().GetMethod("LoadStageById", new[] { typeof(int) });
                if (method == null)
                {
                    Debug.LogWarning("[FailPopup] StageManager.LoadStageById(int) not found.");
                    return false;
                }
                method.Invoke(stageManager, new object[] { stageId });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FailPopup] LoadStageById failed: {e.Message}");
                return false;
            }
        }

        private void TryShowHUD()
        {
            if (gameHUDUI == null) return;
            try
            {
                var method = gameHUDUI.GetType().GetMethod("ShowHUD", Type.EmptyTypes);
                if (method != null) method.Invoke(gameHUDUI, null);
                else gameHUDUI.SendMessage("ShowHUD", SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception e)
            {
                Debug.Log($"[FailPopup] ShowHUD skipped: {e.Message}");
            }
        }

        private void TryHideHUD()
        {
            if (gameHUDUI == null) return;
            try
            {
                var method = gameHUDUI.GetType().GetMethod("HideHUD", Type.EmptyTypes);
                if (method != null) method.Invoke(gameHUDUI, null);
                else gameHUDUI.SendMessage("HideHUD", SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception e)
            {
                Debug.Log($"[FailPopup] HideHUD skipped: {e.Message}");
            }
        }

        // ---- Reflection helpers ---------------------------------------------------------

        private static object TryGetMember(object data, string memberName)
        {
            if (data == null || string.IsNullOrEmpty(memberName)) return null;
            try
            {
                var type = data.GetType();
                var field = type.GetField(memberName);
                if (field != null) return field.GetValue(data);
                var prop = type.GetProperty(memberName);
                if (prop != null) return prop.GetValue(data, null);
            }
            catch (Exception) { /* swallow */ }
            return null;
        }

        private static string TryGetStringMember(object data, string memberName)
        {
            object value = TryGetMember(data, memberName);
            return value as string;
        }

        private static int TryGetIntMember(object data, string memberName, int fallback)
        {
            object value = TryGetMember(data, memberName);
            if (value is int i) return i;
            return fallback;
        }

        // ---- Tiny UI helpers ------------------------------------------------------------

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

        // ---- Integration notes -----------------------------------------------------------
        //
        // BoardManager.ShowFailPopup integration pattern:
        //
        //     [SerializeField] private FailPopupUI failPopupUI;
        //     [SerializeField] private GameObject failPopup; // legacy fallback
        //
        //     private void ShowFailPopup()
        //     {
        //         if (isStageFailed) return;
        //         isStageFailed = true;
        //
        //         if (failPopupUI != null && StageManager.Instance != null)
        //         {
        //             failPopupUI.ShowFailPopup(
        //                 StageManager.Instance.CurrentStageData,
        //                 currentScore,
        //                 remainingMoves);
        //         }
        //         else if (failPopup != null)
        //         {
        //             failPopup.SetActive(true);
        //         }
        //     }
        //
        // The legacy fallback path is preserved so existing scenes that have
        // only the raw GameObject failPopup wired up keep working.
    }
}
