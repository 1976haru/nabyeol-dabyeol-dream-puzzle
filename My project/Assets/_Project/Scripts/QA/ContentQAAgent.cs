using System.Collections.Generic;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Album;
using NabyeolDabyeolDreamPuzzle.Cards;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Dialogue;
using NabyeolDabyeolDreamPuzzle.Learning;
using NabyeolDabyeolDreamPuzzle.Skill;
using NabyeolDabyeolDreamPuzzle.Stage;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.QA
{
    /// <summary>
    /// 콘텐츠 QA 검사 v1. 런타임 호환(UnityEditor 의존 없음).
    /// Database 인스턴스를 외부에서 주입받아 검사. EditorWindow는 AssetDatabase로 자동 탐색해 본 에이전트에 주입한다.
    /// 자동 수정은 하지 않고 보고만 한다.
    /// TODO: Export QA report to JSON.
    /// TODO: Export QA report to CSV.
    /// TODO: Add safe text filter scan (SafetyFilterAgent 연동).
    /// TODO: Add localization key validation.
    /// TODO: Add automated CI validation.
    /// </summary>
    public class ContentQAAgent
    {
        private const int DialogueWarnLength = 80;
        private const int StoryDialogueWarnLength = 80;
        private const int CardShortTextWarnLength = 60;

        public DialogueDatabase dialogueDatabase;
        public CharacterPackDatabase characterPackDatabase;
        public KnowledgeCardDatabase knowledgeCardDatabase;
        public StagePackDatabase stagePackDatabase;
        public StoryPackDatabase storyPackDatabase;
        public AlbumDatabase albumDatabase;
        public LearningPackDatabase learningPackDatabase;

        /// <summary>등록된 모든 Database를 순회하며 검사 결과를 반환.</summary>
        public ContentQAReport RunFullCheck()
        {
            ContentQAReport report = new ContentQAReport();
            try { CheckDialogueDatabase(report); }   catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "DialogueDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckCharacterPackDatabase(report); } catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "CharacterPackDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckKnowledgeCardDatabase(report); } catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "KnowledgeCardDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckStagePackDatabase(report); }  catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "StagePackDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckStoryPackDatabase(report); }  catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "StoryPackDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckAlbumDatabase(report); }      catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "AlbumDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            try { CheckLearningPackDatabase(report); } catch (System.Exception e) { Log(report, ContentQASeverity.Error, "Internal", "LearningPackDatabase check threw " + e.GetType().Name + ": " + e.Message); }
            return report;
        }

        // ───────── DialogueDatabase ─────────

        private void CheckDialogueDatabase(ContentQAReport report)
        {
            const string CAT = "DialogueDatabase";
            if (dialogueDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "DialogueDatabase asset not assigned. Skipping detailed check.");
                return;
            }
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < dialogueDatabase.Entries.Count; i++)
            {
                DialogueEntry e = dialogueDatabase.Entries[i];
                if (e == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"entries[{i}]", "Null entry.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(e.Key))
                {
                    Log(report, ContentQASeverity.Error, CAT, $"entries[{i}]", "Empty key.");
                }
                else if (!seen.Add(e.Key))
                {
                    Log(report, ContentQASeverity.Error, CAT, e.Key, "Duplicate key.");
                }
                if (string.IsNullOrWhiteSpace(e.Text))
                {
                    Log(report, ContentQASeverity.Error, CAT, e.Key, "Empty text.");
                }
                else if (e.Text.Length > DialogueWarnLength)
                {
                    Log(report, ContentQASeverity.Warning, CAT, e.Key, $"Text too long ({e.Text.Length} > {DialogueWarnLength}).");
                }
            }
        }

        // ───────── CharacterPackDatabase ─────────

        private void CheckCharacterPackDatabase(ContentQAReport report)
        {
            const string CAT = "CharacterPack";
            if (characterPackDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "CharacterPackDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<string> seenIds = new HashSet<string>();
            for (int i = 0; i < characterPackDatabase.Characters.Count; i++)
            {
                CharacterPackData c = characterPackDatabase.Characters[i];
                if (c == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"characters[{i}]", "Null character entry.");
                    continue;
                }
                string id = c.CharacterId;
                string nameTag = !string.IsNullOrWhiteSpace(id) ? id : c.name;

                if (string.IsNullOrWhiteSpace(id))
                    Log(report, ContentQASeverity.Error, CAT, c.name, "Empty characterId.");
                else if (!seenIds.Add(id))
                    Log(report, ContentQASeverity.Error, CAT, id, "Duplicate characterId.");
                if (string.IsNullOrWhiteSpace(c.CharacterName))
                    Log(report, ContentQASeverity.Error, CAT, nameTag, "Empty characterName.");

                // dialogue key 참조 검증 (None skill 캐릭터는 skill key 누락 허용)
                CheckDialogueKeyExists(report, CAT, nameTag, "defaultDialogueKey", c.DefaultDialogueKey, required: true);
                CheckDialogueKeyExists(report, CAT, nameTag, "representativeDialogueKey", c.RepresentativeDialogueKey, required: true);

                bool skillRequired = c.SkillType != SkillType.None;
                CheckDialogueKeyExists(report, CAT, nameTag, "skillTitleKey", c.SkillTitleKey, required: skillRequired);
                CheckDialogueKeyExists(report, CAT, nameTag, "skillDescriptionKey", c.SkillDescriptionKey, required: skillRequired);
                CheckDialogueKeyExists(report, CAT, nameTag, "skillSuccessDialogueKey", c.SkillSuccessDialogueKey, required: skillRequired);
                CheckDialogueKeyExists(report, CAT, nameTag, "skillFailDialogueKey", c.SkillFailDialogueKey, required: skillRequired);

                // 대표 대사 템플릿 dialogueKey 참조
                if (c.RepresentativeDialogueTemplates != null)
                {
                    for (int t = 0; t < c.RepresentativeDialogueTemplates.Count; t++)
                    {
                        var tpl = c.RepresentativeDialogueTemplates[t];
                        if (tpl == null) continue;
                        CheckDialogueKeyExists(report, CAT, nameTag,
                            $"representativeDialogueTemplates[{tpl.TemplateId}].dialogueKey",
                            tpl.DialogueKey, required: true);
                    }
                }
            }
        }

        private void CheckDialogueKeyExists(ContentQAReport report, string category, string asset, string fieldName, string key, bool required)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                if (required)
                    Log(report, ContentQASeverity.Error, category, asset, $"{fieldName} is empty.");
                return;
            }
            if (dialogueDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, category, asset, $"{fieldName}='{key}' cannot be verified — DialogueDatabase missing.");
                return;
            }
            if (!dialogueDatabase.HasKey(key))
            {
                Log(report, ContentQASeverity.Error, category, asset, $"{fieldName}='{key}' not found in DialogueDatabase.");
            }
        }

        // ───────── KnowledgeCardDatabase ─────────

        private void CheckKnowledgeCardDatabase(ContentQAReport report)
        {
            const string CAT = "KnowledgeCard";
            if (knowledgeCardDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "KnowledgeCardDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < knowledgeCardDatabase.Cards.Count; i++)
            {
                KnowledgeCardData c = knowledgeCardDatabase.Cards[i];
                if (c == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"cards[{i}]", "Null card entry.");
                    continue;
                }
                string id = c.CardId;
                string tag = !string.IsNullOrWhiteSpace(id) ? id : c.name;

                if (string.IsNullOrWhiteSpace(id))
                    Log(report, ContentQASeverity.Error, CAT, c.name, "Empty cardId.");
                else if (!seen.Add(id))
                    Log(report, ContentQASeverity.Error, CAT, id, "Duplicate cardId.");

                if (string.IsNullOrWhiteSpace(c.CardName))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Empty cardName.");
                if (string.IsNullOrWhiteSpace(c.ShortText))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Empty shortText.");
                else if (c.ShortText.Length > CardShortTextWarnLength)
                    Log(report, ContentQASeverity.Warning, CAT, tag, $"shortText too long ({c.ShortText.Length} > {CardShortTextWarnLength}).");

                if (c.LinkedStageId <= 0)
                    Log(report, ContentQASeverity.Warning, CAT, tag, $"linkedStageId invalid ({c.LinkedStageId}).");
                // Sprite null은 무시 (아트 미준비)
            }
        }

        // ───────── StagePackDatabase ─────────

        private void CheckStagePackDatabase(ContentQAReport report)
        {
            const string CAT = "StagePack";
            if (stagePackDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "StagePackDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<string> seenPackIds = new HashSet<string>();
            HashSet<int> seenStageIds = new HashSet<int>();
            for (int i = 0; i < stagePackDatabase.StagePacks.Count; i++)
            {
                StagePackData pack = stagePackDatabase.StagePacks[i];
                if (pack == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"stagePacks[{i}]", "Null pack entry.");
                    continue;
                }
                string packId = pack.PackId;
                string tag = !string.IsNullOrWhiteSpace(packId) ? packId : pack.name;

                if (string.IsNullOrWhiteSpace(packId))
                    Log(report, ContentQASeverity.Error, CAT, pack.name, "Empty packId.");
                else if (!seenPackIds.Add(packId))
                    Log(report, ContentQASeverity.Error, CAT, packId, "Duplicate packId.");

                if (string.IsNullOrWhiteSpace(pack.PackName))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Empty packName.");

                if (pack.Stages == null || pack.Stages.Count == 0)
                {
                    Log(report, ContentQASeverity.Warning, CAT, tag, "Pack has no stages.");
                    continue;
                }
                for (int s = 0; s < pack.Stages.Count; s++)
                {
                    StageData st = pack.Stages[s];
                    if (st == null)
                    {
                        Log(report, ContentQASeverity.Error, CAT, $"{tag}.stages[{s}]", "Null stage entry.");
                        continue;
                    }
                    string stTag = $"{tag}.stage[{st.StageId}]";
                    if (st.StageId <= 0)
                        Log(report, ContentQASeverity.Error, CAT, stTag, "Invalid stageId.");
                    else if (!seenStageIds.Add(st.StageId))
                        Log(report, ContentQASeverity.Error, CAT, stTag, "Duplicate stageId across packs.");

                    if (st.MoveLimit <= 0)
                        Log(report, ContentQASeverity.Error, CAT, stTag, $"Invalid moveLimit ({st.MoveLimit}).");

                    // rewardCardId 검사 (있으면 KnowledgeCardDatabase에 존재해야 함)
                    if (string.IsNullOrWhiteSpace(st.RewardCardId))
                    {
                        Log(report, ContentQASeverity.Warning, CAT, stTag, "rewardCardId is empty.");
                    }
                    else if (knowledgeCardDatabase != null && knowledgeCardDatabase.FindByCardId(st.RewardCardId) == null)
                    {
                        Log(report, ContentQASeverity.Error, CAT, stTag, $"rewardCardId='{st.RewardCardId}' not found in KnowledgeCardDatabase.");
                    }
                }
            }
        }

        // ───────── StoryPackDatabase ─────────

        private void CheckStoryPackDatabase(ContentQAReport report)
        {
            const string CAT = "StoryPack";
            if (storyPackDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "StoryPackDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<int> seenStoryIds = new HashSet<int>();
            HashSet<int> seenLinkedStageIds = new HashSet<int>();
            for (int i = 0; i < storyPackDatabase.Packs.Count; i++)
            {
                StoryPackData pack = storyPackDatabase.Packs[i];
                if (pack == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"packs[{i}]", "Null pack entry.");
                    continue;
                }
                string packTag = pack.name;
                if (pack.StoryNodes == null) continue;
                for (int n = 0; n < pack.StoryNodes.Count; n++)
                {
                    StoryNode node = pack.StoryNodes[n];
                    if (node == null)
                    {
                        Log(report, ContentQASeverity.Error, CAT, $"{packTag}.nodes[{n}]", "Null story node.");
                        continue;
                    }
                    string nodeTag = $"{packTag}.{node.name}(storyId={node.StoryId})";

                    if (node.StoryId <= 0)
                        Log(report, ContentQASeverity.Error, CAT, nodeTag, "Invalid storyId.");
                    else if (!seenStoryIds.Add(node.StoryId))
                        Log(report, ContentQASeverity.Error, CAT, nodeTag, "Duplicate storyId.");

                    if (node.LinkedStageId <= 0)
                        Log(report, ContentQASeverity.Error, CAT, nodeTag, "Invalid linkedStageId.");
                    else if (!seenLinkedStageIds.Add(node.LinkedStageId))
                        Log(report, ContentQASeverity.Error, CAT, nodeTag, "Duplicate linkedStageId.");

                    CheckStoryDialogueList(report, CAT, nodeTag + ".start", node.StartDialogues);
                    CheckStoryDialogueList(report, CAT, nodeTag + ".clear", node.ClearDialogues);
                    CheckStoryDialogueList(report, CAT, nodeTag + ".fail",  node.FailDialogues);
                    CheckStoryDialogueList(report, CAT, nodeTag + ".boss",  node.BossDialogues);
                }
            }
        }

        private void CheckStoryDialogueList(ContentQAReport report, string category, string nodeTag, IReadOnlyList<StoryDialogueLine> lines)
        {
            if (lines == null) return;
            for (int i = 0; i < lines.Count; i++)
            {
                StoryDialogueLine line = lines[i];
                string tag = $"{nodeTag}[{i}]";
                if (line == null)
                {
                    Log(report, ContentQASeverity.Error, category, tag, "Null line.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line.Dialogue))
                    Log(report, ContentQASeverity.Error, category, tag, "Empty dialogue.");
                else if (line.Dialogue.Length > StoryDialogueWarnLength)
                    Log(report, ContentQASeverity.Warning, category, tag, $"Dialogue too long ({line.Dialogue.Length} > {StoryDialogueWarnLength}).");
                if (string.IsNullOrWhiteSpace(line.SpeakerName))
                    Log(report, ContentQASeverity.Warning, category, tag, "Empty speakerName.");
                if (string.IsNullOrWhiteSpace(line.SpeakerId))
                    Log(report, ContentQASeverity.Warning, category, tag, "Empty speakerId.");
                else if (characterPackDatabase != null && characterPackDatabase.FindById(line.SpeakerId) == null)
                    Log(report, ContentQASeverity.Warning, category, tag, $"speakerId '{line.SpeakerId}' not registered in CharacterPackDatabase.");
            }
        }

        // ───────── AlbumDatabase ─────────

        private void CheckAlbumDatabase(ContentQAReport report)
        {
            const string CAT = "Album";
            if (albumDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "AlbumDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<int> seenPageIds = new HashSet<int>();
            for (int i = 0; i < albumDatabase.Pages.Count; i++)
            {
                AlbumPageData p = albumDatabase.Pages[i];
                if (p == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"pages[{i}]", "Null page entry.");
                    continue;
                }
                string tag = $"page[{p.PageId}]";
                if (p.PageId <= 0)
                    Log(report, ContentQASeverity.Error, CAT, p.name, "Invalid pageId.");
                else if (!seenPageIds.Add(p.PageId))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Duplicate pageId.");

                if (p.LinkedStageId <= 0)
                    Log(report, ContentQASeverity.Warning, CAT, tag, "Invalid linkedStageId.");
                if (string.IsNullOrWhiteSpace(p.LinkedCardId))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Empty linkedCardId.");
                else if (knowledgeCardDatabase != null && knowledgeCardDatabase.FindByCardId(p.LinkedCardId) == null)
                    Log(report, ContentQASeverity.Error, CAT, tag, $"linkedCardId='{p.LinkedCardId}' not found in KnowledgeCardDatabase.");
                if (string.IsNullOrWhiteSpace(p.PageTitle))
                    Log(report, ContentQASeverity.Warning, CAT, tag, "Empty pageTitle.");
            }
        }

        // ───────── LearningPackDatabase ─────────

        private void CheckLearningPackDatabase(ContentQAReport report)
        {
            const string CAT = "LearningPack";
            if (learningPackDatabase == null)
            {
                Log(report, ContentQASeverity.Warning, CAT, "(missing)", "LearningPackDatabase asset not assigned. Skipping check.");
                return;
            }
            HashSet<string> seenPackIds = new HashSet<string>();
            for (int i = 0; i < learningPackDatabase.LearningPacks.Count; i++)
            {
                LearningPackData pack = learningPackDatabase.LearningPacks[i];
                if (pack == null)
                {
                    Log(report, ContentQASeverity.Error, CAT, $"learningPacks[{i}]", "Null pack.");
                    continue;
                }
                string id = pack.PackId;
                string tag = !string.IsNullOrWhiteSpace(id) ? id : pack.name;
                if (string.IsNullOrWhiteSpace(id))
                    Log(report, ContentQASeverity.Error, CAT, pack.name, "Empty packId.");
                else if (!seenPackIds.Add(id))
                    Log(report, ContentQASeverity.Error, CAT, id, "Duplicate packId.");
                if (string.IsNullOrWhiteSpace(pack.PackName))
                    Log(report, ContentQASeverity.Error, CAT, tag, "Empty packName.");

                if (pack.LearningGoals != null)
                {
                    HashSet<string> seenGoalIds = new HashSet<string>();
                    for (int g = 0; g < pack.LearningGoals.Count; g++)
                    {
                        LearningGoalData goal = pack.LearningGoals[g];
                        if (goal == null)
                        {
                            Log(report, ContentQASeverity.Error, CAT, $"{tag}.goals[{g}]", "Null goal.");
                            continue;
                        }
                        string goalTag = $"{tag}.{goal.GoalId}";
                        if (string.IsNullOrWhiteSpace(goal.GoalId))
                            Log(report, ContentQASeverity.Error, CAT, $"{tag}.goals[{g}]", "Empty goalId.");
                        else if (!seenGoalIds.Add(goal.GoalId))
                            Log(report, ContentQASeverity.Error, CAT, goalTag, "Duplicate goalId in pack.");
                        if (string.IsNullOrWhiteSpace(goal.LinkedCardId))
                            Log(report, ContentQASeverity.Error, CAT, goalTag, "Empty linkedCardId.");
                        else if (knowledgeCardDatabase != null && knowledgeCardDatabase.FindByCardId(goal.LinkedCardId) == null)
                            Log(report, ContentQASeverity.Error, CAT, goalTag, $"linkedCardId='{goal.LinkedCardId}' not found in KnowledgeCardDatabase.");
                    }
                }

                // 등록된 카드 목록이 KnowledgeCardDatabase에 모두 있는지
                if (pack.Cards != null && knowledgeCardDatabase != null)
                {
                    for (int c = 0; c < pack.Cards.Count; c++)
                    {
                        KnowledgeCardData card = pack.Cards[c];
                        if (card == null)
                        {
                            Log(report, ContentQASeverity.Warning, CAT, $"{tag}.cards[{c}]", "Null card entry.");
                            continue;
                        }
                        if (knowledgeCardDatabase.FindByCardId(card.CardId) == null)
                            Log(report, ContentQASeverity.Warning, CAT, $"{tag}.cards[{c}]", $"Card '{card.CardId}' not registered in KnowledgeCardDatabase.");
                    }
                }
            }
        }

        // ───────── 로그 헬퍼 ─────────

        private static void Log(ContentQAReport report, ContentQASeverity sev, string category, string asset, string msg)
        {
            report.Add(sev, category, asset, msg);
        }
    }
}
