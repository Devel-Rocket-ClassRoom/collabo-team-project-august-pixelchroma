#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SquadSlotCardPrefabCreator
{
    [MenuItem("Tools/Squad Slot Card 프리팹 생성")]
    public static void CreatePrefab()
    {
        GameObject root = new GameObject("SquadSlotCard");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        root.AddComponent<CanvasRenderer>();
        Image rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.45f, 0.47f, 0.52f, 1f);

        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(root.transform, false);
        RectTransform prt = portraitObj.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.1f, 0.42f);
        prt.anchorMax = new Vector2(0.9f, 0.95f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        portraitObj.AddComponent<CanvasRenderer>();
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.color = Color.white;
        portraitImg.enabled = false;

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nrt = nameObj.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.04f, 0.18f);
        nrt.anchorMax = new Vector2(0.96f, 0.44f);
        nrt.offsetMin = Vector2.zero;
        nrt.offsetMax = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "빈 슬롯";
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 12;
        nameText.fontSizeMax = 22;
        nameText.raycastTarget = false;

        GameObject detailObj = new GameObject("Detail");
        detailObj.transform.SetParent(root.transform, false);
        RectTransform drt = detailObj.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.04f, 0.02f);
        drt.anchorMax = new Vector2(0.96f, 0.2f);
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;
        TextMeshProUGUI detailText = detailObj.AddComponent<TextMeshProUGUI>();
        detailText.text = "+";
        detailText.fontSize = 16;
        detailText.alignment = TextAlignmentOptions.Center;
        detailText.color = new Color(1f, 1f, 1f, 0.85f);
        detailText.enableAutoSizing = true;
        detailText.fontSizeMin = 10;
        detailText.fontSizeMax = 16;
        detailText.raycastTarget = false;

        SquadSlotCard card = root.AddComponent<SquadSlotCard>();
        SerializedObject so = new SerializedObject(card);
        so.FindProperty("portrait").objectReferenceValue = portraitImg;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("detailText").objectReferenceValue = detailText;
        so.ApplyModifiedProperties();

        string path = "Assets/3.Prefabs/UI/SquadSlotCard.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log("SquadSlotCard 프리팹 생성 완료: " + path);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
    }
}
#endif
