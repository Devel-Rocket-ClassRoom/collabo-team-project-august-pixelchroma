using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Video;

public class MainScenario : EditorWindow
{
    private DialogueDataSO currentSO;
    private Vector2 scrollpos;
    private int selectedGroupindex = 0;
    private GUIStyle entryFoldoutStyle;
    //public Image CharacterImage1;
    //public Image CharacterImage2;

    private GUIStyle EntryFoldoutStyle // 대사 줄 접힘 헤더 스타일 (도메인 리로드 시 null이 되므로 매번 확인)
    {
        get
        {
            if (entryFoldoutStyle == null)
            {
                entryFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 22
                };
                entryFoldoutStyle.padding = new RectOffset(16, 4, 3, 3);
            }
            return entryFoldoutStyle;
        }
    }

    [MenuItem("MasterTools/Scenario Editor")]
    public static void ShowWindow()
    {
        GetWindow<MainScenario>("시나리오 에디터");
    }

    void OnGUI()
    {
        GUILayout.Label("시나리오 편집 모드", EditorStyles.boldLabel);
        currentSO = (DialogueDataSO)EditorGUILayout.ObjectField("편집할 파일", currentSO, typeof(DialogueDataSO), false);

        if (currentSO == null)
        {
            if (GUILayout.Button("새 시나리오 파일 생성")) CreateNewSO();
            return;
        }

        EditorGUILayout.Space(5);


        GUILayout.BeginVertical("box");
        {
            GUILayout.Label(" 그룹 관리", EditorStyles.miniBoldLabel);

            if (currentSO.groups == null || currentSO.groups.Count == 0)
            {
                if (GUILayout.Button("+ 첫 번째 그룹 생성"))
                {
                    currentSO.groups = new List<DialogueGroup> { new DialogueGroup { GroupName = "기본 그룹" } };
                }


                GUILayout.EndVertical();
                return;
            }

            string[] groupNames = new string[currentSO.groups.Count];
            for (int i = 0; i < currentSO.groups.Count; i++)
                groupNames[i] = string.IsNullOrEmpty(currentSO.groups[i].GroupName) ? $"그룹 {i}" : currentSO.groups[i].GroupName;

            if (selectedGroupindex >= currentSO.groups.Count) selectedGroupindex = 0;
            selectedGroupindex = GUILayout.Toolbar(selectedGroupindex, groupNames);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("그룹 추가"))
            {
                Undo.RecordObject(currentSO, "그룹 추가");
                currentSO.groups.Add(new DialogueGroup { GroupName = "새 그룹" });
                EditorUtility.SetDirty(currentSO);
            }

            GUI.enabled = selectedGroupindex > 0; // 0값보다 작으면 왼쪽으로감 
            if (GUILayout.Button("◀", GUILayout.Width(30)))
            {
                MoveGroup(selectedGroupindex, selectedGroupindex - 1);
            }
            GUI.enabled = true;

            GUI.enabled = selectedGroupindex < currentSO.groups.Count - 1;// 0이상 이면 오른쪽 이동 오른쪽에 그룹없으면 버튼 비활성화
            if (GUILayout.Button("▶", GUILayout.Width(30)))
            {
                MoveGroup(selectedGroupindex, selectedGroupindex + 1);
            }
            GUI.enabled = true;

            if (GUILayout.Button("현재 그룹 삭제") && currentSO.groups.Count > 1)
            {




                if (EditorUtility.DisplayDialog("시나리오 그룹 삭제 경고",
                   $"정말로 '{currentSO.groups[selectedGroupindex].GroupName}' 그룹을 삭제하시겠습니까?", "삭제", "취소"))
                {

                    Undo.RecordObject(currentSO, "그룹 삭제");

                    currentSO.groups.RemoveAt(selectedGroupindex);


                    selectedGroupindex = Mathf.Clamp(selectedGroupindex - 1, 0, currentSO.groups.Count - 1);


                    EditorUtility.SetDirty(currentSO);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        SerializedObject serializedObject = new SerializedObject(currentSO);
        serializedObject.Update();

        SerializedProperty groupsProp = serializedObject.FindProperty("groups");
        SerializedProperty currentGroupProp = groupsProp.GetArrayElementAtIndex(selectedGroupindex);
        EditorGUILayout.PropertyField(currentGroupProp.FindPropertyRelative("GroupName"), new GUIContent("현재 그룹 이름"));


        scrollpos = EditorGUILayout.BeginScrollView(scrollpos);
        {
            SerializedProperty entriesProperty = currentGroupProp.FindPropertyRelative("entries");
            if (entriesProperty != null)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    entriesProperty.isExpanded = EditorGUILayout.Foldout(entriesProperty.isExpanded, "전체 대사 리스트 (Entries)", true);
                    GUILayout.FlexibleSpace();

                    int currentSize = entriesProperty.arraySize;
                    EditorGUILayout.LabelField("Size", GUILayout.Width(35));
                    int newSize = EditorGUILayout.IntField(currentSize, GUILayout.Width(50));

                    if (GUILayout.Button("+", GUILayout.Width(25))) newSize++;
                    if (GUILayout.Button("-", GUILayout.Width(25)) && newSize > 0) newSize--;

                    if (newSize != currentSize) entriesProperty.arraySize = newSize;
                }
                EditorGUILayout.EndHorizontal();

                if (entriesProperty.isExpanded)
                {
                    EditorGUILayout.Space(5);
                    EditorGUI.indentLevel++;

                    int moveFrom = -1; // 루프 도중 배열을 건드리면 안되므로 예약만 해둠
                    int moveTo = -1;

                    for (int i = 0; i < entriesProperty.arraySize; i++)
                    {
                        SerializedProperty element = entriesProperty.GetArrayElementAtIndex(i);
                        SerializedProperty idProp = element.FindPropertyRelative("id");
                        SerializedProperty nameProp = element.FindPropertyRelative("speakerName");

                        int displayID = (idProp != null) ? idProp.intValue : i;
                        string sName = (nameProp != null) ? nameProp.stringValue : "";
                        string label = $"[ID: {displayID}] " + (string.IsNullOrEmpty(sName) ? "이름 없음" : sName);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox); // 대사 한 줄을 네모 박스로 감쌈

                        EditorGUILayout.BeginHorizontal();
                        {
                            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, label, true, EntryFoldoutStyle);
                            GUILayout.FlexibleSpace();

                            GUI.enabled = i > 0; // 맨 위면 올릴 곳이 없음
                            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(26)))
                            {
                                moveFrom = i;
                                moveTo = i - 1;
                            }

                            GUI.enabled = i < entriesProperty.arraySize - 1; // 맨 아래면 내릴 곳이 없음
                            if (GUILayout.Button("▼", EditorStyles.miniButtonRight, GUILayout.Width(26)))
                            {
                                moveFrom = i;
                                moveTo = i + 1;
                            }
                            GUI.enabled = true;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (element.isExpanded)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(idProp, new GUIContent("고유 ID"));
                            EditorGUILayout.Space(2);
                            EditorGUILayout.PropertyField(nameProp, new GUIContent("화자 이름"));
                            EditorGUILayout.Space(2);
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("dialogueText"), new GUIContent("대사 내용"));


                            EditorGUILayout.PropertyField(element.FindPropertyRelative("showChatUI"), new GUIContent("채팅창 표시 여부: 체크하면 켜지고 해제하면 꺼집니다"));              

                            EditorGUILayout.Space(5);

                          
                            SerializedProperty nextIndexProp = element.FindPropertyRelative("nextIndexOverride");
                            if (nextIndexProp != null)
                            {
                                EditorGUILayout.PropertyField(nextIndexProp, new GUIContent("강제 이동 ID"));
                            }

                            EditorGUILayout.Space(5);
                            EditorGUILayout.LabelField("🎥 영상 연출 설정", EditorStyles.boldLabel);

                            // 1. 비디오 클립 할당 칸
                            SerializedProperty videoClipProp = element.FindPropertyRelative("effectVideoClip");
                            if (videoClipProp != null)
                            {
                                EditorGUILayout.BeginVertical("helpbox"); // 보기 좋게 박스로 감쌉니다.
                                EditorGUILayout.PropertyField(videoClipProp, new GUIContent("연출용 비디오 클립 (.mp4)", "이 대사에서 재생할 영상 파일을 넣어주세요."));

                                // 팁 제공 (마스타의 편의를 위해!)
                                if (videoClipProp.objectReferenceValue == null)
                                {
                                    EditorGUILayout.HelpBox("영상이 없으면 일반 대사로 진행됩니다.", MessageType.None);
                                }
                                EditorGUILayout.EndVertical();
                            }

                            EditorGUILayout.Space(2);// 효과음관련
                            SerializedProperty SoundFolderProp = element.FindPropertyRelative("EffectSound");
                            SoundFolderProp.isExpanded = EditorGUILayout.Foldout(SoundFolderProp.isExpanded, " 사운드설정(효과음)", true);
                            if (SoundFolderProp.isExpanded)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.Space(5);
                                EditorGUILayout.LabelField("사운드 연출", EditorStyles.boldLabel);
                                EditorGUILayout.PropertyField(element.FindPropertyRelative("EffectSound"), new GUIContent("효과음(SE)"));
                                EditorGUILayout.PropertyField(element.FindPropertyRelative("seVolune"), new GUIContent("SE 볼륨"));
                                EditorGUI.indentLevel--;
                            }
                            EditorGUILayout.Space(2);
                            // 캐릭터 설정칸
                            SerializedProperty char1Prop = element.FindPropertyRelative("Char1");
                            SerializedProperty char2Prop = element.FindPropertyRelative("Char2");
                            char1Prop.isExpanded = EditorGUILayout.Foldout(char1Prop.isExpanded, " 전체 캐릭터 및 이미지 설정", true);
                            if (char1Prop.isExpanded)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.Space(5);
                                SerializedProperty char1FolderKey = char1Prop.FindPropertyRelative("CharacterPNG");
                                char1FolderKey.isExpanded = EditorGUILayout.Foldout(char1FolderKey.isExpanded, " 메인 캐릭터 (Char 1)", true);
                                if (char1FolderKey.isExpanded)
                                {
                                    EditorGUI.indentLevel++;
                                    DrawCharacterData(char1Prop);
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUILayout.Space(2);
                                SerializedProperty char2FolderKey = char2Prop.FindPropertyRelative("CharacterPNG");
                                char2FolderKey.isExpanded = EditorGUILayout.Foldout(char2FolderKey.isExpanded, " 서브 캐릭터 (Char 2)", true);
                                if (char2FolderKey.isExpanded)
                                {
                                    EditorGUI.indentLevel++;
                                    DrawCharacterData(char2Prop);
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUI.indentLevel--;
                            }

                            EditorGUILayout.Space(2);
                            SerializedProperty Backgroundimage = element.FindPropertyRelative("characterIllust");
                            Backgroundimage.isExpanded = EditorGUILayout.Foldout(Backgroundimage.isExpanded, "1장 일러스트 설정칸", true);
                            {
                                if (Backgroundimage.isExpanded)
                                {
                                    EditorGUILayout.Space(5);
                                    EditorGUI.indentLevel++;
                                    EditorGUILayout.PropertyField(element.FindPropertyRelative("characterIllust"), new GUIContent("캐릭터 통 일러스트"));
                                    EditorGUILayout.PropertyField(element.FindPropertyRelative("BackGroundSprit"), new GUIContent("배경 이미지"));

                                    EditorGUI.indentLevel--;
                                }
                            }
                          

                          
                            EditorGUILayout.Space(2);

                            SerializedProperty selectFolderProp = element.FindPropertyRelative("choices");
                            selectFolderProp.isExpanded = EditorGUILayout.Foldout(selectFolderProp.isExpanded, " 선택지 설정", true);

                            if (selectFolderProp.isExpanded)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.Space(5);
                                EditorGUILayout.PropertyField(selectFolderProp, new GUIContent("분기점 선택지"), true);
                                EditorGUI.indentLevel--;
                            }

                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(3); // 박스끼리 붙지 않게 간격
                    }

                    if (moveFrom >= 0) // 그리기가 끝난 뒤에 순서 교체 (id 값은 그대로 따라감)
                    {
                        entriesProperty.MoveArrayElement(moveFrom, moveTo);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(currentSO);
                        Repaint();
                    }

                    EditorGUI.indentLevel--;
                }

            }
        }


        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("저장(Force Save)", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(currentSO);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=cyan>시나리오 데이터 저장 완료!</color>");
        }
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(currentSO);



    } // 시나리오 에디터 UI그리는 칸

    void DrawCharacterData(SerializedProperty CharProp) // 캐릭터 설정값을 밖으로 빼고 함수로 변경
    {
        EditorGUILayout.PropertyField(CharProp.FindPropertyRelative("CharacterPNG"), new GUIContent("캐릭터 전용 PNG"));
        EditorGUILayout.PropertyField(CharProp.FindPropertyRelative("CharacterPos"), new GUIContent("위치 (X, Y)"));
        EditorGUILayout.PropertyField(CharProp.FindPropertyRelative("CharacterScale"), new GUIContent("캐릭터 크기값"));
        EditorGUILayout.PropertyField(CharProp.FindPropertyRelative("CharacterRotation"), new GUIContent("회전 (Z축)"));
        EditorGUILayout.PropertyField(CharProp.FindPropertyRelative("moveDuration"), new GUIContent("이동 시간(초)"));

    }

    private void MoveGroup(int OldIndex, int NewIndex) //그룹 순서 변경칸을위한 메서드
    {
        Undo.RecordObject(currentSO,"그룹 순서 변경");
        DialogueGroup Item = currentSO.groups[OldIndex];
        currentSO.groups.RemoveAt(OldIndex);
        currentSO.groups.Insert(NewIndex, Item);
        selectedGroupindex = NewIndex;

        EditorUtility.SetDirty(currentSO);
           
    }


    private void CreateNewSO()
    {
        DialogueDataSO asset = ScriptableObject.CreateInstance<DialogueDataSO>();
        string folderPath = "Assets/5.SOdata/Scenario";
        string fileName = "NewScenario.asset";
        string fullPath = folderPath + "/" + fileName;

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            fullPath = "Assets/" + fileName;
        }

        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();
        currentSO = asset;

        Selection.activeObject = asset;
        Debug.Log($"<color=green>새 시나리오 생성 완료: {fullPath}</color>");
    }

}

