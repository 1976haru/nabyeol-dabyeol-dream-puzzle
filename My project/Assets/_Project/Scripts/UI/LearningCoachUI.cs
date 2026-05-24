using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 오늘 얻은 지식카드 최대 3장을 짧게 보여주는 학습 코치 패널.
    /// - 0장: 안내 문구 표시 + 카드 슬롯 숨김
    /// - 1~3장: 있는 만큼만 표시, 나머지는 빈 문자열 + GameObject 비활성화
    /// - 새로고침: LearningCoachAgent로부터 다시 읽어 표시
    /// </summary>
    public class LearningCoachUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject coachPanel;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI emptyText;

        [Header("Card Slots")]
        [SerializeField] private TextMeshProUGUI card1Text;
        [SerializeField] private TextMeshProUGUI card2Text;
        [SerializeField] private TextMeshProUGUI card3Text;

        [Header("Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;

        [Header("Strings")]
        [SerializeField] private string titleWithCardsString = "오늘의 반짝 지식";
        [SerializeField] private string titleEmptyString = "오늘의 반짝 지식";
        [TextArea(2, 4)]
        [SerializeField] private string emptyMessageString = "오늘은 아직 얻은 지식카드가 없어요. 스테이지를 클리어하면 여기에 모여요!";
        [TextArea(1, 3)]
        [SerializeField] private string summaryEncouragement = "오늘도 새로운 것을 배웠어요!";
        [TextArea(1, 3)]
        [SerializeField] private string summaryEncouragementWhenEmpty = "내일도 반짝 지식을 모아봐요!";
        [SerializeField] private string cardLineFormat = "{0}. {1}\n   {2}"; // index, cardName, shortText

        private void Awake()
        {
            if (coachPanel != null) coachPanel.SetActive(false);

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(RefreshSummary);
                refreshButton.onClick.AddListener(RefreshSummary);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseLearningCoach);
                closeButton.onClick.AddListener(CloseLearningCoach);
            }
        }

        private void OnDestroy()
        {
            if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshSummary);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseLearningCoach);
        }

        public void OpenLearningCoach()
        {
            if (coachPanel != null) coachPanel.SetActive(true);
            Debug.Log("LearningCoachUI: Coach panel opened.");
            RefreshSummary();
        }

        public void CloseLearningCoach()
        {
            if (coachPanel != null) coachPanel.SetActive(false);
            Debug.Log("LearningCoachUI: Coach panel closed.");
        }

        public void RefreshSummary()
        {
            if (LearningCoachAgent.Instance == null)
            {
                Debug.LogWarning("LearningCoachUI: LearningCoachAgent.Instance not found.");
                ShowEmptyState();
                return;
            }

            List<KnowledgeCardData> cards = LearningCoachAgent.Instance.GetTodaySummaryCards();
            if (cards == null || cards.Count == 0)
            {
                ShowEmptyState();
                Debug.Log("LearningCoachUI: No cards today.");
                return;
            }

            if (emptyText != null) emptyText.gameObject.SetActive(false);
            if (titleText != null) titleText.text = titleWithCardsString;

            ApplyCardSlot(card1Text, cards, 0);
            ApplyCardSlot(card2Text, cards, 1);
            ApplyCardSlot(card3Text, cards, 2);

            if (summaryText != null) summaryText.text = summaryEncouragement;
            Debug.Log($"LearningCoachUI: Refreshed with {cards.Count} card(s).");
        }

        private void ShowEmptyState()
        {
            if (titleText != null) titleText.text = titleEmptyString;
            if (emptyText != null)
            {
                emptyText.text = emptyMessageString;
                emptyText.gameObject.SetActive(true);
            }
            // 카드 슬롯 비우기 + 숨기기
            HideCardSlot(card1Text);
            HideCardSlot(card2Text);
            HideCardSlot(card3Text);
            if (summaryText != null) summaryText.text = summaryEncouragementWhenEmpty;
        }

        private void ApplyCardSlot(TextMeshProUGUI slot, List<KnowledgeCardData> cards, int index)
        {
            if (slot == null) return;
            if (index < 0 || index >= cards.Count)
            {
                HideCardSlot(slot);
                return;
            }
            KnowledgeCardData c = cards[index];
            if (c == null)
            {
                HideCardSlot(slot);
                return;
            }
            string title = !string.IsNullOrWhiteSpace(c.CardName) ? c.CardName : c.CardId;
            string body = !string.IsNullOrWhiteSpace(c.ShortText) ? c.ShortText : string.Empty;
            slot.text = string.Format(cardLineFormat, index + 1, title, body);
            slot.gameObject.SetActive(true);
        }

        private void HideCardSlot(TextMeshProUGUI slot)
        {
            if (slot == null) return;
            slot.text = string.Empty;
            slot.gameObject.SetActive(false);
        }
    }
}
