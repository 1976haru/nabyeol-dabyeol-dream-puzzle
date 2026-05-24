using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.Region;
using NabyeolDabyeolDreamPuzzle.Animation;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 지역 복구 상태 표시 UI. 외부 메뉴 버튼이 OpenRegionRestore(region) 또는 OpenRegionById(regionId)로 호출.
    /// RegionRestoreManager의 OnRegionRestoreUpdated 이벤트를 구독해 열려 있는 동안 자동 갱신.
    /// </summary>
    public class RegionRestoreUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject restorePanel;
        [SerializeField] private Button closeButton;

        [Header("Region Display")]
        [SerializeField] private TextMeshProUGUI regionNameText;
        [SerializeField] private TextMeshProUGUI restorePercentText;
        [SerializeField] private TextMeshProUGUI restoreDescriptionText;
        [SerializeField] private Image restoreImage;
        [SerializeField] private Slider restoreSlider;

        [Header("Format")]
        [SerializeField] private string percentFormat = "복구율 {0}%";

        private RestoreRegionData currentRegion;

        private void Awake()
        {
            if (restorePanel != null) restorePanel.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseRegionRestore);
                closeButton.onClick.AddListener(CloseRegionRestore);
            }
        }

        private void OnEnable()
        {
            if (RegionRestoreManager.Instance != null)
            {
                RegionRestoreManager.Instance.OnRegionRestoreUpdated += HandleRegionRestoreUpdated;
            }
        }

        private void OnDisable()
        {
            if (RegionRestoreManager.Instance != null)
            {
                RegionRestoreManager.Instance.OnRegionRestoreUpdated -= HandleRegionRestoreUpdated;
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseRegionRestore);
        }

        public void OpenRegionRestore(RestoreRegionData region)
        {
            if (region == null)
            {
                Debug.LogWarning("RegionRestoreUI: OpenRegionRestore called with null region.");
                return;
            }
            currentRegion = region;
            if (restorePanel != null) restorePanel.SetActive(true);
            RefreshDisplay();
            Debug.Log($"RegionRestoreUI: Opened region '{region.RegionId}'.");
        }

        public void OpenRegionById(string regionId)
        {
            if (RegionRestoreManager.Instance == null)
            {
                Debug.LogWarning("RegionRestoreUI: RegionRestoreManager.Instance not found.");
                return;
            }
            RestoreRegionData region = RegionRestoreManager.Instance.GetRegionById(regionId);
            if (region == null)
            {
                Debug.LogWarning($"RegionRestoreUI: Region not found by id '{regionId}'.");
                return;
            }
            OpenRegionRestore(region);
        }

        public void CloseRegionRestore()
        {
            if (restorePanel != null) restorePanel.SetActive(false);
            currentRegion = null;
            Debug.Log("RegionRestoreUI: Closed.");
        }

        private void RefreshDisplay()
        {
            if (currentRegion == null) return;

            int percent = 0;
            if (RegionRestoreManager.Instance != null)
            {
                percent = RegionRestoreManager.Instance.GetSteppedPercent(currentRegion);
            }
            else
            {
                Debug.LogWarning("RegionRestoreUI: RegionRestoreManager.Instance not found; defaulting to 0%.");
            }

            if (regionNameText != null) regionNameText.text = currentRegion.RegionName ?? string.Empty;
            if (restorePercentText != null) restorePercentText.text = string.Format(percentFormat, percent);
            if (restoreDescriptionText != null) restoreDescriptionText.text = currentRegion.GetDescriptionByPercent(percent) ?? string.Empty;

            if (restoreImage != null)
            {
                Sprite s = currentRegion.GetSpriteByPercent(percent);
                if (s != null)
                {
                    bool sameSprite = restoreImage.sprite == s;
                    restoreImage.enabled = true;

                    if (sameSprite || SimpleAnimationManager.Instance == null || !SimpleAnimationManager.Instance.AnimationsEnabled)
                    {
                        restoreImage.sprite = s;
                    }
                    else
                    {
                        StartCoroutine(SimpleAnimationManager.Instance.FadeSwapSprite(restoreImage, s));
                    }
                }
                else
                {
                    Debug.LogWarning($"RegionRestoreUI: No sprite assigned for region '{currentRegion.RegionId}' at {percent}%. Keeping previous image.");
                }
            }

            if (restoreSlider != null)
            {
                restoreSlider.minValue = 0f;
                restoreSlider.maxValue = 1f;
                restoreSlider.value = percent / 100f;
            }
        }

        private void HandleRegionRestoreUpdated(string regionId, int beforePercent, int afterPercent)
        {
            if (currentRegion == null) return;
            if (currentRegion.RegionId != regionId) return;
            RefreshDisplay();
        }
    }
}
