using System;
using System.Collections.Generic;
using UnityEngine;

namespace NabyeolDabyeolDreamPuzzle.Customization
{
    /// <summary>
    /// 한 캐릭터의 별칭(별명) export 단위.
    /// </summary>
    [Serializable]
    public class CharacterAliasExportData
    {
        public string characterId;
        public string aliasName;
    }

    /// <summary>
    /// 한 캐릭터의 대표 대사 선택 export 단위. (dialogueKey만 저장하고 본문은 DialogueDatabase에서 조회)
    /// </summary>
    [Serializable]
    public class RepresentativeDialogueExportData
    {
        public string characterId;
        public string dialogueKey;
    }

    /// <summary>
    /// 한 스토리 대사 줄의 override export 단위.
    /// dialogueType은 StoryDialogueType enum 이름 문자열 (JsonUtility 제약 회피).
    /// </summary>
    [Serializable]
    public class StoryDialogueOverrideExportData
    {
        public int stageId;
        public string dialogueType; // "StageStart"/"StageClear"/"StageFail"/"BossIntro"
        public int lineIndex;
        public string proposedText;
        public string approvedText;
        public bool isApproved;
    }

    /// <summary>
    /// 가족 기기 간 이동을 위한 커스터마이징 팩 직렬화 루트.
    /// JsonUtility로 직렬화 가능한 형태(리스트 기반, Dictionary 미사용).
    /// formatVersion으로 v2.0 이후 migration 가능성을 열어 둔다.
    /// TODO: Add .mallangpack extension support in v2.0.
    /// TODO: Add progress backup (album/region/card) as opt-in fields in future version.
    /// </summary>
    [Serializable]
    public class PackExportData
    {
        public string formatVersion = PackExportImportManager.CurrentFormatVersion;
        public string appVersion = "1.0-dev";
        public string exportedAt = string.Empty;
        public List<CharacterAliasExportData> characterAliases = new List<CharacterAliasExportData>();
        public List<RepresentativeDialogueExportData> representativeDialogues = new List<RepresentativeDialogueExportData>();
        public List<StoryDialogueOverrideExportData> storyOverrides = new List<StoryDialogueOverrideExportData>();
    }
}
