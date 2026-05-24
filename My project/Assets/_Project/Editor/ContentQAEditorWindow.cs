using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Album;
using NabyeolDabyeolDreamPuzzle.Cards;
using NabyeolDabyeolDreamPuzzle.Character;
using NabyeolDabyeolDreamPuzzle.Dialogue;
using NabyeolDabyeolDreamPuzzle.Learning;
using NabyeolDabyeolDreamPuzzle.QA;
using NabyeolDabyeolDreamPuzzle.Stage;
using NabyeolDabyeolDreamPuzzle.Story;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// Content QA Editor Window. AssetDatabase로 Database들을 자동 탐색하고 ContentQAAgent에 주입한다.
    /// 결과는 창과 Console 양쪽에 표시. UnityEditor를 런타임 코드에 가져오지 않도록 Editor 폴더에 격리.
    /// 메뉴: Tools/NabyeolDabyeol/Content QA Check
    /// TODO: Add one-click select asset from QA window.
    /// TODO: Add severity filter tabs.
    /// </summary>
    public class ContentQAEditorWindow : EditorWindow
    {
        private ContentQAReport lastReport;
        private Vector2 scroll;
        private bool showInfo = true;
        private bool showWarning = true;
        private bool showError = true;

        // 인스펙터에서 수동 지정도 가능하도록 노출
        private DialogueDatabase dialogueDb;
        private CharacterPackDatabase characterDb;
        private KnowledgeCardDatabase cardDb;
        private StagePackDatabase stageDb;
        private StoryPackDatabase storyDb;
        private AlbumDatabase albumDb;
        private LearningPackDatabase learningDb;

        [MenuItem("Tools/NabyeolDabyeol/Content QA Check")]
        public static void Open()
        {
            ContentQAEditorWindow w = GetWindow<ContentQAEditorWindow>("Content QA");
            w.minSize = new Vector2(640, 420);
            w.AutoDiscoverDatabases();
            w.Show();
        }

        private void OnEnable()
        {
            if (dialogueDb == null) AutoDiscoverDatabases();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Content QA v1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("AssetDatabase에서 Database들을 자동으로 찾아 QA 검사를 실행합니다. 자동 수정 없음.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Databases (자동 탐색)", EditorStyles.boldLabel);
            dialogueDb  = (DialogueDatabase) EditorGUILayout.ObjectField("Dialogue", dialogueDb, typeof(DialogueDatabase), false);
            characterDb = (CharacterPackDatabase) EditorGUILayout.ObjectField("Character Pack", characterDb, typeof(CharacterPackDatabase), false);
            cardDb      = (KnowledgeCardDatabase) EditorGUILayout.ObjectField("Knowledge Card", cardDb, typeof(KnowledgeCardDatabase), false);
            stageDb     = (StagePackDatabase) EditorGUILayout.ObjectField("Stage Pack", stageDb, typeof(StagePackDatabase), false);
            storyDb     = (StoryPackDatabase) EditorGUILayout.ObjectField("Story Pack", storyDb, typeof(StoryPackDatabase), false);
            albumDb     = (AlbumDatabase) EditorGUILayout.ObjectField("Album", albumDb, typeof(AlbumDatabase), false);
            learningDb  = (LearningPackDatabase) EditorGUILayout.ObjectField("Learning Pack", learningDb, typeof(LearningPackDatabase), false);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Re-discover Databases", GUILayout.Width(180)))
                {
                    AutoDiscoverDatabases();
                }
                if (GUILayout.Button("Run Full Check", GUILayout.Height(28)))
                {
                    RunCheck();
                }
            }

            EditorGUILayout.Space();
            if (lastReport != null)
            {
                EditorGUILayout.LabelField(lastReport.Summary(), EditorStyles.helpBox);
                using (new EditorGUILayout.HorizontalScope())
                {
                    showError   = GUILayout.Toggle(showError,   $"Error ({lastReport.errorCount})",   "Button");
                    showWarning = GUILayout.Toggle(showWarning, $"Warning ({lastReport.warningCount})", "Button");
                    showInfo    = GUILayout.Toggle(showInfo,    $"Info ({lastReport.infoCount})",    "Button");
                }

                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < lastReport.results.Count; i++)
                {
                    ContentQAResult r = lastReport.results[i];
                    if (r == null) continue;
                    if (r.severity == ContentQASeverity.Error && !showError) continue;
                    if (r.severity == ContentQASeverity.Warning && !showWarning) continue;
                    if (r.severity == ContentQASeverity.Info && !showInfo) continue;

                    Color prev = GUI.color;
                    GUI.color = ColorForSeverity(r.severity);
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"[{r.severity}] {r.category}  ·  {r.assetName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(r.message, EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrEmpty(r.assetPath))
                    {
                        EditorGUILayout.LabelField($"path: {r.assetPath}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                    GUI.color = prev;
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("아직 검사 결과가 없습니다. 'Run Full Check'를 눌러 주세요.", MessageType.None);
            }
        }

        private static Color ColorForSeverity(ContentQASeverity s)
        {
            switch (s)
            {
                case ContentQASeverity.Error:   return new Color(1.00f, 0.70f, 0.70f);
                case ContentQASeverity.Warning: return new Color(1.00f, 0.95f, 0.65f);
                default:                        return new Color(0.85f, 0.95f, 1.00f);
            }
        }

        private void AutoDiscoverDatabases()
        {
            dialogueDb  = FindFirstAsset<DialogueDatabase>();
            characterDb = FindFirstAsset<CharacterPackDatabase>();
            cardDb      = FindFirstAsset<KnowledgeCardDatabase>();
            stageDb     = FindFirstAsset<StagePackDatabase>();
            storyDb     = FindFirstAsset<StoryPackDatabase>();
            albumDb     = FindFirstAsset<AlbumDatabase>();
            learningDb  = FindFirstAsset<LearningPackDatabase>();
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids == null || guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void RunCheck()
        {
            ContentQAAgent agent = new ContentQAAgent
            {
                dialogueDatabase = dialogueDb,
                characterPackDatabase = characterDb,
                knowledgeCardDatabase = cardDb,
                stagePackDatabase = stageDb,
                storyPackDatabase = storyDb,
                albumDatabase = albumDb,
                learningPackDatabase = learningDb
            };
            lastReport = agent.RunFullCheck();

            // assetPath 보강 — 가능한 경우 AssetDatabase 경로를 다시 채움
            TryFillAssetPaths(lastReport);

            // Console 출력
            for (int i = 0; i < lastReport.results.Count; i++)
            {
                ContentQAResult r = lastReport.results[i];
                if (r == null) continue;
                string line = $"[ContentQA] [{r.severity}] {r.category} | {r.assetName} | {r.message}";
                if (r.severity == ContentQASeverity.Error)        Debug.LogError(line);
                else if (r.severity == ContentQASeverity.Warning) Debug.LogWarning(line);
                else                                              Debug.Log(line);
            }
            Debug.Log("[ContentQA] " + lastReport.Summary());
            Repaint();
        }

        private void TryFillAssetPaths(ContentQAReport report)
        {
            if (report == null) return;
            // 자산 경로 캐시 — 등록된 Database 자체의 경로만 보강. 세부 라인은 보고에 포함된 assetName으로 검색 어렵다.
            string[] paths = new string[]
            {
                dialogueDb != null  ? AssetDatabase.GetAssetPath(dialogueDb)  : null,
                characterDb != null ? AssetDatabase.GetAssetPath(characterDb) : null,
                cardDb != null      ? AssetDatabase.GetAssetPath(cardDb)      : null,
                stageDb != null     ? AssetDatabase.GetAssetPath(stageDb)     : null,
                storyDb != null     ? AssetDatabase.GetAssetPath(storyDb)     : null,
                albumDb != null     ? AssetDatabase.GetAssetPath(albumDb)     : null,
                learningDb != null  ? AssetDatabase.GetAssetPath(learningDb)  : null
            };
            Dictionary<string, string> categoryToPath = new Dictionary<string, string>
            {
                { "DialogueDatabase", paths[0] },
                { "CharacterPack",    paths[1] },
                { "KnowledgeCard",    paths[2] },
                { "StagePack",        paths[3] },
                { "StoryPack",        paths[4] },
                { "Album",            paths[5] },
                { "LearningPack",     paths[6] }
            };
            for (int i = 0; i < report.results.Count; i++)
            {
                ContentQAResult r = report.results[i];
                if (r == null) continue;
                if (!string.IsNullOrEmpty(r.assetPath)) continue;
                if (r.category != null && categoryToPath.TryGetValue(r.category, out string p))
                {
                    r.assetPath = p ?? string.Empty;
                }
            }
        }
    }
}
