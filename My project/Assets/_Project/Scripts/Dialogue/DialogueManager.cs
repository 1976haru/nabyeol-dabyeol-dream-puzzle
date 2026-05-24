using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Dialogue
{
    /// <summary>
    /// DialogueDatabase에 대한 런타임 접근점.
    /// Singleton + static Get() 헬퍼로 어디서든 key 기반 대사 조회 가능.
    /// 매니저/데이터베이스 미연결 시에도 fallback으로 안전 동작.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] private DialogueDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("DialogueManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>인스턴스 메서드. database가 없으면 fallback/key 반환 + 경고.</summary>
        public string GetText(string key, string fallback = null)
        {
            if (database == null)
            {
                Debug.LogWarning("DialogueManager: database is not assigned.");
                return fallback ?? key;
            }
            DialogueEntry entry = database.GetEntry(key);
            if (entry == null)
            {
                Debug.LogWarning($"DialogueManager: key '{key}' not found in database.");
                return fallback ?? key;
            }
            return entry.Text;
        }

        /// <summary>
        /// 정적 헬퍼. Instance가 null이어도 안전하게 fallback 반환.
        /// 가장 흔한 호출 패턴: <c>DialogueManager.Get("character.nabyeol.default", "안녕!")</c>
        /// </summary>
        public static string Get(string key, string fallback = null)
        {
            if (Instance == null)
            {
                return fallback ?? key;
            }
            return Instance.GetText(key, fallback);
        }

        public DialogueDatabase Database => database;
    }
}
