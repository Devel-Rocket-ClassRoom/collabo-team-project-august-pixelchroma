using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(MapLoader))]
public class MapLoaderEditor : Editor
{
    private static GameObject previewObject;
    private static MapData previewMapData;
    private static Hash128 lastPrefabHash;

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

        string prefabPath = AssetDatabase.GetAssetPath(mapData.backgroundPrefab);
        Hash128 currentHash = AssetDatabase.GetAssetDependencyHash(prefabPath);
        if (previewObject == null || previewMapData != mapData || currentHash != lastPrefabHash)
        {
            SpawnPreview(mapData);
            lastPrefabHash = currentHash;
        }

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

            // ── 툰 환경 설정 ──
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("툰 셰이딩 설정", EditorStyles.boldLabel);

            Shader toonShader = Shader.Find("Custom/ToonLit");
            if (toonShader == null)
            {
                EditorGUILayout.HelpBox("Custom/ToonLit 셰이더를 찾을 수 없습니다.\nAssets/Shaders/ToonShader.shader 확인하세요.", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("툰 환경 전체 적용 (셰이더 + 조명 + 안개)", GUILayout.Height(35)))
                {
                    ConvertMaterialsToToon(previewObject, toonShader);
                    EnsureEnvironmentSettings(previewObject);
                }

                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("셰이더만 적용", GUILayout.Height(25)))
                {
                    ConvertMaterialsToToon(previewObject, toonShader);
                }
                if (GUILayout.Button("URP Lit 복원", GUILayout.Height(25)))
                {
                    Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpLit != null)
                        RestoreMaterialsToURPLit(previewObject, urpLit);
                    RemoveEnvironmentSettings(previewObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            var existingSettings = previewObject.GetComponent<EnvironmentToonSettings>();
            if (existingSettings != null)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(
                    "EnvironmentToonSettings 적용됨. 프리뷰 오브젝트의 Inspector에서 값을 조정할 수 있습니다.",
                    MessageType.Info);
                if (GUILayout.Button("환경 설정 다시 적용", GUILayout.Height(22)))
                {
                    existingSettings.Apply();
                }
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                "Inspector에서 수치 조정 또는 Scene에서 직접 이동 모두 가능합니다.\n변경사항은 SO에 자동 저장됩니다.",
                MessageType.Info);
        }
    }

    private static void ConvertMaterialsToToon(GameObject obj, Shader toonShader)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        int converted = 0;

        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null || mat.shader == toonShader) continue;

                Texture baseMap = null;

                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    baseMap = mat.GetTexture("_BaseMap");
                else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                    baseMap = mat.GetTexture("_MainTex");

                mat.shader = toonShader;

                if (baseMap != null)
                    mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Flatten", 0.425f);
                mat.SetFloat("_ShadowThreshold", 0.68f);
                mat.SetFloat("_ShadowFeather", 0.10f);
                mat.SetColor("_ShadowTint", new Color(0.66f, 0.71f, 0.86f, 1f));
                mat.SetFloat("_ReceiveShadowStrength", 0f);
                mat.SetFloat("_AmbientStrength", 1.0f);
                mat.SetFloat("_AmbientFlatten", 0.6f);
                mat.SetFloat("_EnvironmentInfluence", 0.583f);
                mat.SetFloat("_Brightness", 1.0f);
                mat.SetFloat("_AdditionalLightIntensity", 0.5f);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MapLoader] {converted}개 머터리얼을 툰 셰이더로 변환 완료");
    }

    private static void EnsureEnvironmentSettings(GameObject obj)
    {
        var settings = obj.GetComponent<EnvironmentToonSettings>();
        if (settings == null)
            settings = obj.AddComponent<EnvironmentToonSettings>();
        settings.Apply();
        Debug.Log("[MapLoader] 환경 툰 설정 적용 완료 (조명 + 앰비언트 + 안개)");
    }

    private static void RemoveEnvironmentSettings(GameObject obj)
    {
        var settings = obj.GetComponent<EnvironmentToonSettings>();
        if (settings != null)
            DestroyImmediate(settings);
    }

    private static void RestoreMaterialsToURPLit(GameObject obj, Shader urpLit)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        int converted = 0;

        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat == null || mat.shader == urpLit) continue;

                Texture baseMap = null;
                Color baseColor = Color.white;

                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    baseMap = mat.GetTexture("_BaseMap");
                if (mat.HasProperty("_BaseColor"))
                    baseColor = mat.GetColor("_BaseColor");

                mat.shader = urpLit;

                if (baseMap != null)
                    mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", baseColor);

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MapLoader] {converted}개 머터리얼을 URP Lit로 복원 완료");
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

    [InitializeOnLoadMethod]
    private static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryAutoSpawn;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            RemovePreview();
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryAutoSpawn;
    }

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryAutoSpawn;
    }

    private static void TryAutoSpawn()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (previewObject != null) return;

        MapLoader loader = Object.FindFirstObjectByType<MapLoader>();
        if (loader == null) return;

        var so = new SerializedObject(loader);
        var mapProp = so.FindProperty("currentMap");
        MapData mapData = mapProp.objectReferenceValue as MapData;

        if (mapData == null || mapData.backgroundPrefab == null) return;

        previewObject = (GameObject)PrefabUtility.InstantiatePrefab(mapData.backgroundPrefab);
        previewObject.name = "[BG Preview] " + mapData.backgroundPrefab.name;
        previewObject.transform.position = mapData.backgroundPosition;
        previewObject.transform.eulerAngles = mapData.backgroundRotation;
        previewObject.transform.localScale = mapData.backgroundScale;
        previewObject.hideFlags = HideFlags.DontSave;
        previewMapData = mapData;

        string prefabPath = AssetDatabase.GetAssetPath(mapData.backgroundPrefab);
        lastPrefabHash = AssetDatabase.GetAssetDependencyHash(prefabPath);
    }
}
