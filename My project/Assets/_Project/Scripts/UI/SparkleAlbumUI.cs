using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Album;
using NabyeolDabyeolDreamPuzzle.Cards;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 반짝 앨범 UI 컨트롤러. AlbumDatabase의 페이지 목록을 버튼 그리드로 보여주고,
    /// 선택된 페이지의 상세(잠금 상태 반영)를 표시한다.
    /// KnowledgeCardDatabase가 연결되어 있으면 linkedCardId로 shortText까지 함께 표시.
    /// </summary>
    public class SparkleAlbumUI : MonoBehaviour
    {
        [Header("Database References")]
        [SerializeField] private AlbumDatabase albumDatabase;
        [SerializeField] private KnowledgeCardDatabase knowledgeCardDatabase;

        [Header("Panel")]
        [SerializeField] private GameObject albumPanel;
        [SerializeField] private TextMeshProUGUI albumTitleText;
        [SerializeField] private Button closeButton;

        [Header("Page List")]
        [SerializeField] private Transform pageListContent;
        [SerializeField] private GameObject pageButtonPrefab;

        [Header("Page Detail")]
        [SerializeField] private TextMeshProUGUI detailTitleText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailStageText;
        [SerializeField] private TextMeshProUGUI detailCardSnippetText;
        [SerializeField] private Image detailPageImage;
        [SerializeField] private GameObject lockedOverlay;

        [Header("Text Presets")]
        [SerializeField] private string albumTitle = "반짝 앨범";
        [SerializeField] private string lockedTitle = "아직 잠긴 장면";
        [TextArea(2, 4)]
        [SerializeField] private string lockedDescription = "스테이지를 클리어하면 열려요.";
        [SerializeField] private string lockedListLabel = "???";
        [SerializeField] private string cardSnippetPrefix = "오늘의 반짝 지식: ";

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            if (albumPanel != null) albumPanel.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseAlbum);
                closeButton.onClick.AddListener(CloseAlbum);
            }
            if (albumTitleText != null)
            {
                albumTitleText.text = albumTitle;
            }
        }

        private void OnEnable()
        {
            if (AlbumProgressManager.Instance != null)
            {
                AlbumProgressManager.Instance.OnPageUnlocked += HandlePageUnlocked;
            }
        }

        private void OnDisable()
        {
            if (AlbumProgressManager.Instance != null)
            {
                AlbumProgressManager.Instance.OnPageUnlocked -= HandlePageUnlocked;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseAlbum);
            }
        }

        /// <summary>외부 메뉴 버튼 등에서 호출. 앨범 패널을 열고 목록을 새로 빌드한다.</summary>
        public void OpenAlbum()
        {
            if (albumPanel == null)
            {
                Debug.LogWarning("SparkleAlbumUI: albumPanel is not assigned.");
                return;
            }
            albumPanel.SetActive(true);
            BuildPageList();
            ClearDetail();
            Debug.Log("SparkleAlbumUI: Album opened.");
        }

        /// <summary>앨범 패널을 닫는다.</summary>
        public void CloseAlbum()
        {
            if (albumPanel != null) albumPanel.SetActive(false);
            Debug.Log("SparkleAlbumUI: Album closed.");
        }

        private void BuildPageList()
        {
            // 기존 버튼 제거
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null) Destroy(spawnedButtons[i]);
            }
            spawnedButtons.Clear();

            if (albumDatabase == null)
            {
                Debug.LogWarning("SparkleAlbumUI: AlbumDatabase is not assigned.");
                return;
            }
            if (pageListContent == null)
            {
                Debug.LogWarning("SparkleAlbumUI: pageListContent is not assigned.");
                return;
            }
            if (pageButtonPrefab == null)
            {
                Debug.LogWarning("SparkleAlbumUI: pageButtonPrefab is not assigned.");
                return;
            }

            for (int i = 0; i < albumDatabase.Pages.Count; i++)
            {
                AlbumPageData page = albumDatabase.Pages[i];
                if (page == null) continue;

                GameObject buttonGO = Instantiate(pageButtonPrefab, pageListContent);
                spawnedButtons.Add(buttonGO);

                bool unlocked = IsPageUnlocked(page.LinkedStageId);
                Button btn = buttonGO.GetComponent<Button>();
                TextMeshProUGUI label = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    label.text = unlocked ? page.PageTitle : lockedListLabel;
                }
                if (btn != null)
                {
                    AlbumPageData captured = page;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectPage(captured));
                }
            }
        }

        private void SelectPage(AlbumPageData page)
        {
            if (page == null) return;
            bool unlocked = IsPageUnlocked(page.LinkedStageId);

            if (detailTitleText != null)
            {
                detailTitleText.text = unlocked ? page.PageTitle : lockedTitle;
            }
            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = unlocked ? page.PageDescription : lockedDescription;
            }
            if (detailStageText != null)
            {
                detailStageText.text = unlocked
                    ? $"{(string.IsNullOrEmpty(page.WorldName) ? "" : page.WorldName + " · ")}Stage {page.LinkedStageId}"
                    : string.Empty;
            }
            if (detailCardSnippetText != null)
            {
                detailCardSnippetText.text = unlocked ? BuildCardSnippet(page.LinkedCardId) : string.Empty;
            }
            if (detailPageImage != null)
            {
                if (unlocked && page.PageImage != null)
                {
                    detailPageImage.sprite = page.PageImage;
                    detailPageImage.enabled = true;
                }
                else if (page.PageImage == null)
                {
                    // page image가 없을 때는 기존 sprite 유지 (변경 안 함).
                }
            }
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!unlocked);
            }

            Debug.Log($"SparkleAlbumUI: Selected page {page.PageId} (stageId={page.LinkedStageId}, unlocked={unlocked}).");
        }

        private void ClearDetail()
        {
            if (detailTitleText != null) detailTitleText.text = string.Empty;
            if (detailDescriptionText != null) detailDescriptionText.text = string.Empty;
            if (detailStageText != null) detailStageText.text = string.Empty;
            if (detailCardSnippetText != null) detailCardSnippetText.text = string.Empty;
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
        }

        private string BuildCardSnippet(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return string.Empty;
            if (knowledgeCardDatabase == null)
            {
                Debug.LogWarning("SparkleAlbumUI: KnowledgeCardDatabase is not assigned. Card snippet omitted.");
                return string.Empty;
            }
            KnowledgeCardData card = knowledgeCardDatabase.FindByCardId(cardId);
            if (card == null)
            {
                Debug.LogWarning($"SparkleAlbumUI: Card not found in database. cardId={cardId}");
                return string.Empty;
            }
            return cardSnippetPrefix + card.ShortText;
        }

        private bool IsPageUnlocked(int linkedStageId)
        {
            if (AlbumProgressManager.Instance == null) return false;
            return AlbumProgressManager.Instance.IsPageUnlocked(linkedStageId);
        }

        private void HandlePageUnlocked(int stageId)
        {
            // 앨범이 열려 있는 동안 다른 경로로 해금되면 목록을 갱신한다.
            if (albumPanel != null && albumPanel.activeSelf)
            {
                BuildPageList();
            }
        }
    }
}
