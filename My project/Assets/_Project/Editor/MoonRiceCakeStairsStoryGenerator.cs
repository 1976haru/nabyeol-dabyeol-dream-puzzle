using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 달떡계단 16~30 스테이지에 매칭되는 StoryNode 자동 생성/업데이트.
    /// 숫자·순서·덧셈 콘셉트의 짧은 대사 (각 스테이지 startDialogues 3줄).
    /// 모찌룬·다별 비중을 높이고, 어린이 친화 표현(차례차례, 나란히 등)만 사용.
    /// TODO: Add StoryPopup playback for stage start.
    /// TODO: Show clear/fail dialogues alongside popups.
    /// TODO: Link speakerId to CharacterData portrait automatically.
    /// </summary>
    public static class MoonRiceCakeStairsStoryGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/Story/MoonRiceCakeStairs";
        private const string FileNameFormat = "Story_MoonRiceCakeStairs_Stage_{0:D3}";

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

        [MenuItem("Tools/NabyeolDabyeol/Generate MoonRiceCakeStairs Stories")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            StoryRow[] rows = BuildRows();
            int created = 0, updated = 0;

            foreach (StoryRow row in rows)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, row.linkedStageId)}.asset";
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

            Debug.Log($"MoonRiceCakeStairsStoryGenerator: Generation done. Created {created}, Updated {updated} (total {created + updated}).");

            int validCount = 0;
            foreach (StoryRow row in rows)
            {
                string assetPath = $"{OutputFolder}/{string.Format(FileNameFormat, row.linkedStageId)}.asset";
                StoryNode sn = AssetDatabase.LoadAssetAtPath<StoryNode>(assetPath);
                if (sn != null && sn.IsValid())
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"MoonRiceCakeStairsStoryGenerator: Stage {row.linkedStageId} failed validation at {assetPath}.");
                }
            }
            Debug.Log($"MoonRiceCakeStairsStoryGenerator: Validation done. {validCount}/{rows.Length} stories passed IsValid().");
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
                    storyId = 216, linkedStageId = 16,
                    storyTitle = "하나둘 달계단 이야기",
                    learningTip = "순서대로 살펴보면 맞출 블록이 보여요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "하나, 둘, 차례대로 올라가 보자."),
                        D("nabyeol", "나별", "달떡계단 첫 발을 내딛는 거야!"),
                        D("mochirun", "모찌룬", "숫자처럼 차근차근 보면 쉬워.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "좋아! 첫 계단을 올랐어!") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 하나씩 다시 보면 돼.") }
                },
                new StoryRow
                {
                    storyId = 217, linkedStageId = 17,
                    storyTitle = "셋째 떡발판 이야기",
                    learningTip = "목표 블록을 먼저 찾으면 쉬워요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "셋째 발판에는 노란 떡이 있어."),
                        D("mochirun", "모찌룬", "노란 블록을 차례차례 모아보자."),
                        D("nabyeol", "나별", "셋까지 세고 시작!")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "셋째 발판도 잘 찾았어.") },
                    failDialogues  = new[] { D("mochirun", "모찌룬", "노란 블록부터 다시 세어보자.") }
                },
                new StoryRow
                {
                    storyId = 218, linkedStageId = 18,
                    storyTitle = "숫자별 반짝 이야기",
                    learningTip = "작은 점수도 모이면 커져요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "숫자별이 반짝이고 있어!"),
                        D("dabyeol", "다별", "점수는 조금씩 더해져 커져."),
                        D("poporing", "포포링", "톡톡 반짝이는 길을 찾아봐!")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "숫자별이 더 밝아졌어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "작은 매치부터 다시 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 219, linkedStageId = 19,
                    storyTitle = "넷씩 모아보기 이야기",
                    learningTip = "같은 색을 묶어 보면 목표가 보여요.",
                    startDialogues = new[] {
                        D("mochirun", "모찌룬", "이번엔 넷씩 묶어 보는 길이야."),
                        D("dabyeol", "다별", "초록 블록을 차분히 모아보자."),
                        D("capymong", "카피몽", "하나씩 모으면 금방이야.")
                    },
                    clearDialogues = new[] { D("mochirun", "모찌룬", "초록 블록을 잘 정리했어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 다시 하나씩 모아보자.") }
                },
                new StoryRow
                {
                    storyId = 220, linkedStageId = 20,
                    storyTitle = "다섯달떡 완성 이야기",
                    learningTip = "여러 번 맞추면 목표 점수에 닿아요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "다섯 달떡이 기다리고 있어!"),
                        D("dabyeol", "다별", "점수를 차곡차곡 더해 보자."),
                        D("mochirun", "모찌룬", "정리하면 큰 점수도 만들 수 있어.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "다섯달떡 완성!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "점수 목표를 다시 확인해 보자.") }
                },
                new StoryRow
                {
                    storyId = 221, linkedStageId = 21,
                    storyTitle = "더하기 첫걸음 이야기",
                    learningTip = "목표 블록은 연쇄로도 모을 수 있어요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "작은 수를 더하면 큰 수가 돼."),
                        D("mochirun", "모찌룬", "파란 블록을 모아 더해보자."),
                        D("nabyeol", "나별", "좋아, 더하기 첫걸음이야!")
                    },
                    clearDialogues = new[] { D("mochirun", "모찌룬", "파란 블록을 잘 더했어.") },
                    failDialogues  = new[] { D("dabyeol", "다별", "파란 블록을 먼저 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 222, linkedStageId = 22,
                    storyTitle = "짝수 달빛길 이야기",
                    learningTip = "나란히 맞추면 길이 더 잘 보여요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "짝수처럼 가지런한 달빛길이야."),
                        D("nabyeol", "나별", "나란히 맞추면 반짝일 거야!"),
                        D("poporing", "포포링", "톡톡, 달빛이 길을 알려줘!")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "가지런하게 잘 풀었어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 천천히 다시 나란히 보자.") }
                },
                new StoryRow
                {
                    storyId = 223, linkedStageId = 23,
                    storyTitle = "홀수 별사탕 이야기",
                    learningTip = "목표 색을 놓치지 말고 모아보세요.",
                    startDialogues = new[] {
                        D("mochirun", "모찌룬", "홀수 별사탕은 톡톡 튀어."),
                        D("dabyeol", "다별", "이번엔 보라 블록을 모아야 해."),
                        D("nabyeol", "나별", "별사탕처럼 반짝 모아보자!")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "별사탕을 멋지게 모았어!") },
                    failDialogues  = new[] { D("mochirun", "모찌룬", "보라 블록을 다시 세어보자.") }
                },
                new StoryRow
                {
                    storyId = 224, linkedStageId = 24,
                    storyTitle = "일곱칸 점프 이야기",
                    learningTip = "큰 매치를 만들면 점수가 빨리 올라요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "일곱 칸을 폴짝 뛰어볼까?"),
                        D("dabyeol", "다별", "큰 매치를 만들면 점수가 커져."),
                        D("poporing", "포포링", "방울처럼 통통 뛰어보자!")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "일곱칸 점프 성공!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "큰 매치 자리를 다시 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 225, linkedStageId = 25,
                    storyTitle = "여덟떡 상자 이야기",
                    learningTip = "목표 개수를 확인하며 모아보세요.",
                    startDialogues = new[] {
                        D("mochirun", "모찌룬", "여덟떡 상자를 정리해 볼게."),
                        D("dabyeol", "다별", "분홍 블록을 목표만큼 모으자."),
                        D("capymong", "카피몽", "차곡차곡 넣으면 돼.")
                    },
                    clearDialogues = new[] { D("mochirun", "모찌룬", "상자가 보기 좋게 정리됐어.") },
                    failDialogues  = new[] { D("capymong", "카피몽", "다시 차곡차곡 모아보자.") }
                },
                new StoryRow
                {
                    storyId = 226, linkedStageId = 26,
                    storyTitle = "규칙찾기 계단 이야기",
                    learningTip = "반복되는 모양을 찾으면 쉬워요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "이 계단에는 규칙이 숨어 있어."),
                        D("mochirun", "모찌룬", "반복되는 블록을 찾아보자."),
                        D("nabyeol", "나별", "규칙을 찾으면 길이 열릴 거야!")
                    },
                    clearDialogues = new[] { D("dabyeol", "다별", "숨은 규칙을 잘 찾았어.") },
                    failDialogues  = new[] { D("mochirun", "모찌룬", "반복되는 모양을 다시 봐보자.") }
                },
                new StoryRow
                {
                    storyId = 227, linkedStageId = 27,
                    storyTitle = "열까지 세기 이야기",
                    learningTip = "같은 색을 세며 목표까지 모아보세요.",
                    startDialogues = new[] {
                        D("mochirun", "모찌룬", "하나부터 열까지 세어보자."),
                        D("dabyeol", "다별", "노란 블록을 정확히 모아야 해."),
                        D("capymong", "카피몽", "천천히 세면 틀리지 않아.")
                    },
                    clearDialogues = new[] { D("mochirun", "모찌룬", "열까지 잘 세었어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "노란 블록 개수를 다시 확인해 보자.") }
                },
                new StoryRow
                {
                    storyId = 228, linkedStageId = 28,
                    storyTitle = "덧셈달 높이 이야기",
                    learningTip = "연쇄 점수는 더하기처럼 커져요.",
                    startDialogues = new[] {
                        D("dabyeol", "다별", "점수가 더해지면 달까지 닿을 거야."),
                        D("nabyeol", "나별", "연쇄를 만들면 더 높이 올라가!"),
                        D("poporing", "포포링", "톡톡 더해서 반짝!")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "덧셈달까지 올라왔어!") },
                    failDialogues  = new[] { D("dabyeol", "다별", "연쇄가 생길 자리를 다시 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 229, linkedStageId = 29,
                    storyTitle = "순서대로 콩콩 이야기",
                    learningTip = "차례대로 찾으면 목표가 가까워져요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "콩콩! 순서대로 밟아보자."),
                        D("dabyeol", "다별", "파란 블록을 차례차례 모으면 돼."),
                        D("mochirun", "모찌룬", "순서를 지키면 더 쉬워져.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "순서대로 콩콩 성공!") },
                    failDialogues  = new[] { D("mochirun", "모찌룬", "파란 블록을 차례대로 다시 찾아보자.") }
                },
                new StoryRow
                {
                    storyId = 230, linkedStageId = 30,
                    storyTitle = "달떡계단 정상 이야기",
                    learningTip = "지금까지 배운 순서와 더하기를 떠올려요.",
                    startDialogues = new[] {
                        D("nabyeol", "나별", "드디어 달떡계단 정상 앞이야!"),
                        D("dabyeol", "다별", "순서와 더하기를 떠올려 보자."),
                        D("mochirun", "모찌룬", "마지막 숫자 길을 정리해 볼게.")
                    },
                    clearDialogues = new[] { D("nabyeol", "나별", "달떡계단을 모두 올랐어!") },
                    failDialogues  = new[] { D("capymong", "카피몽", "괜찮아. 정상은 다시 도전하면 돼.") }
                }
            };
        }
    }
}
