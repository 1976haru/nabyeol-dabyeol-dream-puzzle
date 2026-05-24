using System;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.ParentMode
{
    /// <summary>
    /// 부모 모드 상태 관리 + 보호자 확인 문제 생성/검증 + 자동 종료 타이머.
    /// 활성 상태는 PlayerPrefs에 저장하지 않음 (앱 재시작 시 비활성으로 시작).
    /// TODO: Add confirm popup before resetting progress (inside parent mode dangerous actions).
    /// TODO: Add audio cue for correct/incorrect parent check.
    /// </summary>
    public class ParentModeManager : MonoBehaviour
    {
        public static ParentModeManager Instance { get; private set; }

        [Header("Timeout")]
        // 부모 모드 자동 종료 시간(초). 0 이하이면 무제한.
        [SerializeField, Min(0f)] private float parentModeTimeoutSeconds = 300f;

        [Header("Debug")]
        // 개발 편의용 우회 옵션. 빌드 전 반드시 false로 되돌릴 것.
        [SerializeField] private bool bypassParentCheckForDebug = false;

        private bool isParentModeActive;
        private float remainingTimeoutSeconds;

        /// <summary>(주의) Parent mode active state should not persist across app restarts.</summary>
        public bool IsParentModeActive => isParentModeActive;
        public bool BypassParentCheckForDebug => bypassParentCheckForDebug;
        public float ParentModeTimeoutSeconds => parentModeTimeoutSeconds;
        public float RemainingTimeoutSeconds => remainingTimeoutSeconds;

        public event Action OnEnterParentMode;
        public event Action OnExitParentMode;

        public struct ParentCheckQuestion
        {
            public string questionText;
            public int answer;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("ParentModeManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            isParentModeActive = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!isParentModeActive) return;
            if (parentModeTimeoutSeconds <= 0f) return; // 0 = 무제한 (디자인 옵션)

            remainingTimeoutSeconds -= Time.unscaledDeltaTime;
            if (remainingTimeoutSeconds <= 0f)
            {
                Debug.Log("ParentModeManager: Parent mode auto-timeout reached.");
                ExitParentMode();
            }
        }

        /// <summary>
        /// 10~30 사이 첫 숫자 + 5~15 사이 둘째 숫자의 덧셈 또는 뺄셈 문제 생성.
        /// 어린이가 쉽게 풀지 못하지만 보호자라면 즉답 가능한 난이도.
        /// </summary>
        public ParentCheckQuestion GenerateParentCheckQuestion()
        {
            int a = UnityEngine.Random.Range(10, 31);
            int b = UnityEngine.Random.Range(5, 16);
            bool useAddition = UnityEngine.Random.value > 0.5f || b > a;
            if (useAddition)
            {
                return new ParentCheckQuestion
                {
                    questionText = $"{a} + {b} = ?",
                    answer = a + b
                };
            }
            return new ParentCheckQuestion
            {
                questionText = $"{a} - {b} = ?",
                answer = a - b
            };
        }

        public bool ValidateAnswer(int userAnswer, int correctAnswer)
        {
            return userAnswer == correctAnswer;
        }

        public void EnterParentMode()
        {
            if (isParentModeActive)
            {
                // 타이머만 갱신
                remainingTimeoutSeconds = parentModeTimeoutSeconds;
                Debug.Log("ParentModeManager: Already active. Timeout refreshed.");
                return;
            }
            isParentModeActive = true;
            remainingTimeoutSeconds = parentModeTimeoutSeconds;
            Debug.Log($"ParentModeManager: Parent mode entered. Timeout: {parentModeTimeoutSeconds}s.");
            OnEnterParentMode?.Invoke();
        }

        public void ExitParentMode()
        {
            if (!isParentModeActive) return;
            isParentModeActive = false;
            remainingTimeoutSeconds = 0f;
            Debug.Log("ParentModeManager: Parent mode exited.");
            OnExitParentMode?.Invoke();
        }

        /// <summary>잠금 메뉴 접근 가능 여부. bypass 옵션이 켜져 있어도 통과.</summary>
        public bool CanAccessParentOnlyMenu()
        {
            return isParentModeActive || bypassParentCheckForDebug;
        }
    }
}
