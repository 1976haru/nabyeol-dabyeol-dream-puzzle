using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using NabyeolDabyeolDreamPuzzle.Agents;
using NabyeolDabyeolDreamPuzzle.Stage;

namespace NabyeolDabyeolDreamPuzzle.EditorTools
{
    /// <summary>
    /// 가상 플레이 시뮬레이터 Editor 윈도우.
    /// 단일 StageData 또는 StagePackDatabase 전체를 N회 반복 시뮬레이션해 clearRate/난이도/추천을 보고.
    /// 메뉴: Tools/NabyeolDabyeol/Virtual Play Simulator
    /// 시뮬레이션 결과는 참고용. 실제 아이 플레이와 다를 수 있음.
    /// </summary>
    public class VirtualPlaySimulatorWindow : EditorWindow
    {
        private StageData stageData;
        private StagePackDatabase stagePackDatabase;
        private int simulationCount = 100;
        private int seed = 0;
        private Vector2 scroll;
        private List<VirtualStageReport> lastReports = new List<VirtualStageReport>();

        [MenuItem("Tools/NabyeolDabyeol/Virtual Play Simulator")]
        public static void Open()
        {
            VirtualPlaySimulatorWindow w = GetWindow<VirtualPlaySimulatorWindow>("Virtual Play Sim");
            w.minSize = new Vector2(620, 420);
            w.AutoDiscover();
            w.Show();
        }

        private void OnEnable()
        {
            if (stagePackDatabase == null) AutoDiscover();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Virtual Play Simulator v1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("int[,] 가상 보드로 N회 반복 시뮬레이션. 결과는 참고용이며 실제 아이 플레이와 다를 수 있습니다. 원본 StageData와 PlayerPrefs는 변경되지 않습니다.", MessageType.Info);

            EditorGUILayout.Space();
            stageData = (StageData) EditorGUILayout.ObjectField("Target StageData", stageData, typeof(StageData), false);
            stagePackDatabase = (StagePackDatabase) EditorGUILayout.ObjectField("StagePackDatabase", stagePackDatabase, typeof(StagePackDatabase), false);
            simulationCount = EditorGUILayout.IntSlider("Simulation Count", simulationCount, 10, 1000);
            seed = EditorGUILayout.IntField("Random Seed (0=auto)", seed);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Re-discover", GUILayout.Width(120))) AutoDiscover();
                if (GUILayout.Button("Run Selected Stage", GUILayout.Height(26))) RunSingleStage();
                if (GUILayout.Button("Run All Stages", GUILayout.Height(26))) RunAllStages();
            }

            EditorGUILayout.Space();
            if (lastReports != null && lastReports.Count > 0)
            {
                EditorGUILayout.LabelField($"Reports ({lastReports.Count}):", EditorStyles.boldLabel);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < lastReports.Count; i++)
                {
                    VirtualStageReport r = lastReports[i];
                    if (r == null) continue;
                    Color prev = GUI.color;
                    GUI.color = ColorForGrade(r.grade);
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(r.Summary(), EditorStyles.wordWrappedLabel);
                    if (r.failReasonCounts != null && r.failReasonCounts.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("Fail reasons: ");
                        bool first = true;
                        foreach (KeyValuePair<string, int> kv in r.failReasonCounts)
                        {
                            if (!first) sb.Append(", ");
                            sb.Append($"{kv.Key}({kv.Value})");
                            first = false;
                        }
                        EditorGUILayout.LabelField(sb.ToString(), EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                    GUI.color = prev;
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("아직 시뮬레이션 결과가 없습니다. 'Run Selected Stage' 또는 'Run All Stages'를 눌러 주세요.", MessageType.None);
            }
        }

        private static Color ColorForGrade(string grade)
        {
            switch (grade)
            {
                case "Easy":    return new Color(0.85f, 1.00f, 0.85f);
                case "Normal":  return new Color(0.92f, 0.96f, 1.00f);
                case "Hard":    return new Color(1.00f, 0.95f, 0.65f);
                case "TooHard": return new Color(1.00f, 0.70f, 0.70f);
                default:        return new Color(0.92f, 0.92f, 0.92f);
            }
        }

        private void AutoDiscover()
        {
            stagePackDatabase = FindFirstAsset<StagePackDatabase>();
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids == null || guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void RunSingleStage()
        {
            if (stageData == null)
            {
                Debug.LogWarning("VirtualPlaySimulatorWindow: stageData not assigned.");
                return;
            }
            VirtualPlaySimulator sim = new VirtualPlaySimulator(seed);
            VirtualStageReport report = sim.SimulateStage(stageData, simulationCount);
            lastReports = new List<VirtualStageReport> { report };
            PrintReports(lastReports);
            Repaint();
        }

        private void RunAllStages()
        {
            if (stagePackDatabase == null)
            {
                Debug.LogWarning("VirtualPlaySimulatorWindow: stagePackDatabase not assigned.");
                return;
            }
            VirtualPlaySimulator sim = new VirtualPlaySimulator(seed);
            lastReports = sim.SimulateAllStages(stagePackDatabase, simulationCount);
            PrintReports(lastReports);
            Repaint();
        }

        private static void PrintReports(List<VirtualStageReport> reports)
        {
            if (reports == null) return;
            int easy = 0, normal = 0, hard = 0, tooHard = 0, invalid = 0;
            for (int i = 0; i < reports.Count; i++)
            {
                VirtualStageReport r = reports[i];
                if (r == null) continue;
                switch (r.grade)
                {
                    case "Easy":    easy++; break;
                    case "Normal":  normal++; break;
                    case "Hard":    hard++; break;
                    case "TooHard": tooHard++; break;
                    default:        invalid++; break;
                }
                Debug.Log("[VirtualPlaySim] " + r.Summary());
            }
            Debug.Log($"[VirtualPlaySim] (참고용) Total={reports.Count}, Easy={easy}, Normal={normal}, Hard={hard}, TooHard={tooHard}, Invalid={invalid}");
        }
    }
}
