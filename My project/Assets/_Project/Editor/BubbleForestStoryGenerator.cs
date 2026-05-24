using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 방울숲 1~15 스테이지에 매칭되는 StoryNode 자동 생성/업데이트.
    /// Stage 1은 #60/#61에서 생성된 Story_Stage_001.asset이 있으면 in-place 업데이트해
    /// linkedStageId=1 중복을 회피한다. 없으면 BubbleForest/ 폴더에 새로 생성한다.
    /// Stage 2~15는 항상 BubbleForest/ 폴더에 생성/업데이트한다.
    /// TODO: Add StoryPopup playback for stage start.
    /// TODO: Show clearDialogues before ClearPopup or inside ClearPopup.
    /// TODO: Show failDialogues before FailPopup or inside FailPopup.
    /// TODO: Add speaker portrait auto-link by speakerId.
    /// TODO: Add first-time-only story playback using PlayerPrefs.
    /// </summary>
    public static class BubbleForestStoryGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Story/BubbleForest";
        private const string LegacyStage1Path = "Assets/_Project/Data/Story/Story_Stage_001.asset";
        private const string FileNameFormat = "Story_BubbleForest_Stage_{0:D3}";

        private struct DialogueRow
        {
            public string speakerId;
            public string speakerName;
            public string dialogue;
        }

        private struct StoryRow
        {
            public int storyId;
            public int linkedStageId;
            public string storyTitle;
            public string learningTip;
            public DialogueRow[] startDialogues;
            public DialogueRow[] clearDialogues;
            public DialogueRow[] failDialogues;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate BubbleForest Stories")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            StoryRow[] rows = BuildRows();
            int created = 0, updated = 0;

            foreach (StoryRow row in rows)
            {
                string assetPath = ResolveAssetPath(row);
                StoryNode existing = AssetDatabase.LoadAssetAtPath<StoryNode>(assetPath);
                bool isNew = existing == null;

                StoryNode asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryNode>();
                ApplyRow(asset, row);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"BubbleForestStoryGenerator: Generation done. Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            foreach (StoryRow row in rows)
            {
                string assetPath = ResolveAssetPath(row);
                StoryNode sn = AssetDatabase.LoadAssetAtPath<StoryNode>(assetPath);
                if (sn != null && sn.IsValid())
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"BubbleForestStoryGenerator: Stage {row.linkedStageId} failed validation at {assetPath}.");
                }
            }
            Debug.Log($"BubbleForestStoryGenerator: Validation done. {validCount}/{rows.Length} stories passed IsValid().");
        }

        private static string ResolveAssetPath(StoryRow row)
        {
            // Stage 1: 기존 프롤로그 자산이 있으면 그 경로를 재사용 (linkedStageId=1 중복 회피).
            if (row.linkedStageId == 1)
            {
                StoryNode legacy = AssetDatabase.LoadAssetAtPath<StoryNode>(LegacyStage1Path);
                if (legacy != null)
                {
                    return LegacyStage1Path;
                }
            }
            return $"{OutputFolder}/{string.Format(FileNameFormat, row.linkedStageId)}.asset";
        }

        private static void ApplyRow(StoryNode asset, StoryRow row)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("storyId").intValue = row.storyId;
            so.FindProperty("storyTitle").stringValue = row.storyTitle;
            so.FindProperty("learningTip").stringValue = row.learningTip;
            so.FindProperty("linkedStageId").intValue = row.linkedStageId;

            SetDialogueList(so, "startDialogues", row.startDialogues);
            SetDialogueList(so, "clearDialogues", row.clearDialogues);
            SetDialogueList(so, "failDialogues", row.failDialogues);
            so.FindProperty("bossDialogues").arraySize = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = string.Format(FileNameFormat, row.linkedStageId);
        }

        private static void SetDialogueList(SerializedObject so, string fieldName, DialogueRow[] lines)
        {
            SerializedProperty list = so.FindProperty(fieldName);
            list.arraySize = lines == null ? 0 : lines.Length;
            if (lines == null) return;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("speakerId").stringValue = lines[i].speakerId;
                element.FindPropertyRelative("speakerName").stringValue = lines[i].speakerName;
                element.FindPropertyRelative("dialogue").stringValue = lines[i].dialogue;
                // portrait는 비워둔다. TODO: Link speakerId to CharacterData portrait.
            }
        }

        private static DialogueRow D(string id, string name, string dialogue)
        {
            return new DialogueRow { speakerId = id, speakerName = name, dialogue = dialogue };
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

        private static StoryRow[] BuildRows()
        {
            return new StoryRow[]
            {
                new StoryRow
                {
                    storyId = 101, linkedStageId = 1,
                    storyTitle = "토끼의 첫 점프 이야기",
                    learningTip = "같은 블록 3개를 맞추면 길이 열려요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "토끼처럼 가볍게 시작해 보자!"),
                        D("dabyeol", "다별", "같은 블록 3개를 먼저 찾아봐."),
                        D("capymong", "카피몽", "천천히 해도 괜찮아.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "좋아! 첫 점프 성공이야!") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 다시 폴짝 뛰어보자.") }
                },
                new StoryRow
                {
                    storyId = 102, linkedStageId = 2,
                    storyTitle = "다람쥐 도토리길 이야기",
                    learningTip = "목표 블록을 먼저 찾으면 쉬워요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "이번 길은 도토리처럼 노란 블록이 중요해."),
                        D("nabyeol", "나별", "반짝이는 노란 블록을 모아보자!"),
                        D("capymong", "카피몽", "하나씩 모으면 금방이야.")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "도토리길을 잘 지나왔어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "다음엔 노란 블록부터 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 103, linkedStageId = 3,
                    storyTitle = "고슴도치 반짝길 이야기",
                    learningTip = "연쇄가 생기면 점수가 더 올라요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "고슴도치 길이 반짝반짝해!"),
                        D("dabyeol", "다별", "블록이 이어지면 점수가 더 커져."),
                        D("poporing", "포포링", "톡톡! 반짝 길을 찾아볼래?")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "반짝길을 멋지게 통과했어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "이번엔 이어지는 블록을 살펴보자.") }
                },
                new StoryRow
                {
                    storyId = 104, linkedStageId = 4,
                    storyTitle = "여우의 살금걸음 이야기",
                    learningTip = "목표 색 블록을 차분히 모아보세요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "여우처럼 조심조심 움직여 보자."),
                        D("nabyeol", "나별", "초록 블록을 살금살금 모으면 돼!"),
                        D("capymong", "카피몽", "급하지 않아. 천천히 보자.")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "신중하게 잘 해결했어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 천천히 다시 보면 돼.") }
                },
                new StoryRow
                {
                    storyId = 105, linkedStageId = 5,
                    storyTitle = "사슴의 숲길 이야기",
                    learningTip = "큰 점수는 여러 번 맞추면 만들 수 있어요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "사슴이 지나간 숲길이야!"),
                        D("dabyeol", "다별", "이번엔 점수를 차근차근 모아야 해."),
                        D("capymong", "카피몽", "숲길은 천천히 걸어도 좋아.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "숲길이 환하게 열렸어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "점수 목표를 다시 확인해 보자.") }
                },
                new StoryRow
                {
                    storyId = 106, linkedStageId = 6,
                    storyTitle = "너구리 장난숲 이야기",
                    learningTip = "목표 블록은 연쇄로도 모을 수 있어요.",
                    startDialogues = new[] {
                        D("poporing", "포포링", "장난꾸러기 너구리가 블록을 숨겼어!"),
                        D("dabyeol", "다별", "파란 블록을 먼저 찾아보자."),
                        D("nabyeol", "나별", "좋아, 장난숲을 통과하자!")
                    },
                    clearDialogues = new[] { D("poporing", "포포링", "방울처럼 톡! 잘 찾았어.") },
                    failDialogues  = new[] { D("nabyeol", "나별", "다시 찾으면 분명 보일 거야!") }
                },
                new StoryRow
                {
                    storyId = 107, linkedStageId = 7,
                    storyTitle = "부엉이 밤눈 이야기",
                    learningTip = "어두운 길에서도 같은 블록은 보여요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "부엉이는 밤에도 길을 잘 찾아."),
                        D("nabyeol", "나별", "우리도 반짝 블록을 찾아보자!"),
                        D("capymong", "카피몽", "눈을 천천히 움직여 봐.")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "어두운 길도 잘 지나왔어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 다시 천천히 보면 돼.") }
                },
                new StoryRow
                {
                    storyId = 108, linkedStageId = 8,
                    storyTitle = "개구리 연못점프 이야기",
                    learningTip = "목표 블록을 모으면 길이 가까워져요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "개구리처럼 폴짝 뛰어보자!"),
                        D("dabyeol", "다별", "이번엔 초록 블록을 모아야 해."),
                        D("poporing", "포포링", "방울 연못이 톡톡 울려!")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "연못을 멋지게 건넜어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "초록 블록을 더 먼저 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 109, linkedStageId = 9,
                    storyTitle = "오리의 물결길 이야기",
                    learningTip = "부드럽게 이어 맞추면 점수가 올라요.",
                    startDialogues = new[] {
                        D("capymong", "카피몽", "물결처럼 천천히 흘러가 보자."),
                        D("nabyeol", "나별", "오리가 지나간 길을 따라가자!"),
                        D("dabyeol", "다별", "점수를 차근차근 모으면 돼.")
                    },
                    clearDialogues = new[] { D("capymong", "카피몽", "물결길을 편하게 지나왔어.") },
                    failDialogues  = new[] { D("nabyeol", "나별", "다시 하면 더 멀리 갈 수 있어!") }
                },
                new StoryRow
                {
                    storyId = 110, linkedStageId = 10,
                    storyTitle = "고양이 낮잠숲 이야기",
                    learningTip = "필요한 색을 먼저 찾으면 수집이 쉬워요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "쉿, 고양이가 낮잠 자고 있어."),
                        D("dabyeol", "다별", "조용히 보라 블록을 모아보자."),
                        D("capymong", "카피몽", "느긋하게 해도 괜찮아.")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "조용하고 깔끔하게 성공했어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "고양이처럼 쉬었다 다시 해보자.") }
                },
                new StoryRow
                {
                    storyId = 111, linkedStageId = 11,
                    storyTitle = "강아지 꼬리흔들 이야기",
                    learningTip = "연쇄가 생기면 목표 점수에 빨리 닿아요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "강아지가 신나게 꼬리를 흔들어!"),
                        D("poporing", "포포링", "톡톡 튀는 매치를 찾아보자!"),
                        D("dabyeol", "다별", "연쇄가 생기면 점수가 커져.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "신나게 성공했어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "연쇄가 생길 자리를 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 112, linkedStageId = 12,
                    storyTitle = "햄스터 씨앗창고 이야기",
                    learningTip = "수집 목표는 같은 색을 많이 모아야 해요.",
                    startDialogues = new[] {
                        D("capymong", "카피몽", "햄스터가 씨앗을 모으고 있어."),
                        D("nabyeol", "나별", "분홍 블록을 창고에 채워보자!"),
                        D("dabyeol", "다별", "목표 개수를 잘 확인해.")
                    },
                    clearDialogues = new[] { D("capymong", "카피몽", "씨앗창고가 가득 찼어.") },
                    failDialogues  = new[] { D("nabyeol", "나별", "다음엔 분홍 블록을 더 찾아보자!") }
                },
                new StoryRow
                {
                    storyId = 113, linkedStageId = 13,
                    storyTitle = "팬더 대나무숲 이야기",
                    learningTip = "큰 점수는 차분한 선택에서 나와요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "팬더의 대나무숲은 조용해."),
                        D("capymong", "카피몽", "차분히 보면 좋은 길이 보여."),
                        D("nabyeol", "나별", "큰 점수에 도전해 보자!")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "차분하게 잘 풀었어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 한 번 더 천천히 보자.") }
                },
                new StoryRow
                {
                    storyId = 114, linkedStageId = 14,
                    storyTitle = "코알라 꿈나무 이야기",
                    learningTip = "목표 블록을 끝까지 모으면 성공이에요.",
                    startDialogues = new[] {
                        D("capymong", "카피몽", "코알라가 꿈나무 위에서 쉬고 있어."),
                        D("dabyeol", "다별", "파란 블록을 끝까지 모아보자."),
                        D("poporing", "포포링", "방울처럼 톡톡 찾아줄게!")
                    },
                    clearDialogues = new[] { D("capymong", "카피몽", "꿈나무가 반짝 깨어났어.") },
                    failDialogues  = new[] { D("dabyeol", "다별", "목표 개수를 다시 확인해 보자.") }
                },
                new StoryRow
                {
                    storyId = 115, linkedStageId = 15,
                    storyTitle = "무지개 숲친구 이야기",
                    learningTip = "여러 블록을 맞추며 목표 점수를 모아요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "방울숲 친구들이 모두 모였어!"),
                        D("dabyeol", "다별", "마지막 목표 점수에 도전하자."),
                        D("capymong", "카피몽", "함께라면 천천히 가도 괜찮아.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "방울숲을 모두 밝혔어!") },
                    failDialogues  = new[] { D("capymong", "카피몽", "마지막 길도 다시 하면 열릴 거야.") }
                }
            };
        }
    }
}
