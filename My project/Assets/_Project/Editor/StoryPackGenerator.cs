using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 5개 StoryPack 자산을 자동 생성/업데이트:
    ///   1. BubbleForest (1~15)
    ///   2. MoonRiceCakeStairs (16~30)
    ///   3. Boss (31~32)
    ///   4. Prologue (1)
    ///   5. NonoSpecialEvent (31)
    /// 기존 StoryNode 자산을 AssetDatabase.FindAssets로 스캔해 각 팩의 범위에 맞게 자동 연결한다.
    /// 사건 템플릿 샘플도 함께 포함. 씬에 StoryPackManager가 있으면 database 슬롯 자동 attach.
    /// </summary>
    public static class StoryPackGenerator
    {
        private const string OutputFolder = "Assets/_Project/Data/StoryPacks";
        private const string DatabasePath = "Assets/_Project/Data/StoryPacks/StoryPackDatabase.asset";
        private const string StoryRoot = "Assets/_Project/Data/Story";

        private struct PackRow
        {
            public string fileTag;
            public string packId;
            public string packName;
            public StoryPackType packType;
            public string description;
            public int startStageId;
            public int endStageId;
            public StoryEventTemplateRow[] events;
        }

        private struct StoryEventTemplateRow
        {
            public string eventId;
            public string eventName;
            public StoryEventType eventType;
            public string eventDescription;
            public DialogueLineRow[] lines;
        }

        private struct DialogueLineRow
        {
            public string speakerId;
            public string speakerName;
            public string dialogue;
        }

        [MenuItem("Tools/NabyeolDabyeol/Generate Story Packs")]
        public static void GenerateAll()
        {
            EnsureFolder(OutputFolder);

            // 기존 StoryNode 자산 일괄 로드
            List<StoryNode> allNodes = LoadAllStoryNodes();
            Debug.Log($"StoryPackGenerator: Discovered {allNodes.Count} StoryNode assets under {StoryRoot}.");

            PackRow[] rows = BuildRows();
            int created = 0, updated = 0;
            StoryPackData[] generated = new StoryPackData[rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                PackRow row = rows[i];
                string assetPath = $"{OutputFolder}/StoryPack_{row.fileTag}.asset";
                StoryPackData existing = AssetDatabase.LoadAssetAtPath<StoryPackData>(assetPath);
                bool isNew = existing == null;
                StoryPackData asset = existing != null ? existing : ScriptableObject.CreateInstance<StoryPackData>();

                ApplyRow(asset, row, allNodes);

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
                generated[i] = asset;
            }

            // Database
            StoryPackDatabase db = AssetDatabase.LoadAssetAtPath<StoryPackDatabase>(DatabasePath);
            bool dbIsNew = db == null;
            if (db == null) db = ScriptableObject.CreateInstance<StoryPackDatabase>();

            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty packsList = dbSo.FindProperty("packs");
            packsList.arraySize = generated.Length;
            for (int i = 0; i < generated.Length; i++)
            {
                packsList.GetArrayElementAtIndex(i).objectReferenceValue = generated[i];
            }
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            if (dbIsNew) AssetDatabase.CreateAsset(db, DatabasePath);
            else EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"StoryPackGenerator: Packs Created {created}, Updated {updated} (total {created + updated}).");
            Debug.Log($"StoryPackGenerator: Database {(dbIsNew ? "created" : "updated")} at {DatabasePath}.");

            StoryPackDatabase loaded = AssetDatabase.LoadAssetAtPath<StoryPackDatabase>(DatabasePath);
            if (loaded != null)
            {
                bool ok = loaded.ValidatePacks();
                Debug.Log($"StoryPackGenerator: ValidatePacks = {ok} (count={loaded.Count}).");
            }

            // 씬의 StoryPackManager 자동 attach
            StoryPackManager mgr = Object.FindAnyObjectByType<StoryPackManager>();
            if (mgr != null)
            {
                SerializedObject so = new SerializedObject(mgr);
                SerializedProperty dbProp = so.FindProperty("database");
                if (dbProp != null)
                {
                    dbProp.objectReferenceValue = loaded;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mgr);
                    AssetDatabase.SaveAssets();
                    Debug.Log("StoryPackGenerator: Attached database to scene StoryPackManager.");
                }
            }
            else
            {
                Debug.Log("StoryPackGenerator: No StoryPackManager found in active scene. Drag the asset manually into Database slot.");
            }
        }

        private static List<StoryNode> LoadAllStoryNodes()
        {
            List<StoryNode> result = new List<StoryNode>();
            string[] roots = AssetDatabase.IsValidFolder(StoryRoot)
                ? new[] { StoryRoot }
                : new[] { "Assets" };
            string[] guids = AssetDatabase.FindAssets("t:StoryNode", roots);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                StoryNode n = AssetDatabase.LoadAssetAtPath<StoryNode>(path);
                if (n != null) result.Add(n);
            }
            return result;
        }

        private static void ApplyRow(StoryPackData asset, PackRow row, List<StoryNode> allNodes)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("packId").stringValue = row.packId;
            so.FindProperty("packName").stringValue = row.packName;
            so.FindProperty("packType").enumValueIndex = (int)row.packType;
            so.FindProperty("description").stringValue = row.description;
            so.FindProperty("startStageId").intValue = row.startStageId;
            so.FindProperty("endStageId").intValue = row.endStageId;

            // 범위에 맞는 StoryNode 자동 등록
            SerializedProperty nodesProp = so.FindProperty("storyNodes");
            List<StoryNode> matchedNodes = new List<StoryNode>();
            for (int i = 0; i < allNodes.Count; i++)
            {
                StoryNode n = allNodes[i];
                if (n == null) continue;
                if (n.LinkedStageId >= row.startStageId && n.LinkedStageId <= row.endStageId)
                {
                    matchedNodes.Add(n);
                }
            }
            matchedNodes.Sort((a, b) => a.LinkedStageId.CompareTo(b.LinkedStageId));

            nodesProp.arraySize = matchedNodes.Count;
            for (int i = 0; i < matchedNodes.Count; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = matchedNodes[i];
            }

            // 사건 템플릿
            SerializedProperty eventsProp = so.FindProperty("eventTemplates");
            eventsProp.arraySize = row.events == null ? 0 : row.events.Length;
            if (row.events != null)
            {
                for (int i = 0; i < row.events.Length; i++)
                {
                    SerializedProperty e = eventsProp.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("eventId").stringValue = row.events[i].eventId;
                    e.FindPropertyRelative("eventName").stringValue = row.events[i].eventName;
                    e.FindPropertyRelative("eventType").enumValueIndex = (int)row.events[i].eventType;
                    e.FindPropertyRelative("eventDescription").stringValue = row.events[i].eventDescription;

                    SerializedProperty linesProp = e.FindPropertyRelative("eventDialogues");
                    DialogueLineRow[] lines = row.events[i].lines;
                    linesProp.arraySize = lines == null ? 0 : lines.Length;
                    if (lines != null)
                    {
                        for (int j = 0; j < lines.Length; j++)
                        {
                            SerializedProperty line = linesProp.GetArrayElementAtIndex(j);
                            line.FindPropertyRelative("speakerId").stringValue = lines[j].speakerId;
                            line.FindPropertyRelative("speakerName").stringValue = lines[j].speakerName;
                            line.FindPropertyRelative("dialogue").stringValue = lines[j].dialogue;
                        }
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = "StoryPack_" + row.fileTag;
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

        private static DialogueLineRow L(string id, string name, string dlg)
        {
            return new DialogueLineRow { speakerId = id, speakerName = name, dialogue = dlg };
        }

        private static PackRow[] BuildRows()
        {
            return new PackRow[]
            {
                new PackRow {
                    fileTag = "BubbleForest", packId = "pack_bubble_forest", packName = "방울숲 이야기",
                    packType = StoryPackType.Region,
                    description = "방울숲 1~15 스테이지의 동물 친구 대사를 모은 팩입니다.",
                    startStageId = 1, endStageId = 15,
                    events = new[] {
                        new StoryEventTemplateRow {
                            eventId = "region_intro_bubble_forest",
                            eventName = "방울숲 시작",
                            eventType = StoryEventType.RegionIntro,
                            eventDescription = "방울숲에 처음 들어올 때 보여줄 수 있는 짧은 사건입니다.",
                            lines = new[] {
                                L("nabyeol", "나별", "방울숲이 우리를 기다리고 있어!"),
                                L("dabyeol", "다별", "동물 친구들의 길을 하나씩 밝혀보자.")
                            }
                        }
                    }
                },
                new PackRow {
                    fileTag = "MoonRiceCakeStairs", packId = "pack_moon_ricecake_stairs", packName = "달떡계단 이야기",
                    packType = StoryPackType.Region,
                    description = "달떡계단 16~30 스테이지의 숫자/순서/덧셈 대사를 모은 팩입니다.",
                    startStageId = 16, endStageId = 30,
                    events = new[] {
                        new StoryEventTemplateRow {
                            eventId = "region_intro_moon_stairs",
                            eventName = "달떡계단 시작",
                            eventType = StoryEventType.RegionIntro,
                            eventDescription = "숫자 계단을 처음 오를 때 보여줄 수 있는 사건입니다.",
                            lines = new[] {
                                L("dabyeol",  "다별",   "숫자 계단은 순서대로 보면 쉬워."),
                                L("mochirun", "모찌룬", "하나씩 정리하며 올라가 보자.")
                            }
                        }
                    }
                },
                new PackRow {
                    fileTag = "Boss", packId = "pack_boss", packName = "보스 클라이맥스",
                    packType = StoryPackType.Boss,
                    description = "기억나무·거꾸로 시계탑 같은 보스 스테이지 대사를 모은 팩입니다.",
                    startStageId = 31, endStageId = 32,
                    events = new[] {
                        new StoryEventTemplateRow {
                            eventId = "boss_entry_memory_tree",
                            eventName = "기억나무 등장",
                            eventType = StoryEventType.BossEntry,
                            eventDescription = "기억나무 보스 스테이지 시작 직전 등장 컷.",
                            lines = new[] {
                                L("memory_tree", "기억나무", "작은 기억 조각들이 숲에 흩어졌구나."),
                                L("nabyeol",     "나별",     "우리가 초록 기억 블록을 모아줄게!")
                            }
                        }
                    }
                },
                new PackRow {
                    fileTag = "Prologue", packId = "pack_prologue", packName = "프롤로그",
                    packType = StoryPackType.Prologue,
                    description = "게임을 처음 시작할 때 만나는 캐릭터 첫 인사 팩입니다. (linkedStageId 1)",
                    startStageId = 1, endStageId = 1,
                    events = new[] {
                        new StoryEventTemplateRow {
                            eventId = "prologue_first_meet",
                            eventName = "프롤로그 첫 만남",
                            eventType = StoryEventType.FirstMeet,
                            eventDescription = "나별·다별·카피몽의 첫 만남 인트로.",
                            lines = new[] {
                                L("nabyeol",  "나별",   "어? 여긴 반짝반짝한 숲이야!"),
                                L("dabyeol",  "다별",   "나별, 조심해. 길이 퍼즐로 막혀 있어."),
                                L("capymong", "카피몽", "안녕… 나는 카피몽이야. 이 숲에 살고 있어.")
                            }
                        }
                    }
                },
                new PackRow {
                    fileTag = "NonoSpecialEvent", packId = "pack_nono_special", packName = "노노 첫 등장",
                    packType = StoryPackType.SpecialEvent,
                    description = "노노가 처음 장난스럽게 등장하는 특별 사건 팩입니다. (linkedStageId 31에 추가)",
                    startStageId = 31, endStageId = 31,
                    events = new[] {
                        new StoryEventTemplateRow {
                            eventId = "nono_first_appearance",
                            eventName = "노노 첫 등장",
                            eventType = StoryEventType.FirstMeet,
                            eventDescription = "노노가 처음 장난스럽게 등장하는 사건입니다.",
                            lines = new[] {
                                L("nono",    "노노",   "히히, 여긴 내가 살짝 바꿔놨어!"),
                                L("nabyeol", "나별",   "어? 너는 누구야?"),
                                L("nono",    "노노",   "나는 노노! 퍼즐 장난을 좋아해.")
                            }
                        }
                    }
                }
            };
        }
    }
}
