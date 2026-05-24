using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Album
{
    /// <summary>
    /// 반짝 앨범의 페이지별 해금 상태를 관리하는 매니저.
    /// linkedStageId 기준으로 PlayerPrefs에 unlock 상태를 저장한다.
    /// 스테이지 클리어 시 BoardManager가 UnlockPageByStageId를 호출한다.
    /// TODO: Save album progress with a proper save system instead of PlayerPrefs.
    /// </summary>
    public class AlbumProgressManager : MonoBehaviour
    {
        public static AlbumProgressManager Instance { get; private set; }

        [Header("Database (optional, for UnlockAll debug)")]
        [SerializeField] private AlbumDatabase albumDatabase;

        // PlayerPrefs 키 prefix. linkedStageId를 붙여 페이지별 독립 저장.
        private const string KeyPrefix = "AlbumPageUnlocked_";

        public event System.Action<int> OnPageUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("AlbumProgressManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsPageUnlocked(int linkedStageId)
        {
            if (linkedStageId <= 0) return false;
            return PlayerPrefs.GetInt(KeyPrefix + linkedStageId, 0) == 1;
        }

        public void UnlockPageByStageId(int linkedStageId)
        {
            if (linkedStageId <= 0) return;
            if (IsPageUnlocked(linkedStageId))
            {
                Debug.Log($"AlbumProgressManager: Album page already unlocked. stageId={linkedStageId}");
                return;
            }
            PlayerPrefs.SetInt(KeyPrefix + linkedStageId, 1);
            PlayerPrefs.Save();
            Debug.Log($"AlbumProgressManager: Album page unlocked. stageId={linkedStageId}");
            OnPageUnlocked?.Invoke(linkedStageId);
        }

        [ContextMenu("Unlock All Album Pages")]
        public void UnlockAllAlbumPagesForDebug()
        {
            if (albumDatabase == null)
            {
                Debug.LogWarning("AlbumProgressManager: AlbumDatabase reference is missing. Cannot unlock all.");
                return;
            }
            int unlockedCount = 0;
            for (int i = 0; i < albumDatabase.Pages.Count; i++)
            {
                AlbumPageData p = albumDatabase.Pages[i];
                if (p == null) continue;
                if (!IsPageUnlocked(p.LinkedStageId))
                {
                    PlayerPrefs.SetInt(KeyPrefix + p.LinkedStageId, 1);
                    OnPageUnlocked?.Invoke(p.LinkedStageId);
                    unlockedCount++;
                }
            }
            PlayerPrefs.Save();
            Debug.Log($"AlbumProgressManager: Unlocked all album pages (new unlocks: {unlockedCount}).");
        }

        [ContextMenu("Reset Album Progress")]
        public void ResetAlbumProgress()
        {
            if (albumDatabase == null)
            {
                Debug.LogWarning("AlbumProgressManager: AlbumDatabase reference is missing. Resetting blindly may leave keys behind.");
            }
            if (albumDatabase != null)
            {
                for (int i = 0; i < albumDatabase.Pages.Count; i++)
                {
                    AlbumPageData p = albumDatabase.Pages[i];
                    if (p == null) continue;
                    PlayerPrefs.DeleteKey(KeyPrefix + p.LinkedStageId);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("AlbumProgressManager: Album progress reset.");
        }
    }
}
