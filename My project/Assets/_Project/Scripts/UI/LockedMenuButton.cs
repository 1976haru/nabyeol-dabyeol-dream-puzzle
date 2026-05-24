using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NabyeolDabyeolDreamPuzzle.ParentMode;

namespace NabyeolDabyeolDreamPuzzle.UI
{
    /// <summary>
    /// 부모 모드가 활성 상태일 때만 실제 기능을 실행하는 잠금 버튼 래퍼.
    /// 잠금 상태에서 클릭하면 ParentModeUI.OpenParentCheck를 호출해 보호자 확인을 띄운다.
    /// 부모 모드 활성 또는 bypass debug 옵션 시 onUnlockedClick UnityEvent를 호출한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LockedMenuButton : MonoBehaviour
    {
        [Header("Unlock Action")]
        [SerializeField] private UnityEvent onUnlockedClick;

        [Header("Parent Mode UI")]
        [SerializeField] private ParentModeUI parentModeUI;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            if (ParentModeManager.Instance == null)
            {
                Debug.LogWarning("LockedMenuButton: ParentModeManager.Instance not found. Click ignored.");
                return;
            }

            if (ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.Log("LockedMenuButton: Unlocked click invoked.");
                onUnlockedClick?.Invoke();
            }
            else
            {
                Debug.Log("LockedMenuButton: Locked. Opening parent check.");
                if (parentModeUI != null)
                {
                    parentModeUI.OpenParentCheck();
                }
                else
                {
                    Debug.LogWarning("LockedMenuButton: ParentModeUI is not assigned. Cannot show parent check.");
                }
            }
        }

        /// <summary>외부에서 ParentModeUI 참조를 코드로 설정할 수 있도록 노출.</summary>
        public void SetParentModeUI(ParentModeUI ui)
        {
            parentModeUI = ui;
        }
    }
}
