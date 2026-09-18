using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 플레이 튜토리얼 구성 도구.
/// - 세계관 대사 / 전투 가이드 데이터 / 가이드 UI 프리팹 생성 (이미 있으면 건드리지 않음)
/// - Addressables 그룹 "Tutorial"에 씬·데이터·프리팹 등록
/// - 1.2 / 1.3 튜토리얼 씬에 진행 스크립트 연결
/// - 전투 HUD에 위협 범위 토글 버튼 추가
/// </summary>
public static class TutorialSetup
{
    private const string StoryScenePath = "Assets/1.Sence/1.2.Tutorial_Storymode.unity";
    private const string BattleScenePath = "Assets/1.Sence/1.3.Tutorial Game.unity";
    private const string DataFolder = "Assets/5.SOdata/Tutorial";
    private const string WorldStoryPath = DataFolder + "/Tutorial_World.asset";
    private const string GuideDataPath = DataFolder + "/Tutorial_BattleGuide.asset";
    private const string PrefabFolder = "Assets/3.Prefabs/1.3.Tutorial";
    private const string GuidePrefabPath = PrefabFolder + "/TutorialGuide (PlayHere).prefab";
    private const string HudPrefabPath = "Assets/3.Prefabs/4.MainGame/BattleHUDUI (Here) (PlayHere).prefab";
    private const string FontPath = "Assets/6.Font/경기천년제목_Medium SDF.asset";
    private const string GroupName = "Tutorial";

    [MenuItem("Tools/POLTERGEIST/Tutorial/튜토리얼 구성 (데이터 · 프리팹 · Addressables · 씬)")]
    public static void SetupAll()
    {
        EnsureFolder(DataFolder);
        EnsureFolder(PrefabFolder);

        DialogueDataSO world = CreateWorldStoryIfMissing();
        TutorialGuideData guide = CreateGuideDataIfMissing();
        GameObject guidePrefab = CreateGuidePrefabIfMissing();
        AddThreatToggleToHud();
        RegisterAddressables(world, guide, guidePrefab);
        HookScenes();

        AssetDatabase.SaveAssets();
        Debug.Log("<color=lime>[Tutorial] 튜토리얼 구성 완료</color>");
    }

    [MenuItem("Tools/POLTERGEIST/Tutorial/튜토리얼 완료 기록 삭제 (다시 보기)")]
    public static void ResetProgress()
    {
        TutorialProgress.Reset();
        Debug.Log($"[Tutorial] 완료 기록을 지웠습니다: {TutorialProgress.FilePath}");
    }

    [MenuItem("Tools/POLTERGEIST/Tutorial/완료 기록 폴더 열기")]
    public static void RevealProgress()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    // ───────────────────── 데이터 ─────────────────────

    private static DialogueDataSO CreateWorldStoryIfMissing()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DialogueDataSO>(WorldStoryPath);
        if (existing != null) return existing;

        Sprite background = FindStoryBackground();
        var so = ScriptableObject.CreateInstance<DialogueDataSO>();
        int id = 0;

        void Group(string name, params (string speaker, string text)[] lines)
        {
            var group = new DialogueGroup { id = so.groups.Count, GroupName = name };
            foreach (var (speaker, text) in lines)
            {
                group.entries.Add(new DialogueEntry
                {
                    id = id++,
                    speakerName = speaker,
                    dialogueText = text,
                    BackGroundSprit = background,
                    showChatUI = true
                });
            }
            so.groups.Add(group);
        }

        const string Yuki = "키타노 유키";
        Group("첫 출근",
            ("나", "국무총리 명의의 임명장. 오늘부터 내 직함은 '이상관리국 국장'이다."),
            (Yuki, "국장님, 이상관리국 AMA 서기 겸 비서 키타노 유키입니다. 오늘부터 국장님의 업무를 보좌합니다."),
            (Yuki, "부임 첫날이니 기본 브리핑부터 드리겠습니다. 메모는 필요 없습니다. 필요한 건 제가 전부 기억해 두니까요."));

        Group("세계관 브리핑",
            (Yuki, "약 50년 전부터, 사람에게서 설명할 수 없는 능력이 발현되기 시작했습니다. 학술명은 '이상현상자'. 세간에서는 '폴터가이스트'라고 부르죠."),
            (Yuki, "현재 대한민국 인구의 약 20%가 능력자입니다. 발현은 평균 15세 무렵이고, 18세 이후에 발현된 사례는 아직 없습니다."),
            (Yuki, "능력자는 능력자분류국에 등록할 의무가 있습니다. 등록하지 않으면 카드 발급도, 휴대폰 개통도 어렵습니다."),
            (Yuki, "그리고 능력이 얽힌 사건을 조사하고, 능력자를 보호하고, 현장에 대응하는 기관. 그게 저희 이상관리국, AMA입니다."),
            (Yuki, "대통령 직속 CSIA와는 협업 관계입니다. …표면상으로는요. 그쪽 사람들은 조심하시는 편이 좋습니다."));

