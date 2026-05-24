using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Album
{
    /// <summary>
    /// 반짝 앨범의 한 페이지 데이터. 스테이지 클리어 시 되찾은 장면을 그림책처럼 보관한다.
    /// 실제 그림은 pageImage 슬롯에 후속 작업에서 연결한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AlbumPage",
        menuName = "NabyeolDabyeol/Album Page",
        order = 130)]
    public class AlbumPageData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int pageId = 1;
        [SerializeField, Min(1)] private int linkedStageId = 1;
        [SerializeField] private string worldName;

        [Header("Content")]
        [SerializeField] private string pageTitle;
        [TextArea(2, 4)]
        [SerializeField] private string pageDescription;
        [SerializeField] private string linkedCardId;
        [SerializeField] private Sprite pageImage;

        public int PageId => pageId;
        public int LinkedStageId => linkedStageId;
        public string WorldName => worldName;
        public string PageTitle => pageTitle;
        public string PageDescription => pageDescription;
        public string LinkedCardId => linkedCardId;
        public Sprite PageImage => pageImage;

        public bool IsValid()
        {
            if (pageId <= 0) return false;
            if (linkedStageId <= 0) return false;
            if (string.IsNullOrWhiteSpace(pageTitle)) return false;
            if (string.IsNullOrWhiteSpace(linkedCardId)) return false;
            return true;
        }
    }
}
