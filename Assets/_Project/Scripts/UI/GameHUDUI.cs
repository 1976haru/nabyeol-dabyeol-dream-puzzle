// GameHUDUI.cs
// Task 94 — Single in-game HUD that surfaces stage name, goal, moves,
// score and the six character skill buttons.
//
// Design:
//   - BoardManager calls SetStageName / SetGoalText / SetMoveText /
//     SetScoreText so the HUD never reads BoardManager state directly.
//   - Skill buttons each have a label that reads "이름 (N)" where N is the
//     remaining-uses count read from SkillManager via reflection.
//   - Skill refresh strategy:
//       (a) Reflection-based subscribe to SkillManager.OnSkillUseCountChanged
//           if its delegate type matches a parameterless handler.
//       (b) Polling every skillRefreshIntervalSeconds while HUD is visible —
//           cheap and works even when (a) is unavailable.
//
// Defensive design: every UI reference and every external manager is optional.
// Buttons whose Button reference is null are skipped entirely. Missing
// SkillManager keeps buttons usable but their click only logs a warning.

using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class GameHUDUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject gameHUDPanel;

        // ---- Top bar ----------------------------------------------------------------------

        [Header("Top bar")]
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private TextMeshProUGUI goalText;
        [SerializeField] private TextMeshProUGUI moveText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Color used for moveText when remaining moves <= lowMoveThreshold.")]
        [SerializeField] private Color lowMoveColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color normalMoveColor = Color.white;
        [SerializeField, Min(0)] private int lowMoveThreshold = 5;

        // ---- Skill bar --------------------------------------------------------------------

        [Serializable]
        public class SkillButtonBinding
        {
            public string displayName;
            public string skillId;          // for SkillManager remaining-count lookup
            public string useMethodName;    // SkillManager method to invoke on click
            public Button button;
            public TextMeshProUGUI label;
            public Image background;        // optional, recolored from CharacterPack if available
            public string characterId;      // for CharacterPack color lookup
            public Color defaultColor = Color.white;
        }

        [Header("Skill bar")]
        [SerializeField] private SkillButtonBinding nabyeolSkill = new SkillButtonBinding
        {
            displayName = "별자리 보기", skillId = "nabyeol_hint",
            useMethodName = "UseNabyeolHintSkill", characterId = "nabyeol",
        };
        [SerializeField] private SkillButtonBinding dabyeolSkill = new SkillButtonBinding
        {
            displayName = "꿈결 움직이기", skillId = "dabyeol_move",
            useMethodName = "UseDabyeolMoveSkill", characterId = "dabyeol",
        };
        [SerializeField] private SkillButtonBinding twinSkill = new SkillButtonBinding
        {
            displayName = "트윈스타 팡", skillId = "twin_star_pop",
            useMethodName = "UseTwinStarPopSkill", characterId = "twin",
        };
        [SerializeField] private SkillButtonBinding capymongSkill = new SkillButtonBinding
        {
            displayName = "느긋한 숨결", skillId = "capymong_breath",
            useMethodName = "UseCapymongBreathSkill", characterId = "capymong",
        };
        [SerializeField] private SkillButtonBinding poporingSkill = new SkillButtonBinding
        {
            displayName = "방울 힌트", skillId = "poporing_bubble_hint",
            useMethodName = "UsePoporingBubbleHintSkill", characterId = "poporing",
        };
        [SerializeField] private SkillButtonBinding mochirunSkill = new SkillButtonBinding
        {
            displayName = "숫자 정리", skillId = "mochirun_number_sort",
            useMethodName = "UseMochirunNumberSortSkill", characterId = "mochirun",
        };

        // ---- Mini dialogue ----------------------------------------------------------------

        [Header("Mini dialogue")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        // ---- System buttons ---------------------------------------------------------------

        [Header("System buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button backToStageSelectButton;

        // ---- External managers / UIs (loose coupling) -------------------------------------

        [Header("External managers (optional)")]
        [SerializeField] private MonoBehaviour stageManager;
        [SerializeField] private MonoBehaviour skillManager;
        [SerializeField] private MonoBehaviour characterPackManager;
        [SerializeField] private MonoBehaviour characterUIManager;
        [SerializeField] private MonoBehaviour stageSelectUI;

        // ---- Refresh policy ---------------------------------------------------------------

        [Header("Refresh policy")]
        [Tooltip("Polling interval for RefreshSkillButtons while HUD is visible.")]
        [SerializeField, Min(0.05f)] private float skillRefreshIntervalSeconds = 0.25f;

        // ---- Runtime ----------------------------------------------------------------------

        private SkillButtonBinding[] allSkillBindings;
        private UnityEngine.Events.UnityAction[] skillClickHandlers;
        private Coroutine pollRoutine;
        private Delegate subscribedDelegate;
        private EventInfo subscribedEvent;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            allSkillBindings = new[]
            {
                nabyeolSkill, dabyeolSkill, twinSkill,
                capymongSkill, poporingSkill, mochirunSkill,
            };
            skillClickHandlers = new UnityEngine.Events.UnityAction[allSkillBindings.Length];
        }

        private void OnEnable()
        {
            for (int i = 0; i < allSkillBindings.Length; i++) BindSkillButton(i);
            BindButton(pauseButton,                OnPauseClicked);
            BindButton(backToStageSelectButton,    OnBackToStageSelectClicked);

            ApplyCharacterColors();
            TrySubscribeToSkillEvent();
            StartSkillPolling();
            RefreshSkillButtons();
        }

        private void OnDisable()
        {
            for (int i = 0; i < allSkillBindings.Length; i++) UnbindSkillButton(i);
            UnbindButton(pauseButton,             OnPauseClicked);
            UnbindButton(backToStageSelectButton, OnBackToStageSelectClicked);

            TryUnsubscribeFromSkillEvent();
            StopSkillPolling();
        }

        // ---- Public API: HUD show/hide ----------------------------------------------------

        public void ShowHUD()
        {
            if (gameHUDPanel != null) gameHUDPanel.SetActive(true);
            RefreshSkillButtons();
            Debug.Log("[GameHUD] shown.");
        }

        public void HideHUD()
        {
            if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
            Debug.Log("[GameHUD] hidden.");
        }

        // ---- Public API: setters from BoardManager / StageManager -------------------------

        public void SetStageName(string stageName)
        {
            if (stageNameText != null)
                stageNameText.text = string.IsNullOrEmpty(stageName) ? string.Empty : stageName;
        }

        public void SetGoalText(string text)
        {
            if (goalText != null)
                goalText.text = string.IsNullOrEmpty(text) ? "목표 -" : text;
        }

        public void SetMoveText(int remainingMoves)
        {
            if (moveText == null) return;
            moveText.text = $"이동 {Mathf.Max(0, remainingMoves)}";
            moveText.color = remainingMoves <= lowMoveThreshold ? lowMoveColor : normalMoveColor;
        }

        public void SetScoreText(int score)
        {
            if (scoreText != null)
                scoreText.text = $"점수 {score:N0}";
        }

        public void SetCharacterDialogue(string speakerName, string dialogue)
        {
            if (speakerNameText != null) speakerNameText.text = speakerName ?? string.Empty;
            if (dialogueText    != null) dialogueText.text    = dialogue    ?? string.Empty;
        }

        // ---- Public API: refresh skill buttons --------------------------------------------

        /// <summary>
        /// Re-read remaining-uses from SkillManager and update each button's
        /// label and interactable state. Safe to call from anywhere.
        /// </summary>
        public void RefreshSkillButtons()
        {
            if (allSkillBindings == null) return;
            foreach (var b in allSkillBindings) RefreshOne(b);
        }

        private void RefreshOne(SkillButtonBinding b)
        {
            if (b == null || b.button == null) return;

            int remaining = TryGetSkillRemainingUses(b.skillId);

            if (b.label != null)
            {
                b.label.text = remaining >= 0
                    ? $"{b.displayName} ({remaining})"
                    : b.displayName;
            }

            // remaining < 0 means SkillManager unavailable -> leave interactable for v1.
            // remaining == 0 means used up -> disable.
            b.button.interactable = remaining != 0;
        }

        // ---- Skill click handler ----------------------------------------------------------

        private void OnSkillClicked(SkillButtonBinding b)
        {
            if (b == null) return;
            Debug.Log($"[GameHUD] skill click: {b.displayName} ({b.skillId}).");

            if (skillManager == null)
            {
                Debug.LogWarning($"[GameHUD] SkillManager not assigned — '{b.useMethodName}' ignored.");
                return;
            }

            try
            {
                var method = skillManager.GetType().GetMethod(b.useMethodName, Type.EmptyTypes);
                if (method == null)
                {
                    Debug.LogWarning($"[GameHUD] SkillManager.{b.useMethodName}() not found.");
                    return;
                }
                method.Invoke(skillManager, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameHUD] SkillManager.{b.useMethodName}() threw: {e.Message}");
            }

            // Refresh after invocation so the count and disabled-state update immediately.
            RefreshSkillButtons();
        }

        // ---- Skill remaining-uses lookup --------------------------------------------------

        private int TryGetSkillRemainingUses(string skillId)
        {
            if (skillManager == null || string.IsNullOrEmpty(skillId)) return -1;
            try
            {
                var type = skillManager.GetType();
                foreach (var name in new[] { "GetRemainingUses", "GetSkillRemainingUses", "GetUsesLeft" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null || method.ReturnType != typeof(int)) continue;
                    return (int)method.Invoke(skillManager, new object[] { skillId });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameHUD] remaining-uses lookup failed for '{skillId}': {e.Message}");
            }
            return -1;
        }

        // ---- SkillManager event subscription (best-effort, parameterless handler) ---------

        private void TrySubscribeToSkillEvent()
        {
            subscribedDelegate = null;
            subscribedEvent = null;

            if (skillManager == null) return;

            try
            {
                var type = skillManager.GetType();
                var evt = type.GetEvent("OnSkillUseCountChanged");
                if (evt == null) return;

                // Try to create a delegate from RefreshSkillButtons. If the
                // event's delegate type does not accept a parameterless handler
                // this will throw — we swallow and fall back to polling.
                var handler = Delegate.CreateDelegate(
                    evt.EventHandlerType, this,
                    typeof(GameHUDUI).GetMethod(nameof(RefreshSkillButtons)),
                    throwOnBindFailure: false);

                if (handler == null) return;

                evt.AddEventHandler(skillManager, handler);
                subscribedDelegate = handler;
                subscribedEvent = evt;
                Debug.Log("[GameHUD] subscribed to SkillManager.OnSkillUseCountChanged.");
            }
            catch (Exception e)
            {
                Debug.Log($"[GameHUD] event subscribe skipped: {e.Message}");
            }
        }

        private void TryUnsubscribeFromSkillEvent()
        {
            if (subscribedEvent == null || subscribedDelegate == null || skillManager == null) return;
            try
            {
                subscribedEvent.RemoveEventHandler(skillManager, subscribedDelegate);
            }
            catch (Exception e)
            {
                Debug.Log($"[GameHUD] event unsubscribe failed: {e.Message}");
            }
            subscribedEvent = null;
            subscribedDelegate = null;
        }

        // ---- Polling ---------------------------------------------------------------------

        private void StartSkillPolling()
        {
            StopSkillPolling();
            if (!isActiveAndEnabled) return;
            pollRoutine = StartCoroutine(SkillPollLoop());
        }

        private void StopSkillPolling()
        {
            if (pollRoutine != null) { StopCoroutine(pollRoutine); pollRoutine = null; }
        }

        private IEnumerator SkillPollLoop()
        {
            var wait = new WaitForSeconds(skillRefreshIntervalSeconds);
            while (true)
            {
                yield return wait;
                if (gameHUDPanel == null || gameHUDPanel.activeInHierarchy)
                    RefreshSkillButtons();
            }
        }

        // ---- CharacterPack color tinting (optional) ---------------------------------------

        private void ApplyCharacterColors()
        {
            if (characterPackManager == null || allSkillBindings == null) return;

            foreach (var b in allSkillBindings)
            {
                if (b == null || b.background == null || string.IsNullOrEmpty(b.characterId)) continue;
                Color? color = TryGetCharacterColor(b.characterId);
                b.background.color = color ?? b.defaultColor;
            }
        }

        private Color? TryGetCharacterColor(string characterId)
        {
            if (characterPackManager == null || string.IsNullOrEmpty(characterId)) return null;
            try
            {
                var type = characterPackManager.GetType();
                foreach (var name in new[] { "GetCharacterData", "GetCharacter", "GetById" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null) continue;
                    var data = method.Invoke(characterPackManager, new object[] { characterId });
                    if (data == null) continue;

                    var dataType = data.GetType();
                    var field = dataType.GetField("characterColor");
                    if (field != null && field.FieldType == typeof(Color))
                        return (Color)field.GetValue(data);

                    var prop = dataType.GetProperty("characterColor");
                    if (prop != null && prop.PropertyType == typeof(Color))
                        return (Color)prop.GetValue(data, null);
                }
            }
            catch (Exception) { /* swallow */ }
            return null;
        }

        // ---- System button handlers -------------------------------------------------------

        private void OnPauseClicked()
        {
            Debug.Log("[GameHUD] pause clicked.");
            // TODO: Connect PausePopup in a follow-up task.
        }

        private void OnBackToStageSelectClicked()
        {
            Debug.Log("[GameHUD] back-to-stage-select clicked.");
            // TODO: route through a forfeit confirmation popup before navigating.
            if (stageSelectUI != null)
                stageSelectUI.SendMessage("OpenStageSelect", SendMessageOptions.DontRequireReceiver);
        }

        // ---- Button binding helpers -------------------------------------------------------

        private void BindSkillButton(int index)
        {
            if (allSkillBindings == null || index < 0 || index >= allSkillBindings.Length) return;
            var b = allSkillBindings[index];
            if (b == null || b.button == null) return;

            UnityEngine.Events.UnityAction handler = () => OnSkillClicked(b);
            skillClickHandlers[index] = handler;
            b.button.onClick.AddListener(handler);
        }

        private void UnbindSkillButton(int index)
        {
            if (allSkillBindings == null || index < 0 || index >= allSkillBindings.Length) return;
            var b = allSkillBindings[index];
            var handler = skillClickHandlers != null ? skillClickHandlers[index] : null;
            if (b == null || b.button == null || handler == null) return;

            b.button.onClick.RemoveListener(handler);
            skillClickHandlers[index] = null;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.AddListener(handler);
        }

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null || handler == null) return;
            button.onClick.RemoveListener(handler);
        }
    }
}
