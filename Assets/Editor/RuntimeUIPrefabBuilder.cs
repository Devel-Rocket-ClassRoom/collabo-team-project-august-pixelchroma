using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RuntimeUIPrefabBuilder
{
    private const string Folder = "Assets/3.Prefabs/UI";
    private const string DeploymentPath = Folder + "/DeploymentUI.prefab";
    private const string AttackPath = Folder + "/AttackPreviewUI.prefab";
    private const string SquadPath = Folder + "/SquadFormationUI.prefab";

    static RuntimeUIPrefabBuilder()
    {
        EditorApplication.delayCall += BuildMissingPrefabs;
    }

    [MenuItem("Tools/SRPG UI/누락된 UI Prefab 생성")]
    public static void BuildMissingPrefabs()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EnsureFolders();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DeploymentPath) == null)
            BuildDeploymentPrefab();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(AttackPath) == null)
            BuildAttackPrefab();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SquadPath) == null)
            BuildSquadPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/SRPG UI/기본 UI Prefab 다시 생성")]
    public static void RebuildAllPrefabs()
    {
        EnsureFolders();
        BuildDeploymentPrefab();
        BuildAttackPrefab();
        BuildSquadPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/3.Prefabs", "UI");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void BuildDeploymentPrefab()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/6.Font/경기천년제목_Medium SDF.asset");
        GameObject root = CreateCanvas("DeploymentUI", 40);
        DeploymentUIView view = root.AddComponent<DeploymentUIView>();

        Image panel = CreateImage(root.transform, "배치 패널", new Color(0.025f, 0.04f, 0.065f, 0.97f));
        SetAnchors(panel.rectTransform, 0.025f, 0.08f, 0.975f, 0.28f);

        TMP_Text info = CreateText(panel.transform, "배치 안내", "배치할 유닛을 선택하세요", 24f, font);
        SetAnchors(info.rectTransform, 0.025f, 0.68f, 0.975f, 0.96f);

        RectTransform row = CreateRect(panel.transform, "캐릭터 카드 영역");
        SetAnchors(row, 0.025f, 0.06f, 0.975f, 0.66f);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        string[] samples = { "유닛 1", "유닛 2", "유닛 3", "유닛 4" };
        Color[] colors =
        {
            new Color(0.95f, 0.3f, 0.12f), new Color(0.5f, 0.3f, 0.95f),
            new Color(0.1f, 0.82f, 0.9f), new Color(0.15f, 0.8f, 0.4f)
        };
        for (int i = 0; i < samples.Length; i++)
        {
            Button card = CreateButton(row, "카드 예시 " + (i + 1), colors[i]);
            TMP_Text label = CreateText(card.transform, "정보", samples[i] + "\n체력 4  공격 2\n이동 3  사거리 2", 20f, font);
            Stretch(label.rectTransform);
        }

        Button start = CreateButton(root.transform, "게임 시작 버튼", new Color(1f, 0.72f, 0.08f));
        SetAnchors((RectTransform)start.transform, 0.24f, 0.295f, 0.76f, 0.36f);
        TMP_Text startLabel = CreateText(start.transform, "문구", "게임 시작", 34f, font);
        startLabel.color = new Color(0.08f, 0.06f, 0.02f);
        Stretch(startLabel.rectTransform);

        Image hint = CreateImage(root.transform, "배치 취소 안내", new Color(0.02f, 0.035f, 0.06f, 0.96f));
        SetAnchors(hint.rectTransform, 0.08f, 0.292f, 0.92f, 0.347f);
        TMP_Text hintText = CreateText(hint.transform, "문구", "배치 취소하려면 배치된 캐릭터를 눌러주세요", 27f, font);
        hintText.color = new Color(1f, 0.82f, 0.18f);
        Stretch(hintText.rectTransform);
        hint.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("panel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("characterContainer").objectReferenceValue = row;
        serialized.FindProperty("infoText").objectReferenceValue = info;
        serialized.FindProperty("startButton").objectReferenceValue = start;
        serialized.FindProperty("cancelHint").objectReferenceValue = hint.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, DeploymentPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildAttackPrefab()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/6.Font/경기천년제목_Medium SDF.asset");
        GameObject root = CreateCanvas("AttackPreviewUI", 100);
        AttackPreviewUIView view = root.AddComponent<AttackPreviewUIView>();

        Image panel = CreateImage(root.transform, "공격 미리보기 패널", new Color(0.035f, 0.025f, 0.025f, 0.98f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(640f, 390f);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.35f, 0.12f);
        outline.effectDistance = new Vector2(7f, -7f);

        TMP_Text info = CreateText(panel.transform, "전투 정보", "예상 피해 2    체력 4 > 2\n이동 3    사거리 2", 36f, font);
        SetAnchors(info.rectTransform, 0f, 0.43f, 1f, 1f);

        Button cancel = CreateButton(panel.transform, "취소 버튼", new Color(0.24f, 0.27f, 0.32f));
        SetAnchors((RectTransform)cancel.transform, 0.035f, 0.045f, 0.485f, 0.39f);
        TMP_Text cancelText = CreateText(cancel.transform, "문구", "취소", 42f, font);
        Stretch(cancelText.rectTransform);

        Button confirm = CreateButton(panel.transform, "공격 버튼", new Color(0.85f, 0.16f, 0.08f));
        SetAnchors((RectTransform)confirm.transform, 0.515f, 0.045f, 0.965f, 0.39f);
        TMP_Text confirmText = CreateText(confirm.transform, "문구", "공격", 42f, font);
        Stretch(confirmText.rectTransform);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("panel").objectReferenceValue = panelRect;
        serialized.FindProperty("previewText").objectReferenceValue = info;
        serialized.FindProperty("cancelButton").objectReferenceValue = cancel;
        serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, AttackPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildSquadPrefab()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/6.Font/경기천년제목_Medium SDF.asset");
        GameObject root = CreateCanvas("SquadFormationUI", 0);
        SquadFormationUIView view = root.AddComponent<SquadFormationUIView>();

        Image background = CreateImage(root.transform, "배경", new Color(0.035f, 0.047f, 0.07f));
        Stretch(background.rectTransform);
        RectTransform safe = CreateRect(root.transform, "SafeArea");
        Stretch(safe);

        Image header = CreateImage(safe, "상단 헤더", new Color(0.045f, 0.06f, 0.09f));
        SetAnchors(header.rectTransform, 0f, 0.86f, 1f, 1f);
        Button back = CreateButton(header.transform, "뒤로가기 버튼", new Color(0.12f, 0.15f, 0.2f));
        SetAnchors((RectTransform)back.transform, 0.035f, 0.2f, 0.15f, 0.78f);
        TMP_Text backText = CreateText(back.transform, "문구", "<", 32f, font);
        Stretch(backText.rectTransform);
        TMP_Text title = CreateText(header.transform, "제목", "스쿼드 편성", 46f, font);
        title.alignment = TextAlignmentOptions.Left;
        SetAnchors(title.rectTransform, 0.18f, 0.48f, 0.96f, 0.9f);
        TMP_Text stage = CreateText(header.transform, "스테이지", "작전 01  /  도심 구역", 24f, font);
        stage.alignment = TextAlignmentOptions.Left;
        stage.color = new Color(0.05f, 0.78f, 1f);
        SetAnchors(stage.rectTransform, 0.185f, 0.15f, 0.78f, 0.48f);
        TMP_Text power = CreateText(header.transform, "전투력", "전투력 0000", 25f, font);
        power.alignment = TextAlignmentOptions.Right;
        power.color = new Color(1f, 0.78f, 0.08f);
        SetAnchors(power.rectTransform, 0.68f, 0.12f, 0.96f, 0.46f);

        Image squadSection = CreateImage(safe, "출전 슬롯 영역", new Color(0.075f, 0.094f, 0.13f, 0.98f));
        SetAnchors(squadSection.rectTransform, 0.035f, 0.56f, 0.965f, 0.845f);
        TMP_Text squadLabel = CreateText(squadSection.transform, "제목", "출전 슬롯", 25f, font);
        squadLabel.alignment = TextAlignmentOptions.Left;
        squadLabel.color = new Color(0.48f, 0.53f, 0.62f);
        SetAnchors(squadLabel.rectTransform, 0.035f, 0.84f, 0.7f, 0.98f);
        TMP_Text count = CreateText(squadSection.transform, "인원", "0 / 4", 25f, font);
        count.alignment = TextAlignmentOptions.Right;
        count.color = new Color(1f, 0.78f, 0.08f);
        SetAnchors(count.rectTransform, 0.72f, 0.84f, 0.965f, 0.98f);

        Button[] slots = new Button[4];
        TMP_Text[] slotNames = new TMP_Text[4];
        TMP_Text[] slotDetails = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            float left = 0.035f + i * 0.238f;
            slots[i] = CreateButton(squadSection.transform, $"출전 슬롯 {i + 1}", new Color(0.11f, 0.14f, 0.19f));
            SetAnchors((RectTransform)slots[i].transform, left, 0.08f, left + 0.21f, 0.79f);
            slotNames[i] = CreateText(slots[i].transform, "이름", "빈 슬롯", 25f, font);
            SetAnchors(slotNames[i].rectTransform, 0.04f, 0.5f, 0.96f, 0.92f);
            slotDetails[i] = CreateText(slots[i].transform, "정보", "+", 38f, font);
            slotDetails[i].color = new Color(0.48f, 0.53f, 0.62f);
            SetAnchors(slotDetails[i].rectTransform, 0.04f, 0.08f, 0.96f, 0.52f);
        }

        Image rosterSection = CreateImage(safe, "보유 유닛 영역", new Color(0.055f, 0.07f, 0.1f));
        SetAnchors(rosterSection.rectTransform, 0.035f, 0.17f, 0.965f, 0.54f);
        TMP_Text rosterLabel = CreateText(rosterSection.transform, "제목", "출전 가능 유닛", 27f, font);
        rosterLabel.alignment = TextAlignmentOptions.Left;
        SetAnchors(rosterLabel.rectTransform, 0.035f, 0.86f, 0.7f, 0.98f);

        string[] names = { "유탄병", "비전술사", "관통저격수", "선봉대", "전격술사", "야전의무병" };
        Color[] colors =
        {
            new Color(0.95f, 0.38f, 0.16f), new Color(0.55f, 0.3f, 1f),
            new Color(0.25f, 0.8f, 1f), new Color(0.2f, 0.85f, 0.45f),
            new Color(0.15f, 0.95f, 0.9f), new Color(1f, 0.35f, 0.7f)
        };
        Button[] cards = new Button[6];
        TMP_Text[] cardNames = new TMP_Text[6];
        TMP_Text[] cardStats = new TMP_Text[6];
        TMP_Text[] cardStatuses = new TMP_Text[6];
        TMP_Text[] cardMonograms = new TMP_Text[6];
        for (int i = 0; i < 6; i++)
        {
            int column = i % 3;
            int rowIndex = i / 3;
            float left = 0.035f + column * 0.322f;
            float top = 0.82f - rowIndex * 0.39f;
            cards[i] = CreateButton(rosterSection.transform, $"유닛 카드 {i + 1}", colors[i]);
            SetAnchors((RectTransform)cards[i].transform, left, top - 0.34f, left + 0.285f, top);
            Image portrait = CreateImage(cards[i].transform, "캐릭터 이미지", Color.Lerp(colors[i], Color.black, 0.35f));
            SetAnchors(portrait.rectTransform, 0.04f, 0.34f, 0.96f, 0.96f);
            cardMonograms[i] = CreateText(portrait.transform, "임시 이미지 문자", names[i].Substring(0, 2), 50f, font);
            Stretch(cardMonograms[i].rectTransform);
            cardNames[i] = CreateText(cards[i].transform, "이름", names[i], 22f, font);
            SetAnchors(cardNames[i].rectTransform, 0.03f, 0.17f, 0.97f, 0.36f);
            cardStats[i] = CreateText(cards[i].transform, "능력치", "체력 4  공격 2  이동 3  사거리 2", 16f, font);
            SetAnchors(cardStats[i].rectTransform, 0.02f, 0.01f, 0.98f, 0.18f);
            cardStatuses[i] = CreateText(cards[i].transform, "편성 상태", "", 20f, font);
            cardStatuses[i].color = new Color(1f, 0.78f, 0.08f);
            Stretch(cardStatuses[i].rectTransform);
        }

        Image footer = CreateImage(safe, "하단 영역", new Color(0.045f, 0.06f, 0.09f));
        SetAnchors(footer.rectTransform, 0f, 0f, 1f, 0.15f);
        TMP_Text hint = CreateText(footer.transform, "안내", "출전할 유닛 4명을 선택하세요", 22f, font);
        hint.alignment = TextAlignmentOptions.Left;
        hint.color = new Color(0.48f, 0.53f, 0.62f);
        SetAnchors(hint.rectTransform, 0.05f, 0.58f, 0.72f, 0.9f);
        Button start = CreateButton(footer.transform, "전투 시작 버튼", new Color(1f, 0.78f, 0.08f));
        SetAnchors((RectTransform)start.transform, 0.47f, 0.12f, 0.95f, 0.58f);
        TMP_Text startText = CreateText(start.transform, "문구", "전투 시작  >", 28f, font);
        Stretch(startText.rectTransform);

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("safeAreaRoot").objectReferenceValue = safe;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.FindProperty("powerText").objectReferenceValue = power;
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("startButton").objectReferenceValue = start;
        SetObjectArray(serialized.FindProperty("squadSlots"), slots);
        SetObjectArray(serialized.FindProperty("squadNames"), slotNames);
        SetObjectArray(serialized.FindProperty("squadDetails"), slotDetails);
        SetObjectArray(serialized.FindProperty("rosterCards"), cards);
        SetObjectArray(serialized.FindProperty("rosterNames"), cardNames);
        SetObjectArray(serialized.FindProperty("rosterStats"), cardStats);
        SetObjectArray(serialized.FindProperty("rosterStatuses"), cardStatuses);
        SetObjectArray(serialized.FindProperty("rosterMonograms"), cardMonograms);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SquadPath);
        Object.DestroyImmediate(root);
    }

    private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return obj.GetComponent<Button>();
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        if (font != null) text.font = font;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, 0f, 0f, 1f, 1f);
    }

    private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
