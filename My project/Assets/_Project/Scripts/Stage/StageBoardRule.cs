using System;
using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Puzzle;

namespace NabyeolDabyeolDreamPuzzle.Stage
{
    /// <summary>
    /// Stage Pack 단위로 공유하는 공통 보드 규칙.
    /// 개별 StageData의 값이 비어 있을 때 fallback으로 사용되는 기본값을 보관한다.
    /// </summary>
    [Serializable]
    public class StageBoardRule
    {
        [SerializeField, Min(3)] private int defaultBoardWidth = 8;
        [SerializeField, Min(3)] private int defaultBoardHeight = 8;
        [SerializeField] private List<BlockType> defaultAvailableBlockTypes = new List<BlockType>
        {
            BlockType.DreamBubble,
            BlockType.MoonRiceCake,
            BlockType.InkStar,
            BlockType.WaveCloud,
            BlockType.HeartLight
        };
        [SerializeField, Min(1)] private int defaultMoveLimit = 20;
        [SerializeField] private bool allowCascade = true;
        [SerializeField] private bool allowSkills = true;

        public int DefaultBoardWidth => defaultBoardWidth;
        public int DefaultBoardHeight => defaultBoardHeight;
        public IReadOnlyList<BlockType> DefaultAvailableBlockTypes => defaultAvailableBlockTypes;
        public int DefaultMoveLimit => defaultMoveLimit;
        public bool AllowCascade => allowCascade;
        public bool AllowSkills => allowSkills;

        public bool IsValid()
        {
            if (defaultBoardWidth < 3 || defaultBoardHeight < 3) return false;
            if (defaultMoveLimit <= 0) return false;
            if (defaultAvailableBlockTypes == null || defaultAvailableBlockTypes.Count == 0) return false;
            return true;
        }
    }
}
