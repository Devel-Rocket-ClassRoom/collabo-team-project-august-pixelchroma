using UnityEngine;
using UnityEditor;
using System.IO;

public static class EnemySpriteLinker
{
    [MenuItem("Tools/적 유닛 스프라이트 자동 연결")]
    public static void LinkSprites()
    {
        string unitFolder = "Assets/5.SOdata/Enemy/Units";
        string spriteFolder = "Assets/4.Image/Characters/Enemy";

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
        if (spriteGuids.Length == 0)
        {
            Debug.LogError("Enemy 스프라이트 폴더에 이미지가 없습니다: " + spriteFolder);
            return;
        }

        var sprites = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (string guid in spriteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                string name = Path.GetFileNameWithoutExtension(path).ToLower();
                sprites[name] = sprite;
                Debug.Log($"발견된 스프라이트: {name} ({path})");
            }
        }

        string[] unitGuids = AssetDatabase.FindAssets("t:EnemyUnitData", new[] { unitFolder });
        int linked = 0;

        foreach (string guid in unitGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData unit = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (unit == null) continue;

            if (unit.BattleSprite != null)
            {
                Debug.Log($"[건너뜀] {unit.name} - 이미 스프라이트 있음");
                continue;
            }

            Sprite matched = FindBestMatch(unit.name, sprites);
            if (matched == null)
                matched = FindBestMatch(unit.CharacterId, sprites);

            if (matched != null)
            {
                SerializedObject so = new SerializedObject(unit);
                SerializedProperty prop = so.FindProperty("battleSprite");
                if (prop != null)
                {
                    prop.objectReferenceValue = matched;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(unit);
                    Debug.Log($"[연결] {unit.name} ← {matched.name}");
                    linked++;
                }
            }
            else
            {
                Debug.LogWarning($"[매칭 실패] {unit.name} - 매칭되는 스프라이트 없음");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"완료: {linked}개 유닛에 스프라이트 연결됨");
    }

    [MenuItem("Tools/적 유닛 스프라이트 강제 연결 (전체)")]
    public static void ForceLink()
    {
        string unitFolder = "Assets/5.SOdata/Enemy/Units";
        string spriteFolder = "Assets/4.Image/Characters/Enemy";

        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
        var spriteList = new System.Collections.Generic.List<Sprite>();
        foreach (string guid in spriteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) spriteList.Add(s);
        }

        if (spriteList.Count == 0)
        {
            Debug.LogError("스프라이트가 없습니다");
            return;
        }

        string[] unitGuids = AssetDatabase.FindAssets("t:EnemyUnitData", new[] { unitFolder });
        int linked = 0;

        foreach (string guid in unitGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData unit = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (unit == null) continue;

            var sprites = new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (Sprite sp in spriteList)
                sprites[sp.name.ToLower()] = sp;

            Sprite matched = FindBestMatch(unit.name, sprites);
            if (matched == null)
                matched = FindBestMatch(unit.CharacterId, sprites);
            if (matched == null)
                matched = spriteList[linked % spriteList.Count];

            SerializedObject so = new SerializedObject(unit);
            SerializedProperty prop = so.FindProperty("battleSprite");
            if (prop != null)
            {
                prop.objectReferenceValue = matched;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(unit);
                Debug.Log($"[강제 연결] {unit.name} ← {matched.name}");
                linked++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"완료: {linked}개 유닛에 스프라이트 강제 연결됨");
    }

    private static Sprite FindBestMatch(
        string unitName,
        System.Collections.Generic.Dictionary<string, Sprite> sprites)
    {
        string lower = unitName.ToLower();

        foreach (var kv in sprites)
        {
            if (kv.Key.Contains(lower) || lower.Contains(kv.Key))
                return kv.Value;
        }

        foreach (var kv in sprites)
        {
            string[] parts = kv.Key.Replace("_battle", "").Replace("_", " ").Split(' ');
            foreach (string part in parts)
            {
                if (part.Length > 2 && lower.Contains(part))
                    return kv.Value;
            }
        }

        return null;
    }
}