        Group("S.A.F.E.",
            ("나", "그래서, 현장에는 누가 나가지?"),
            (Yuki, "J사립고 SAFE반. 국장님이 보충수업 교사로 담당하시게 될, 능력자 학생 여섯 명입니다."),
            (Yuki, "국장님은 비능력자시니 직접 싸우실 필요는 없습니다. 판단하고, 배치하고, 지휘하는 것. 그게 국장님의 일입니다."),
            (Yuki, "마침 모의 전술 훈련이 준비되어 있습니다. 실전에 나가기 전에 지휘 방식부터 익혀 두시죠."));

        AssetDatabase.CreateAsset(so, WorldStoryPath);
        return so;
    }

    private static Sprite FindStoryBackground()
    {
        var chapter = AssetDatabase.LoadAssetAtPath<DialogueDataSO>("Assets/5.SOdata/Scenario/Story_Chapter01.asset");
        if (chapter == null) return null;
        foreach (var group in chapter.groups)
            foreach (var entry in group.entries)
                if (entry.BackGroundSprit != null) return entry.BackGroundSprit;
        return null;
    }

    private static TutorialGuideData CreateGuideDataIfMissing()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TutorialGuideData>(GuideDataPath);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<TutorialGuideData>();
        void Step(TutorialStepGoal goal, string text, string objective = "")
            => data.steps.Add(new TutorialStep { goal = goal, text = text, objective = objective });

        Step(TutorialStepGoal.PressNext,
            "여기는 AMA 모의 전술 훈련장입니다. 지휘는 전부 손가락 하나로 할 수 있게 되어 있습니다.");
        Step(TutorialStepGoal.StartBattle,
            "먼저 요원을 배치합니다. 아래 목록에서 요원을 고르고, 파랗게 빛나는 배치 구역을 눌러 세우세요. 다 세웠으면 '작전 개시'를 누르시면 됩니다.",
            "요원을 배치하고 작전을 개시하세요");
        Step(TutorialStepGoal.SelectUnit,
            "작전 시작입니다. 아군 요원을 눌러 보세요. 파란 칸은 이동할 수 있는 곳, 빨간 칸은 공격이 닿는 곳입니다.",
            "아군 요원을 선택하세요");
        Step(TutorialStepGoal.MoveUnit,
            "파란 칸 중 한 곳을 눌러 이동하세요. 잘못 움직였다면 행동을 확정하기 전까지 '되돌리기'로 취소할 수 있습니다.",
            "파란 칸을 눌러 이동하세요");
        Step(TutorialStepGoal.Attack,
            "이제 공격입니다. 빨간 칸 안의 적을 누르면 예상 피해, 명중률, 반격 여부가 먼저 표시됩니다. 확인하고 공격하세요.",
            "적을 눌러 예측을 확인하고 공격하세요");
        Step(TutorialStepGoal.PressNext,
            "공격받은 적이 살아남고 사거리가 닿으면 반격합니다. 공격하기 전에 예측 창에서 반격 여부를 꼭 확인하세요. 적에게 공격받은 요원도 똑같이 반격합니다.");
        Step(TutorialStepGoal.ToggleThreatRange,
            "마지막으로 가장 중요한 것. 왼쪽 위 '위협 범위' 버튼을 눌러 보세요. 적이 다음 턴에 공격할 수 있는 칸이 붉게 표시됩니다.",
            "위협 범위 버튼을 누르세요");
        Step(TutorialStepGoal.PressNext,
            "붉은 칸이 짙을수록 여러 적이 노리는 자리입니다. 요원을 붉은 칸 밖이나 엄폐물 뒤에 세우는 게 기본입니다.");
        Step(TutorialStepGoal.WinBattle,
            "행동을 마친 요원은 회색이 되고, 모두 행동하면 적의 턴이 됩니다. 훈련 목표는 적 전멸. 실전처럼 지휘해 보시죠.",
            "적을 모두 쓰러뜨리세요");

        data.victoryText = "훈련 종료. 첫 지휘치고는 나쁘지 않았습니다, 국장님. 실전도 이 감각으로 부탁드립니다.";
        data.defeatText = "괜찮습니다. 모의 훈련이니까요. 위협 범위를 확인하면서 다시 해 보시죠.";

        AssetDatabase.CreateAsset(data, GuideDataPath);
        return data;
    }

    // ───────────────────── 가이드 UI 프리팹 ─────────────────────

    private static GameObject CreateGuidePrefabIfMissing()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GuidePrefabPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var root = new GameObject("TutorialGuide (PlayHere)", typeof(RectTransform)) { layer = 5 };
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 110;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2220);
        scaler.matchWidthOrHeight = 0f;
        root.AddComponent<GraphicRaycaster>();
        var view = root.AddComponent<TutorialGuideView>();

        var safe = Rect("SafeArea", root.transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaAdapter>();

        // 대사창
        var box = Rect("Dialogue Box", safe, new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.81f));
        var boxBg = box.gameObject.AddComponent<ChamferGraphic>();
        boxBg.color = UITheme.WithAlpha(UITheme.Charcoal, 0.94f);
        boxBg.SetCuts(28, 0, 28, 0);
        var accent = Rect("Accent Line", box, new Vector2(0, 0), new Vector2(0, 1));
        accent.pivot = new Vector2(0, 0.5f);
        accent.sizeDelta = new Vector2(8, 0);
        Graphic(accent, UITheme.Accent);

        var portraitRt = Rect("Portrait", box, new Vector2(0.02f, 0.06f), new Vector2(0.23f, 0.94f));
        var portrait = portraitRt.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portraitRt.gameObject.SetActive(false);

        var textArea = Rect("Text Area", box, new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.94f));
        var speaker = Text("Speaker", textArea, new Vector2(0, 0.74f), new Vector2(1, 1), "키타노 유키", 38, UITheme.Accent, font);
        speaker.fontStyle = FontStyles.Bold;
        var body = Text("Body", textArea, new Vector2(0, 0), new Vector2(1, 0.72f), "안내 대사", 34, UITheme.White, font);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true;
        body.fontSizeMin = 24;
        body.fontSizeMax = 34;

        var next = Rect("Next Button", box, new Vector2(1, 0), new Vector2(1, 0));
        next.pivot = new Vector2(1, 0);
        next.sizeDelta = new Vector2(200, 72);
        next.anchoredPosition = new Vector2(-24, 18);
        var nextBg = next.gameObject.AddComponent<ChamferGraphic>();
        nextBg.color = UITheme.Accent;
        nextBg.SetCuts(14, 0, 14, 0);
        var nextButton = next.gameObject.AddComponent<Button>();
        nextButton.targetGraphic = nextBg;
        next.gameObject.AddComponent<UIPressFeedback>();
        var nextLabel = Text("Label", next, Vector2.zero, Vector2.one, "다음", 34, UITheme.Charcoal, font);
        nextLabel.alignment = TextAlignmentOptions.Center;
        nextLabel.fontStyle = FontStyles.Bold;

        // 목표 표시
        var chip = Rect("Objective Chip", safe, new Vector2(0.04f, 0.595f), new Vector2(0.96f, 0.632f));
        var chipBg = chip.gameObject.AddComponent<ChamferGraphic>();
        chipBg.color = UITheme.WithAlpha(UITheme.Accent, 0.18f);
        chipBg.BorderWidth = 3;
        chipBg.raycastTarget = false;
        var objective = Text("Objective", chip, Vector2.zero, Vector2.one, "목표", 30, UITheme.White, font);
        objective.alignment = TextAlignmentOptions.Center;
        objective.margin = new Vector4(20, 0, 20, 0);

        // 건너뛰기
        var skip = Rect("Skip Button", safe, new Vector2(1, 1), new Vector2(1, 1));
        skip.pivot = new Vector2(1, 1);
        skip.sizeDelta = new Vector2(300, 72);
        skip.anchoredPosition = new Vector2(-32, -40);
        var skipBg = skip.gameObject.AddComponent<ChamferGraphic>();
        skipBg.color = UITheme.WithAlpha(UITheme.Charcoal, 0.85f);
        skipBg.SetCuts(14, 0, 14, 0);
        var skipButton = skip.gameObject.AddComponent<Button>();
        skipButton.targetGraphic = skipBg;
        skip.gameObject.AddComponent<UIPressFeedback>();
        var skipLabel = Text("Label", skip, Vector2.zero, Vector2.one, "튜토리얼 건너뛰기", 28, UITheme.LightGray, font);
        skipLabel.alignment = TextAlignmentOptions.Center;

        var so = new SerializedObject(view);
        so.FindProperty("dialogueBox").objectReferenceValue = box.gameObject;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("textArea").objectReferenceValue = textArea;
        so.FindProperty("speakerText").objectReferenceValue = speaker;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("nextButton").objectReferenceValue = nextButton;
        so.FindProperty("nextButtonText").objectReferenceValue = nextLabel;
        so.FindProperty("objectiveChip").objectReferenceValue = chip.gameObject;
        so.FindProperty("objectiveText").objectReferenceValue = objective;
        so.FindProperty("skipButton").objectReferenceValue = skipButton;
        so.FindProperty("skipButtonText").objectReferenceValue = skipLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GuidePrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ───────────────────── HUD 위협 범위 버튼 ─────────────────────

    private static void AddThreatToggleToHud()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        Transform safe = hud.transform.Find("SafeArea");
        Transform speed = safe != null ? safe.Find("배속 버튼") : null;
        if (safe == null || speed == null || safe.Find("위협 범위 버튼") != null)
        {
            PrefabUtility.UnloadPrefabContents(hud);
            return;
        }

        GameObject toggle = Object.Instantiate(speed.gameObject, safe);
        toggle.name = "위협 범위 버튼";
        toggle.transform.SetSiblingIndex(speed.GetSiblingIndex() + 1);
        Object.DestroyImmediate(toggle.GetComponent<GameSpeedToggle>());
        Object.DestroyImmediate(toggle.GetComponent<Button>());
        var button = toggle.AddComponent<Button>();
        button.targetGraphic = toggle.GetComponent<Graphic>();

        var rt = (RectTransform)toggle.transform;
        rt.sizeDelta = new Vector2(260, 72);
        rt.anchoredPosition = new Vector2(32, -124);

        var threat = toggle.AddComponent<ThreatRangeToggleButton>();
        var so = new SerializedObject(threat);
        so.FindProperty("label").objectReferenceValue = toggle.transform.Find("Label").GetComponent<TMP_Text>();
        so.FindProperty("highlight").objectReferenceValue = toggle.transform.Find("Border").GetComponent<Graphic>();
        so.ApplyModifiedPropertiesWithoutUndo();
        toggle.transform.Find("Label").GetComponent<TMP_Text>().text = "위협 범위 ON";

        PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        PrefabUtility.UnloadPrefabContents(hud);
    }

    // ───────────────────── Addressables ─────────────────────

    private static void RegisterAddressables(DialogueDataSO world, TutorialGuideData guide, GameObject guidePrefab)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        // 플레이어 빌드 때 Addressables 콘텐츠도 같이 빌드합니다. (APK에 튜토리얼이 빠지지 않도록)
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            group = settings.CreateGroup(GroupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        var entries = new List<(string path, string address)>
        {
            (StoryScenePath, TutorialFlow.StorySceneKey),
            (BattleScenePath, TutorialFlow.BattleSceneKey),
            (AssetDatabase.GetAssetPath(world), TutorialFlow.WorldStoryKey),
            (AssetDatabase.GetAssetPath(guide), TutorialFlow.BattleGuideKey),
            (AssetDatabase.GetAssetPath(guidePrefab), TutorialFlow.GuideViewKey),
        };

        foreach (var (path, address) in entries)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[Tutorial] Addressables 등록 실패: {path} 가 없습니다.");
                continue;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(GroupName, true, true, false);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
    }

    // ───────────────────── 씬 연결 ─────────────────────

    private static void HookScenes()
    {
        string activePath = EditorSceneManager.GetActiveScene().path;

        HookScene(StoryScenePath, scene =>
        {
            var controller = Object.FindAnyObjectByType<ScenarioController>();
            if (controller == null) return false;
            if (controller.GetComponent<TutorialStoryDirector>() != null) return false;
            controller.gameObject.AddComponent<TutorialStoryDirector>();
            return true;
        });

        HookScene(BattleScenePath, scene =>
        {
            if (Object.FindAnyObjectByType<TutorialBattleDirector>() != null) return false;
            var director = new GameObject("TutorialDirector (Here)");
            SceneManager_Move(director, scene);
            director.AddComponent<TutorialBattleDirector>();
            return true;
        });

        if (!string.IsNullOrEmpty(activePath) && EditorSceneManager.GetActiveScene().path != activePath)
            EditorSceneManager.OpenScene(activePath);
    }

    private static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene)
        => UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

    private static void HookScene(string path, System.Func<UnityEngine.SceneManagement.Scene, bool> modify)
    {
        var scene = EditorSceneManager.GetSceneByPath(path);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        var previousActive = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SetActiveScene(scene);
        bool changed = modify(scene);
        if (previousActive.IsValid() && previousActive != scene) EditorSceneManager.SetActiveScene(previousActive);

        if (changed) EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
    }

    // ───────────────────── 도우미 ─────────────────────

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static void Graphic(RectTransform rt, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        string text, float size, Color color, TMP_FontAsset font)
    {
        var rt = Rect(name, parent, anchorMin, anchorMax);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        return tmp;
    }
}
