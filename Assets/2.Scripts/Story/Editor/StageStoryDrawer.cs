using UnityEditor;
using UnityEngine;

// One row per stage: [label] [x 스토리 재생] [x 전투없음] [scenario SO]
[CustomPropertyDrawer(typeof(StageStoryTable.StageStory))]
public class StageStoryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty stageLabel = property.FindPropertyRelative("label");
        SerializedProperty playStory = property.FindPropertyRelative("playStory");
        SerializedProperty noBattle = property.FindPropertyRelative("noBattle");
        SerializedProperty scenario = property.FindPropertyRelative("scenario");

        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect labelRect = new Rect(position.x, position.y, 60f, position.height);
        Rect storyRect = new Rect(labelRect.xMax + 4f, position.y, 90f, position.height);
        Rect noBattleRect = new Rect(storyRect.xMax + 4f, position.y, 78f, position.height);
        Rect scenarioRect = new Rect(noBattleRect.xMax + 4f, position.y, position.xMax - noBattleRect.xMax - 4f, position.height);

        stageLabel.stringValue = EditorGUI.TextField(labelRect, stageLabel.stringValue);
        playStory.boolValue = EditorGUI.ToggleLeft(storyRect, "스토리 재생", playStory.boolValue);
        using (new EditorGUI.DisabledScope(!playStory.boolValue))
        {
            noBattle.boolValue = EditorGUI.ToggleLeft(noBattleRect,
                new GUIContent("전투없음", "체크 시 스토리가 끝나면 전투 없이 3.Stage List로 돌아갑니다."), noBattle.boolValue);
            EditorGUI.PropertyField(scenarioRect, scenario, GUIContent.none);
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
