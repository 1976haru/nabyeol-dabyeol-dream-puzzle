using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Album
{
    /// <summary>
    /// 모든 AlbumPageData를 보관하는 ScriptableObject 카탈로그.
    /// SparkleAlbumUI가 이 데이터베이스의 pages 목록을 순회해 버튼을 생성한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AlbumDatabase",
        menuName = "NabyeolDabyeol/Album Database",
        order = 131)]
    public class AlbumDatabase : ScriptableObject
    {
        [SerializeField] private List<AlbumPageData> pages = new List<AlbumPageData>();

        public IReadOnlyList<AlbumPageData> Pages => pages;
        public int Count => pages == null ? 0 : pages.Count;

        public AlbumPageData FindByPageId(int pageId)
        {
            if (pages == null) return null;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null && pages[i].PageId == pageId) return pages[i];
            }
            return null;
        }

        public AlbumPageData FindByStageId(int stageId)
        {
            if (pages == null) return null;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null && pages[i].LinkedStageId == stageId) return pages[i];
            }
            return null;
        }

        public bool ValidatePages()
        {
            if (pages == null) return false;
            bool ok = true;
            HashSet<int> seenPageIds = new HashSet<int>();
            HashSet<int> seenStageIds = new HashSet<int>();
            for (int i = 0; i < pages.Count; i++)
            {
                AlbumPageData p = pages[i];
                if (p == null)
                {
                    Debug.LogWarning($"AlbumDatabase: pages[{i}] is null.");
                    ok = false; continue;
                }
                if (!p.IsValid())
                {
                    Debug.LogWarning($"AlbumDatabase: pages[{i}] '{p.name}' failed IsValid().");
                    ok = false;
                }
                if (!seenPageIds.Add(p.PageId))
                {
                    Debug.LogWarning($"AlbumDatabase: Duplicate pageId {p.PageId} at pages[{i}].");
                    ok = false;
                }
                if (!seenStageIds.Add(p.LinkedStageId))
                {
                    Debug.LogWarning($"AlbumDatabase: Duplicate linkedStageId {p.LinkedStageId} at pages[{i}].");
                    ok = false;
                }
            }
            return ok;
        }
    }
}
