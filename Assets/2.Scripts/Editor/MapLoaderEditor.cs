using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapLoader))]
public class MapLoaderEditor : Editor
{
    private static GameObject previewObject;
    private static MapData previewMapData;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapLoader loader = (MapLoader)target;
        SerializedProperty mapProp = serializedObject.FindProperty("currentMap");
        MapData mapData = mapProp.objectReferenceValue as MapData;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("배경 미리보기", EditorStyles.boldLabel);

        if (mapData == null)
        {
            EditorGUILayout.HelpBox("MapData를 먼저 할당하세요.", MessageType.Info);
            RemovePreview();
            return;
        }

        if (mapData.backgroundPrefab == null)
        {
            EditorGUILayout.HelpBox("MapData에 Background Prefab이 없습니다.", MessageType.Warning);
            RemovePreview();
            return;
        }

        if (previewObject == null || previewMapData != mapData)
            SpawnPreview(mapData);

        if (previewObject != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("배경 Transform", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            Vector3 pos = EditorGUILayout.Vector3Field("Position", mapData.backgroundPosition);
            Vector3 rot = EditorGUILayout.Vector3Field("Rotation", mapData.backgroundRotation);
            Vector3 scale = EditorGUILayout.Vector3Field("Scale", mapData.backgroundScale);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mapData, "Edit Background Transform");
                mapData.backgroundPosition = pos;
                mapData.backgroundRotation = rot;
                mapData.backgroundScale = scale;
                EditorUtility.SetDirty(mapData);

                previewObject.transform.position = pos;
                previewObject.transform.eulerAngles = rot;
                previewObject.transform.localScale = scale;
            }

            // Scene에서 직접 이동한 경우 SO에 반영
            SyncPreviewToSO(mapData);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("카메라 이동", GUILayout.Height(25)))
            {
                Selection.activeGameObject = previewObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            if (GUILayout.Button("위치 초기화", GUILayout.Height(25)))
            {
                Undo.RecordObject(mapData, "Reset Background Transform");
                mapData.backgroundPosition = Vector3.zero;
                mapData.backgroundRotation = Vector3.zero;
                mapData.backgroundScale = Vector3.one;
                EditorUtility.SetDirty(mapData);

                previewObject.transform.position = Vector3.zero;
                previewObject.transform.rotation = Quaternion.identity;
                previewObject.transform.localScale = Vector3.one;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                "Inspector에서 수치 조정 또는 Scene에서 직접 이동 모두 가능합니다.\n변경사항은 SO에 자동 저장됩니다.",
                MessageType.Info);
        }
    }

    private void SpawnPreview(MapData mapData)
    {
        RemovePreview();
        previewObject = (GameObject)PrefabUtility.InstantiatePrefab(mapData.backgroundPrefab);
        previewObject.name = "[BG Preview] " + mapData.backgroundPrefab.name;
        previewObject.transform.position = mapData.backgroundPosition;
        previewObject.transform.eulerAngles = mapData.backgroundRotation;
        previewObject.transform.localScale = mapData.backgroundScale;
        previewObject.hideFlags = HideFlags.DontSave;
        previewMapData = mapData;
    }

    private void SyncPreviewToSO(MapData mapData)
    {
        if (previewObject == null || mapData == null) return;

        bool changed = false;
        if (previewObject.transform.position != mapData.backgroundPosition) changed = true;
        if (previewObject.transform.eulerAngles != mapData.backgroundRotation) changed = true;
        if (previewObject.transform.localScale != mapData.backgroundScale) changed = true;

        if (changed)
        {
            Undo.RecordObject(mapData, "Sync Background Transform");
            mapData.backgroundPosition = previewObject.transform.position;
            mapData.backgroundRotation = previewObject.transform.eulerAngles;
            mapData.backgroundScale = previewObject.transform.localScale;
            EditorUtility.SetDirty(mapData);
            Repaint();
        }
    }

    private static void RemovePreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
        previewMapData = null;
    }

    private void OnDisable()
    {
        RemovePreview();
    }

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeCallback()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            RemovePreview();
    }
}
