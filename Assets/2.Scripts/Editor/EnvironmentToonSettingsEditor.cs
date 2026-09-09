using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnvironmentToonSettings))]
public class EnvironmentToonSettingsEditor : Editor
{
    private bool foldMidTone = true;
    private bool foldBuilding = true;
    private bool foldTree = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 비교용 ──
        DrawHeader("비교용");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("disableToon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("revertLighting"));

        // ── 배경 셰이딩 ──
        DrawHeader("배경 셰이딩");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyShading"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReplaceShader"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("flatten"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowThreshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowFeather"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowTint"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("receiveShadowStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("environmentInfluence"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientFlatten"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brightness"));

        // ── 건물 / 나무 분리 설정 (접이식) ──
        EditorGUILayout.Space(6);
        foldMidTone = EditorGUILayout.BeginFoldoutHeaderGroup(foldMidTone, "건물 / 나무 설정");
        if (foldMidTone)
        {
            EditorGUI.indentLevel++;

            foldBuilding = EditorGUILayout.Foldout(foldBuilding, "건물", true);
            if (foldBuilding)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineWidthBuilding"), new GUIContent("Outline Width"));
                EditorGUILayout.LabelField("3톤 그림자", EditorStyles.miniBoldLabel);
                DrawMidToneGroup(serializedObject.FindProperty("midToneBuilding"));
                EditorGUI.indentLevel--;
            }

            foldTree = EditorGUILayout.Foldout(foldTree, "나무", true);
            if (foldTree)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineWidthTree"), new GUIContent("Outline Width"));
                EditorGUILayout.LabelField("3톤 그림자", EditorStyles.miniBoldLabel);
                DrawMidToneGroup(serializedObject.FindProperty("midToneTree"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ── 나머지 섹션 ──
        EditorGUILayout.PropertyField(serializedObject.FindProperty("specularIntensity"));

        DrawHeader("키 라이트");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keyLight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("disableExtraDirectionalLights"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyLight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightIntensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightAngleVertical"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightAngleHorizontal"));

        DrawHeader("환경광");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyAmbient"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientSky"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientEquator"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientGround"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientIntensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reflectionIntensity"));

        DrawHeader("안개 (공기원근)");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFog"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fogStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fogEnd"));

        DrawHeader("포스트 프로세싱");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeProfile"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyPost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("postExposure"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("postContrast"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("postSaturation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowColorGrade"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowLift"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bloomThreshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bloomIntensity"));

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawMidToneGroup(SerializedProperty group)
    {
        EditorGUILayout.PropertyField(group.FindPropertyRelative("enabled"), new GUIContent("활성화"));
        EditorGUILayout.PropertyField(group.FindPropertyRelative("tint"), new GUIContent("Tint"));
        EditorGUILayout.PropertyField(group.FindPropertyRelative("threshold"), new GUIContent("Threshold"));
        EditorGUILayout.PropertyField(group.FindPropertyRelative("feather"), new GUIContent("Feather"));
    }

    private static void DrawHeader(string label)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }
}
