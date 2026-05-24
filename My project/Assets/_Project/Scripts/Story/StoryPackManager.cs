using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Story
{
    /// <summary>
    /// StoryPackDatabase에 대한 런타임 접근점.
    /// StoryManager가 자체 storyNodes 목록에서 못 찾은 경우 fallback으로 호출한다.
    /// </summary>
    public class StoryPackManager : MonoBehaviour
    {
        public static StoryPackManager Instance { get; private set; }

        [SerializeField] private StoryPackDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("StoryPackManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (database != null)
            {
                bool ok = database.ValidatePacks();
                Debug.Log($"StoryPackManager: ValidatePacks = {ok} (count={database.Count}).");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public StoryPackData GetPackById(string packId)
        {
            if (database == null) return null;
            return database.FindByPackId(packId);
        }

        public StoryPackData GetPackByStageId(int stageId)
        {
            if (database == null) return null;
            return database.FindPackByStageId(stageId);
        }

        public StoryNode GetStoryNodeByStageId(int stageId)
        {
            if (database == null) return null;
            return database.FindStoryNodeByStageId(stageId);
        }

        public StoryEventTemplate GetEventByEventId(string eventId)
        {
            if (database == null) return null;
            return database.FindEventByEventId(eventId);
        }

        public StoryPackDatabase Database => database;
    }
}
