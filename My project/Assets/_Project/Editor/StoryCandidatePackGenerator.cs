using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;
using NabyeolDabyeolDreamPuzzle.Agents;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 기본 스토리 대사 후보팩(DefaultStoryCandidatePack.asset)을 자동 생성/업데이트한다.
    /// 캐릭터별/상황별 안전 문장 24개+를 등록하고, 씬에 StoryMakerAgent가 있으면 자동 attach.
    /// 같은 id의 항목은 in-place 업데이트, 신규 id는 append. 멱등.
    /// 메뉴: Tools/NabyeolDabyeol/Generate Story Candidate Pack
    /// </summary>
    public static class StoryCandidatePackGenerator
    {
        private const string Folder = "Assets/_Project/Data/StoryCandidates";
        private const string AssetPath = "Assets/_Project/Data/StoryCandidates/DefaultStoryCandidatePack.asset";

        private struct Row
        {
            public string id;
            public string speakerId;
            public StoryDialogueType type;
            public bool allTypes;
            public StoryCandidateTone tone;
            public string text;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Story Candidate Pack")]
        public static void GenerateAll()
        {
            EnsureFolder(Folder);

            StoryCandidatePack pack = AssetDatabase.LoadAssetAtPath<StoryCandidatePack>(AssetPath);
            bool isNew = pack == null;
            if (pack == null) pack = ScriptableObject.CreateInstance<StoryCandidatePack>();

            Row[] rows = BuildRows();
            ApplyRows(pack, rows);

            if (isNew)
            {
                AssetDatabase.CreateAsset(pack, AssetPath);
                Debug.Log("StoryCandidatePackGenerator: Created " + AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(pack);
                Debug.Log("StoryCandidatePackGenerator: Updated " + AssetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            StoryCandidatePack loaded = AssetDatabase.LoadAssetAtPath<StoryCandidatePack>(AssetPath);
            if (loaded != null)
            {
                bool ok = loaded.Validate();
                Debug.Log($"StoryCandidatePackGenerator: Validate={ok}, count={loaded.Count}.");
            }

            // 씬의 StoryMakerAgent 자동 attach
            StoryMakerAgent agent = Object.FindAnyObjectByType<StoryMakerAgent>();
            if (agent != null)
            {
                SerializedObject so = new SerializedObject(agent);
                SerializedProperty prop = so.FindProperty("candidatePack");
                if (prop != null)
                {
                    prop.objectReferenceValue = loaded;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(agent);
                    AssetDatabase.SaveAssets();
                    Debug.Log("StoryCandidatePackGenerator: Attached pack to scene StoryMakerAgent.");
                }
            }
            else
            {
                Debug.Log("StoryCandidatePackGenerator: No StoryMakerAgent in active scene. Drag the asset manually into CandidatePack slot.");
            }
        }

        private static void ApplyRows(StoryCandidatePack pack, Row[] rows)
        {
            SerializedObject so = new SerializedObject(pack);
            SerializedProperty list = so.FindProperty("candidates");

            // 기존 id → 인덱스 매핑
            System.Collections.Generic.Dictionary<string, int> existing =
                new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty el = list.GetArrayElementAtIndex(i);
                string id = el.FindPropertyRelative("id").stringValue;
                if (!string.IsNullOrWhiteSpace(id)) existing[id] = i;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                Row r = rows[i];
                int targetIdx;
                if (existing.TryGetValue(r.id, out targetIdx))
                {
                    // in-place 업데이트
                }
                else
                {
                    targetIdx = list.arraySize;
                    list.arraySize = targetIdx + 1;
                    existing[r.id] = targetIdx;
                }
                SerializedProperty el = list.GetArrayElementAtIndex(targetIdx);
                el.FindPropertyRelative("id").stringValue = r.id;
                el.FindPropertyRelative("targetSpeakerId").stringValue = r.speakerId ?? string.Empty;
                el.FindPropertyRelative("targetDialogueType").enumValueIndex = (int)r.type;
                el.FindPropertyRelative("applicableToAllTypes").boolValue = r.allTypes;
                el.FindPropertyRelative("tone").enumValueIndex = (int)r.tone;
                el.FindPropertyRelative("text").stringValue = r.text;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Row R(string id, string speakerId, StoryDialogueType type, bool allTypes, StoryCandidateTone tone, string text)
        {
            return new Row { id = id, speakerId = speakerId, type = type, allTypes = allTypes, tone = tone, text = text };
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Row[] BuildRows()
        {
            return new Row[]
            {
                // ── 나별 (StageStart 기본) ──
                R("nabyeol_start_brave_01",  "nabyeol",  StoryDialogueType.StageStart, false, StoryCandidateTone.Brave,       "좋아! 우리가 함께 해보자!"),
                R("nabyeol_start_brave_02",  "nabyeol",  StoryDialogueType.StageStart, false, StoryCandidateTone.Brave,       "반짝이는 길을 찾아보자!"),
                R("nabyeol_fail_cheer_01",   "nabyeol",  StoryDialogueType.StageFail,  false, StoryCandidateTone.Cheer,       "괜찮아, 다시 도전하면 돼!"),

                // ── 다별 (StageStart/Clear) ──
                R("dabyeol_start_calm_01",   "dabyeol",  StoryDialogueType.StageStart, false, StoryCandidateTone.Calm,        "차분히 보면 답이 보여."),
                R("dabyeol_start_calm_02",   "dabyeol",  StoryDialogueType.StageStart, false, StoryCandidateTone.Calm,        "먼저 목표를 확인해 보자."),
                R("dabyeol_clear_calm_01",   "dabyeol",  StoryDialogueType.StageClear, false, StoryCandidateTone.Calm,        "순서대로 살펴보면 쉬워."),

                // ── 카피몽 (StageFail/Start, 격려) ──
                R("capymong_start_enc_01",   "capymong", StoryDialogueType.StageStart, false, StoryCandidateTone.Encouraging, "천천히 해도 괜찮아."),
                R("capymong_fail_enc_01",    "capymong", StoryDialogueType.StageFail,  false, StoryCandidateTone.Encouraging, "쉬어 가도 다시 할 수 있어."),
                R("capymong_start_enc_02",   "capymong", StoryDialogueType.StageStart, false, StoryCandidateTone.Encouraging, "같이 보면 길이 보여."),

                // ── 포포링 (StageStart, 방울/힌트) ──
                R("poporing_start_bub_01",   "poporing", StoryDialogueType.StageStart, false, StoryCandidateTone.Bubble,      "톡톡! 방울이 길을 알려줘."),
                R("poporing_start_bub_02",   "poporing", StoryDialogueType.StageStart, false, StoryCandidateTone.Bubble,      "반짝 방울을 따라가 보자!"),
                R("poporing_clear_bub_01",   "poporing", StoryDialogueType.StageClear, false, StoryCandidateTone.Bubble,      "작은 힌트가 떠올랐어!"),

                // ── 모찌룬 (StageStart, 순서/숫자) ──
                R("mochirun_start_num_01",   "mochirun", StoryDialogueType.StageStart, false, StoryCandidateTone.Numeric,     "차례대로 정리해 보자."),
                R("mochirun_start_num_02",   "mochirun", StoryDialogueType.StageStart, false, StoryCandidateTone.Numeric,     "같은 모양을 나란히 찾아봐."),
                R("mochirun_start_num_03",   "mochirun", StoryDialogueType.StageStart, false, StoryCandidateTone.Numeric,     "숫자처럼 하나씩 보면 쉬워."),

                // ── 노노 (BossIntro/StageStart, 장난기) ──
                R("nono_boss_play_01",       "nono",     StoryDialogueType.BossIntro,  false, StoryCandidateTone.Playful,     "히히, 장난 퍼즐도 재밌지?"),
                R("nono_boss_play_02",       "nono",     StoryDialogueType.BossIntro,  false, StoryCandidateTone.Playful,     "맞춰봐! 풀면 길이 열릴 거야."),
                R("nono_fail_play_01",       "nono",     StoryDialogueType.StageFail,  false, StoryCandidateTone.Playful,     "이번엔 조금 헷갈렸지?"),

                // ── 보스/특수 공통 (BossIntro) ──
                R("common_boss_clim_01",     "",         StoryDialogueType.BossIntro,  false, StoryCandidateTone.BossClimax,  "작은 조각이 모이면 빛이 돌아와요."),
                R("common_boss_clim_02",     "",         StoryDialogueType.BossIntro,  false, StoryCandidateTone.BossClimax,  "천천히 맞추면 길이 다시 열려요."),
                R("common_boss_clim_03",     "",         StoryDialogueType.BossIntro,  false, StoryCandidateTone.BossClimax,  "마지막까지 차분히 해보자."),

                // ── 실패/재도전 공통 (StageFail) ──
                R("common_fail_enc_01",      "",         StoryDialogueType.StageFail,  false, StoryCandidateTone.Encouraging, "괜찮아. 다시 하면 더 잘할 수 있어."),
                R("common_fail_enc_02",      "",         StoryDialogueType.StageFail,  false, StoryCandidateTone.Encouraging, "이번엔 목표를 먼저 찾아보자."),
                R("common_fail_enc_03",      "",         StoryDialogueType.StageFail,  false, StoryCandidateTone.Encouraging, "천천히 보면 좋은 길이 보여.")
            };
        }
    }
}
