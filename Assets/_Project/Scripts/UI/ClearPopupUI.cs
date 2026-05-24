// ClearPopupUI.cs
// Task 95 — Reward-focused stage clear popup.
//
// Design:
//   - BoardManager (or any stage-clear handler) calls ShowClearPopup(stageData,
//     score, remainingMoves) when a stage is cleared. This UI renders title,
//     stage name, score, move bonus, sparkle-piece reward, and an optional
//     knowledge-card reward block.
//   - Sparkle-piece reward = 10 + remainingMoves (+10 for boss stages). This is
//     display-only; an actual sparkle currency store is out of scope for v1.
//   - Knowledge cards are looked up from KnowledgeCardDatabase by reflection
//     using StageData.rewardCardId. If the database or card is missing the
//     card group is hidden — the popup still appears with all other info.
//   - Buttons: Next loads stageId + 1, Retry reloads the current stage,
//     StageSelect opens StageSelectUI. Each is null-safe and logs on failure.
//
// Defensive design:
//   - StageManager / KnowledgeCardDatabase / AlbumProgressManager /
//     LearningCoachAgent / GameHUDUI / StageSelectUI are loose-coupled via
//     MonoBehaviour fields and accessed by reflection.
//   - StageData fields (stageId, stageName, rewardCardId, isBossStage) are read
//     by reflection so this UI does not need to know the concrete data class.
//   - Every UI reference is optional. Missing pieces only suppress that one
//     row — they never throw.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Sound;
using NabyeolDabyeolDreamPuzzle.Animation;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    public class ClearPopupUI : MonoBehaviour
    {
        // ---- Panel ------------------------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject clearPopup;

        // ---- Header / summary -------------------------------------------------------------

        [Header("Header / summary")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI moveBonusText;

        [Tooltip("Title shown for regular stages.")]
        [SerializeField] private string normalTitle = "스테이지 클리어!";
        [Tooltip("Title shown when StageData.isBossStage is true.")]
        [SerializeField] private string bossTitle = "보스 스테이지 클리어!";

        // ---- Sparkle reward ---------------------------------------------------------------

        [Header("Sparkle piece reward")]
        [SerializeField] private GameObject sparklePieceGroup;
        [SerializeField] private Image sparkleIcon;
        [SerializeField] private TextMeshProUGUI sparklePieceText;

        [Tooltip("Base sparkle pieces granted just for clearing the stage.")]
        [SerializeField, Min(0)] private int baseSparkleReward = 10;
        [Tooltip("Extra sparkle pieces granted when StageData.isBossStage is true.")]
        [SerializeField, Min(0)] private int bossSparkleBonus = 10;

        // ---- Knowledge card reward --------------------------------------------------------

        [Header("Knowledge card reward")]
        [SerializeField] private GameObject knowledgeCardGroup;
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardShortText;
        [SerializeField] private TextMeshProUGUI cardRarityText;
        [SerializeField] private TextMeshProUGUI noCardText;

        [Tooltip("Message shown when no rewardCardId is available.")]
        [SerializeField] private string noCardMessage = "새로운 지식카드는 다음에 만나요!";

        // ---- Album unlock -----------------------------------------------------------------

        [Header("Album unlock")]
        [SerializeField] private GameObject albumUnlockGroup;
        [SerializeField] private TextMeshProUGUI albumUnlockText;
        [SerializeField] private string albumUnlockMessage = "반짝 앨범에 새 장면이 저장됐어요!";

        // ---- Buttons ----------------------------------------------------------------------

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button stageSelectButton;

        // ---- External managers / UIs (loose coupling) -------------------------------------

        [Header("External managers (optional)")]
        [SerializeField] private MonoBehaviour stageManager;
        [SerializeField] private MonoBehaviour knowledgeCardDatabase;
        [SerializeField] private MonoBehaviour albumProgressManager;
        [SerializeField] private MonoBehaviour learningCoachAgent;

        [Header("External UIs (optional)")]
        [SerializeField] private MonoBehaviour stageSelectUI;
        [SerializeField] private MonoBehaviour gameHUDUI;

        // ---- Runtime ----------------------------------------------------------------------

        private int currentStageId;
        private bool isShowing;

        // ---- Lifecycle --------------------------------------------------------------------

        private void Awake()
        {
            if (clearPopup != null) clearPopup.SetActive(false);
        }

        private void OnEnable()
        {
            BindButton(nextButton,        OnNextClicked);
            BindButton(retryButton,       OnRetryClicked);
            BindButton(stageSelectButton, OnStageSelectClicked);
        }

        private void OnDisable()
        {
            UnbindButton(nextButton,        OnNextClicked);
            UnbindButton(retryButton,       OnRetryClicked);
            UnbindButton(stageSelectButton, OnStageSelectClicked);
        }

        // ---- Public API -------------------------------------------------------------------

        /// <summary>
        /// Show the clear popup with reward data. Safe to call with stageData == null
        /// — the popup still appears with fallback text. Duplicate calls are filtered
        /// while the popup is already visible.
        /// </summary>
        public void ShowClearPopup(object stageData, int score, int remainingMoves)
        {
            if (isShowing)
            {
                Debug.Log("[ClearPopup] already showing — duplicate ShowClearPopup ignored.");
                return;
            }
            isShowing = true;

            if (clearPopup != null) clearPopup.SetActive(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SfxType.PopupOpen);
                SoundManager.Instance.PlaySfx(SfxType.Clear);
                SoundManager.Instance.PlaySfx(SfxType.Reward);
            }

            int    stageId    = TryGetIntMember(stageData, "stageId", 0);
            string stageName  = TryGetStringMember(stageData, "stageName");
            bool   isBoss     = TryGetBoolMember(stageData, "isBossStage", false);
            string rewardId   = TryGetStringMember(stageData, "rewardCardId");

            currentStageId = stageId;

            // ---- Header ----
            if (titleText != null)
                titleText.text = isBoss ? bossTitle : normalTitle;
            if (stageNameText != null)
                stageNameText.text = string.IsNullOrEmpty(stageName)
                    ? (stageId > 0 ? $"Stage {stageId}" : string.Empty)
                    : stageName;
            if (scoreText != null)
                scoreText.text = $"점수 {Mathf.Max(0, score):N0}";
            if (moveBonusText != null)
                moveBonusText.text = $"남은 이동 {Mathf.Max(0, remainingMoves)}";

            // ---- Sparkle reward ----
            int sparkleAmount = CalculateSparkleReward(remainingMoves, isBoss);
            ApplySparkleReward(sparkleAmount);

            // ---- Knowledge card ----
            ApplyKnowledgeCard(rewardId);

            // ---- Album unlock notice ----
            ApplyAlbumUnlockMessage(stageId);

            // ---- LearningCoachAgent record ----
            TryRecordCardEarnedToday(rewardId);

            Debug.Log(
                $"[ClearPopup] shown stageId={stageId} boss={isBoss} score={score} " +
                $"remainingMoves={remainingMoves} sparkle={sparkleAmount} cardId='{rewardId ?? string.Empty}'.");

            PlayShowAnimations();

            // TODO: Add sparkle particle effect.
            // TODO: Add card flip animation.
        }

        private void PlayShowAnimations()
        {
            SimpleAnimationManager mgr = SimpleAnimationManager.Instance;
            if (mgr == null || !mgr.AnimationsEnabled) return;

            if (clearPopup != null)
            {
                RectTransform panelRect = clearPopup.transform as RectTransform;
                if (panelRect != null) StartCoroutine(mgr.PlayPopupOpen(panelRect));
            }
            if (sparklePieceGroup != null && sparklePieceGroup.activeInHierarchy)
            {
                RectTransform rect = sparklePieceGroup.transform as RectTransform;
                if (rect != null) StartCoroutine(mgr.PlayUIPulse(rect));
            }
            if (knowledgeCardGroup != null && knowledgeCardGroup.activeInHierarchy)
            {
                RectTransform rect = knowledgeCardGroup.transform as RectTransform;
                if (rect != null) StartCoroutine(mgr.PlayUIPulse(rect));
            }
        }

        /// <summary>Hide the clear popup. Safe to call when already hidden.</summary>
        public void HideClearPopup()
        {
            bool wasShowing = isShowing;
            if (clearPopup != null) clearPopup.SetActive(false);
            isShowing = false;
            if (wasShowing && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(SfxType.PopupClose);
            }
            Debug.Log("[ClearPopup] hidden.");
        }

        // ---- Sparkle reward ---------------------------------------------------------------

        private int CalculateSparkleReward(int remainingMoves, bool isBoss)
        {
            int amount = baseSparkleReward + Mathf.Max(0, remainingMoves);
            if (isBoss) amount += bossSparkleBonus;
            return amount;
        }

        private void ApplySparkleReward(int amount)
        {
            if (sparklePieceGroup != null) sparklePieceGroup.SetActive(true);
            if (sparklePieceText  != null) sparklePieceText.text = $"반짝 조각 +{amount}";
            // sparkleIcon is optional — nothing to do when null.
        }

        // ---- Knowledge card ---------------------------------------------------------------

        private void ApplyKnowledgeCard(string rewardCardId)
        {
            if (string.IsNullOrEmpty(rewardCardId))
            {
                HideKnowledgeCardWithMessage();
                Debug.LogWarning("[ClearPopup] rewardCardId is empty — knowledge card group hidden.");
                return;
            }

            object cardData = TryGetKnowledgeCardData(rewardCardId);
            if (cardData == null)
            {
                HideKnowledgeCardWithMessage();
                Debug.LogWarning($"[ClearPopup] KnowledgeCardData not found for id='{rewardCardId}'.");
                return;
            }

            if (knowledgeCardGroup != null) knowledgeCardGroup.SetActive(true);
            if (noCardText != null) noCardText.gameObject.SetActive(false);

            string cardName  = TryGetStringMember(cardData, "cardName")
                            ?? TryGetStringMember(cardData, "name");
            string shortText = TryGetStringMember(cardData, "shortText")
                            ?? TryGetStringMember(cardData, "description");
            object rarityObj = TryGetMember(cardData, "rarity");
            Sprite sprite    = TryGetMember(cardData, "image") as Sprite
                            ?? TryGetMember(cardData, "cardImage") as Sprite
                            ?? TryGetMember(cardData, "sprite") as Sprite;

            if (cardNameText != null)
                cardNameText.text = string.IsNullOrEmpty(cardName) ? rewardCardId : cardName;

            if (cardShortText != null)
                cardShortText.text = string.IsNullOrEmpty(shortText) ? string.Empty : shortText;

            if (cardRarityText != null)
                cardRarityText.text = GetRarityText(rarityObj);

            ApplyCardImage(sprite);
        }

        private void ApplyCardImage(Sprite sprite)
        {
            if (cardImage == null) return;
            if (sprite != null)
            {
                cardImage.sprite = sprite;
                cardImage.enabled = true;
            }
            // If sprite is null, leave whatever default the prefab has. Do not
            // null out a placeholder sprite — that would show a magenta box.
        }

        private void HideKnowledgeCardWithMessage()
        {
            if (noCardText != null)
            {
                if (knowledgeCardGroup != null) knowledgeCardGroup.SetActive(true);
                noCardText.gameObject.SetActive(true);
                noCardText.text = noCardMessage;
                if (cardNameText   != null) cardNameText.text   = string.Empty;
                if (cardShortText  != null) cardShortText.text  = string.Empty;
                if (cardRarityText != null) cardRarityText.text = string.Empty;
                if (cardImage      != null) cardImage.enabled   = false;
                return;
            }
            if (knowledgeCardGroup != null) knowledgeCardGroup.SetActive(false);
        }

        private object TryGetKnowledgeCardData(string cardId)
        {
            if (knowledgeCardDatabase == null || string.IsNullOrEmpty(cardId)) return null;
            try
            {
                var type = knowledgeCardDatabase.GetType();
                foreach (var name in new[] { "GetCardById", "GetCard", "GetCardData", "FindCard" })
                {
                    var method = type.GetMethod(name, new[] { typeof(string) });
                    if (method == null) continue;
                    return method.Invoke(knowledgeCardDatabase, new object[] { cardId });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClearPopup] knowledge card lookup failed for '{cardId}': {e.Message}");
            }
            return null;
        }

        private string GetRarityText(object rarity)
        {
            if (rarity == null) return string.Empty;
            string s = rarity.ToString();
            switch (s)
            {
                case "Common": return "기본 카드";
                case "Rare":   return "반짝 카드";
                case "Epic":   return "특별 카드";
                default:       return s;
            }
        }

        // ---- Album unlock notice ----------------------------------------------------------

        private void ApplyAlbumUnlockMessage(int stageId)
        {
            bool hasAlbum = albumProgressManager != null;
            if (albumUnlockGroup != null) albumUnlockGroup.SetActive(hasAlbum);
            if (!hasAlbum) return;

            if (albumUnlockText != null) albumUnlockText.text = albumUnlockMessage;

            // AlbumProgressManager is expected to already unlock the page from the
            // clear flow; we only render the notice here. If it has not been wired
            // yet but does expose an UnlockPageByStageId(int) helper, attempt it
            // defensively so the message remains truthful.
            if (stageId <= 0) return;
            try
            {
                var method = albumProgressManager.GetType().GetMethod(
                    "UnlockPageByStageId", new[] { typeof(int) });
                if (method != null)
                {
                    method.Invoke(albumProgressManager, new object[] { stageId });
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[ClearPopup] AlbumProgressManager.UnlockPageByStageId skipped: {e.Message}");
            }
        }

        // ---- LearningCoachAgent record ----------------------------------------------------

        private void TryRecordCardEarnedToday(string cardId)
        {
            if (learningCoachAgent == null || string.IsNullOrEmpty(cardId)) return;
            try
            {
                var method = learningCoachAgent.GetType().GetMethod(
                    "RecordCardEarnedToday", new[] { typeof(string) });
                if (method == null)
                {
                    Debug.Log("[ClearPopup] LearningCoachAgent.RecordCardEarnedToday(string) not found — skipped.");
                    return;
                }
                method.Invoke(learningCoachAgent, new object[] { cardId });
                Debug.Log($"[ClearPopup] LearningCoachAgent recorded card '{cardId}'.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClearPopup] LearningCoachAgent record failed: {e.Message}");
            }
        }

        // ---- Button handlers --------------------------------------------------------------

        private void OnNextClicked()
        {
            int nextId = ResolveNextStageId();
            if (nextId <= 0)
            {
                Debug.Log("[ClearPopup] next stage not available — button click ignored.");
                if (nextButton != null) nextButton.interactable = false;
                return;
            }
            if (!TryInvokeStageLoad(nextId))
            {
                Debug.LogWarning($"[ClearPopup] LoadStageById({nextId}) failed — 다음 스테이지 준비 중.");
                return;
            }
            Debug.Log($"[ClearPopup] next stage loaded: {nextId}.");
            HideClearPopup();
        }

        private void OnRetryClicked()
        {
            int retryId = ResolveCurrentStageId();
            if (retryId <= 0)
            {
                Debug.LogWarning("[ClearPopup] retry clicked but currentStageId unknown.");
                return;
            }
            if (!TryInvokeStageLoad(retryId))
            {
                Debug.LogWarning($"[ClearPopup] retry: LoadStageById({retryId}) failed.");
                return;
            }
            Debug.Log($"[ClearPopup] retry loaded stage {retryId}.");
            HideClearPopup();
        }

        private void OnStageSelectClicked()
        {
            if (stageSelectUI == null)
            {
                Debug.LogWarning("[ClearPopup] stageSelectUI not assigned — ignoring stage select.");
                HideClearPopup();
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
                Debug.LogWarning($"[ClearPopup] OpenStageSelect failed: {e.Message}");
            }
            TryHideHUD();
            HideClearPopup();
        }

        // ---- Stage id resolution ----------------------------------------------------------

        private int ResolveCurrentStageId()
        {
            if (currentStageId > 0) return currentStageId;
            object data = TryGetCurrentStageData();
            return TryGetIntMember(data, "stageId", 0);
        }

        private int ResolveNextStageId()
        {
            int current = ResolveCurrentStageId();
            return current > 0 ? current + 1 : 0;
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
                Debug.LogWarning($"[ClearPopup] CurrentStageData lookup failed: {e.Message}");
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
                    Debug.LogWarning("[ClearPopup] StageManager.LoadStageById(int) not found.");
                    return false;
                }
                method.Invoke(stageManager, new object[] { stageId });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClearPopup] LoadStageById failed: {e.Message}");
                return false;
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
                Debug.Log($"[ClearPopup] HideHUD skipped: {e.Message}");
            }
        }

        // ---- Reflection helpers -----------------------------------------------------------

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

        private static bool TryGetBoolMember(object data, string memberName, bool fallback)
        {
            object value = TryGetMember(data, memberName);
            if (value is bool b) return b;
            return fallback;
        }

        // ---- Tiny UI helpers --------------------------------------------------------------

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

        // ---- Integration notes ------------------------------------------------------------
        //
        // BoardManager.ShowClearPopup integration pattern:
        //
        //     [SerializeField] private ClearPopupUI clearPopupUI;
        //     [SerializeField] private GameObject clearPopup; // legacy fallback
        //
        //     private void ShowClearPopup()
        //     {
        //         if (isStageCleared) return;
        //         isStageCleared = true;
        //
        //         if (clearPopupUI != null && StageManager.Instance != null)
        //         {
        //             clearPopupUI.ShowClearPopup(
        //                 StageManager.Instance.CurrentStageData,
        //                 currentScore,
        //                 remainingMoves);
        //         }
        //         else if (clearPopup != null)
        //         {
        //             clearPopup.SetActive(true);
        //         }
        //     }
        //
        // The legacy fallback path is preserved so existing scenes that have
        // only the raw GameObject clearPopup wired up keep working.
    }
}
