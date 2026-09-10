using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 적 AI의 판단 로그를 보는 창입니다.
/// 미션 가이드가 요구한 "AI 판단 로그"를 개발 중 확인하고
/// 발표 자료로 내보내는 용도입니다.
/// </summary>
public class EnemyAILogWindow : EditorWindow
{
    private Vector2 scroll;
    private bool echoToConsole = true;

    [MenuItem("Tools/PixelChroma/AI 판단 로그")]
    public static void Open()
    {
        EnemyAILogWindow window = GetWindow<EnemyAILogWindow>("AI 판단 로그");
        window.minSize = new Vector2(460f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        echoToConsole = EnemyAILog.EchoToConsole;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            EnemyAILog.Clear();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            bool next = EditorGUILayout.ToggleLeft(
                "Console에도 출력", echoToConsole, GUILayout.Width(140f));
            if (next != echoToConsole)
            {
                echoToConsole = next;
                EnemyAILog.EchoToConsole = next;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("비우기", GUILayout.Width(70f)))
            {
                EnemyAILog.Clear();
                Repaint();
            }

            if (GUILayout.Button("txt로 내보내기", GUILayout.Width(110f)))
                Export();
        }

        EditorGUILayout.Space(4);

        int count = EnemyAILog.Entries.Count;
        EditorGUILayout.LabelField(
            $"기록 {count}건 (최대 {EnemyAILog.MaxEntries}건 유지)",
            EditorStyles.miniLabel);

        if (count == 0)
        {
            EditorGUILayout.HelpBox(
                "아직 기록이 없습니다.\n" +
                "Play로 전투를 시작하고 적 턴이 지나가면 여기에 쌓입니다.",
                MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // 최신 기록이 위로 오도록 역순 출력합니다.
        for (int i = EnemyAILog.Entries.Count - 1; i >= 0; i--)
        {
            EnemyAILog.Entry entry = EnemyAILog.Entries[i];
            EditorGUILayout.SelectableLabel(
                entry.Text,
                EditorStyles.textArea,
                GUILayout.Height(EstimateHeight(entry.Text)));
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private static float EstimateHeight(string text)
    {
        int lines = 1;
        foreach (char c in text)
            if (c == '\n') lines++;
        return Mathf.Clamp(lines * 13f + 8f, 40f, 600f);
    }

    private void Export()
    {
        string path = EditorUtility.SaveFilePanel(
            "AI 판단 로그 내보내기",
            Application.dataPath,
            "EnemyAILog.txt",
            "txt");

        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, EnemyAILog.DumpAll());
        Debug.Log($"[EnemyAILog] 내보냄: {path}");
    }

    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying) Repaint();
    }
}
