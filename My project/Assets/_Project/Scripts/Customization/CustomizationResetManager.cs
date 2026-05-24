using System;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.ParentMode;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.Customization
{
    /// <summary>
    /// 사용자 커스터마이징 데이터(별칭/대표 대사/스토리 override)를 기본값으로 복구.
    /// - 원본 ScriptableObject 자산은 절대 수정/삭제하지 않는다.
    /// - PlayerPrefs의 진행도(StageCleared_, HighestUnlockedStageId, AlbumPageUnlocked_, 카드/지역 복구 등)는 보호.
    /// - 부모 모드 가드: 일반 진입(`ResetCustomizationToDefaults`)은 부모 모드 필수.
    /// - ContextMenu 디버그 진입은 가드 우회 가능하지만 로그에 명확히 남긴다.
    /// 위험한 전체 진행도 초기화는 별도 함수 `ResetAllProgressDangerous`로 분리 (이번 작업 미구현, TODO).
    /// TODO: Add separate protected flow for full progress reset.
    /// TODO: Add undo window (소프트 삭제 후 N초 안에 되돌릴 수 있는 stash).
    /// </summary>
    public class CustomizationResetManager : MonoBehaviour
    {
        public static CustomizationResetManager Instance { get; private set; }

        /// <summary>참고용 — 본 매니저가 다루는 커스터마이징 PlayerPrefs prefix 목록.</summary>
        public static readonly string[] CustomizationKeyPrefixes =
        {
            "CharacterAlias_",
            "CharacterRepDialogue_",
            "StoryOverride_"
        };

        public event Action OnCustomizationReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CustomizationResetManager: Another instance already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 커스터마이징 데이터 일괄 복구. 부모 모드에서만 실행 가능.
        /// 진행도 PlayerPrefs(StageCleared_/HighestUnlockedStageId/AlbumPageUnlocked_/카드/지역)는 건드리지 않는다.
        /// </summary>
        public bool ResetCustomizationToDefaults()
        {
            if (!RequireParentMode("ResetCustomizationToDefaults")) return false;
            ExecuteReset(skipParentCheck: false);
            return true;
        }

        /// <summary>개발용. 부모 모드 우회. 빌드 전 사용 금지.</summary>
        [ContextMenu("Reset Customization To Defaults For Debug")]
        public void ResetCustomizationToDefaultsForDebug()
        {
            Debug.LogWarning("CustomizationResetManager: DEBUG reset invoked. Parent mode check bypassed. DO NOT ship debug calls.");
            ExecuteReset(skipParentCheck: true);
        }

        private void ExecuteReset(bool skipParentCheck)
        {
            Debug.Log($"CustomizationResetManager: Reset started (parentCheckSkipped={skipParentCheck}).");

            // 1) 캐릭터 별칭 초기화
            if (CharacterAliasManager.Instance != null)
            {
                CharacterAliasManager.Instance.ClearAllAliases();
                Debug.Log("CustomizationResetManager: Aliases cleared.");
            }
            else
            {
                Debug.LogWarning("CustomizationResetManager: CharacterAliasManager not found. Skipping alias reset.");
            }

            // 2) 대표 대사 선택 초기화
            if (CharacterRepresentativeDialogueManager.Instance != null)
            {
                CharacterRepresentativeDialogueManager.Instance.ClearAllRepresentativeDialogues();
                Debug.Log("CustomizationResetManager: Representative dialogue selections cleared.");
            }
            else
            {
                Debug.LogWarning("CustomizationResetManager: CharacterRepresentativeDialogueManager not found. Skipping representative dialogue reset.");
            }

            // 3) 스토리 대사 override 초기화 (proposed/approved/isApproved + KeyList)
            if (StoryDialogueOverrideManager.Instance != null)
            {
                StoryDialogueOverrideManager.Instance.ClearAllOverrides();
                Debug.Log("CustomizationResetManager: Story dialogue overrides cleared.");
            }
            else
            {
                Debug.LogWarning("CustomizationResetManager: StoryDialogueOverrideManager not found. Skipping story override reset.");
            }

            PlayerPrefs.Save();
            Debug.Log("CustomizationResetManager: Reset completed. Firing OnCustomizationReset.");
            OnCustomizationReset?.Invoke();
        }

        /// <summary>위험 — 진행도 + 커스터마이징 모두 초기화. 이번 작업에서는 미구현.</summary>
        public void ResetAllProgressDangerous()
        {
            Debug.LogWarning("CustomizationResetManager: ResetAllProgressDangerous is not implemented yet. TODO: Add separate protected flow for full progress reset.");
            // 진행도(StageCleared_, HighestUnlockedStageId, AlbumPageUnlocked_)는 본 함수가 다룰 영역.
            // 별도 작업에서 부모 모드 + 2단계 확인 + 위험 안내 후 구현 예정.
        }

        private bool RequireParentMode(string op)
        {
            if (ParentModeManager.Instance == null)
            {
                Debug.LogWarning($"CustomizationResetManager: {op} blocked. ParentModeManager not found.");
                return false;
            }
            if (!ParentModeManager.Instance.CanAccessParentOnlyMenu())
            {
                Debug.LogWarning($"CustomizationResetManager: {op} blocked. Parent mode is not active.");
                return false;
            }
            return true;
        }
    }
}
