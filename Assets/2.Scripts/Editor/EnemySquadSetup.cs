using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// "능력자자유해방전선" 단체와 소속 적 캐릭터 에셋을 한 번에 생성합니다.
/// Tools > PixelChroma > 적 부대 생성 으로 실행합니다.
/// 이미 있는 에셋은 덮어쓰지 않고 건너뜁니다.
/// </summary>
public static class EnemySquadSetup
{
    private const string EnemyRoot = "Assets/5.SOdata/Enemy";
    private const string UnitFolder = EnemyRoot + "/Units";
    private const string SquadFolder = EnemyRoot + "/Squads";
    private const string ProfileFolder = EnemyRoot + "/AIProfiles";

    [MenuItem("Tools/PixelChroma/적 부대 생성 (능력자자유해방전선)")]
    public static void CreateAll()
    {
        EnsureFolders();

        AIWeightProfile profile = CreateOrGet<AIWeightProfile>(
            ProfileFolder, "AIWeights_Default");

        // 새로 만든 프로필은 난이도 마스크가 0이므로 기본 조합을 채워 줍니다.
        SerializedObject profileSo = new SerializedObject(profile);
        SerializedProperty easy = profileSo.FindProperty("easyMetrics");
        if (easy.intValue == 0)
        {
            easy.intValue = (int)AIMetricPresets.Easy;
            profileSo.FindProperty("normalMetrics").intValue = (int)AIMetricPresets.Normal;
            profileSo.FindProperty("hardMetrics").intValue = (int)AIMetricPresets.Hard;
            profileSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        // ── 소속 캐릭터 ────────────────────────────────
        EnemyUnitData patroller = CreateUnit(
            "e_patroller", "순찰병",
            EnemyRole.Tank, AttackType.Pierce, ArmorType.Light,
            hp: 5, atk: 1, mov: 3, range: 1,
            EnemyTrait.None, 0, 0, 0);

        EnemyUnitData charger = CreateUnit(
            "e_charger", "돌격병",
            EnemyRole.MeleeDealer, AttackType.Explosive, ArmorType.Light,
            hp: 4, atk: 3, mov: 4, range: 1,
            EnemyTrait.Momentum, 1, 0, 0);

        EnemyUnitData bulwark = CreateUnit(
            "e_bulwark", "방패병",
            EnemyRole.Tank, AttackType.Pierce, ArmorType.Heavy,
            hp: 7, atk: 1, mov: 2, range: 1,
            EnemyTrait.Guard, 1, 1, 0);

        EnemyUnitData emplacement = CreateUnit(
            "e_emplacement", "고정포",
            EnemyRole.RangedDealer, AttackType.Explosive, ArmorType.Heavy,
            hp: 4, atk: 2, mov: 1, range: 4,
            EnemyTrait.Emplaced, 0, 0, 0);

        EnemyUnitData marksman = CreateUnit(
            "e_marksman", "저격수",
            EnemyRole.RangedDealer, AttackType.Pierce, ArmorType.Light,
            hp: 3, atk: 3, mov: 2, range: 5,
            EnemyTrait.HighGroundBonus, 1, 0, 0);

        EnemyUnitData spotter = CreateUnit(
            "e_spotter", "관측수",
            EnemyRole.Supporter, AttackType.Mystic, ArmorType.Special,
            hp: 3, atk: 1, mov: 3, range: 4,
            EnemyTrait.Spot, 1, 3, 0);

        EnemyUnitData corpsman = CreateUnit(
            "e_corpsman", "야전위생병",
            EnemyRole.Healer, AttackType.Mystic, ArmorType.Special,
            hp: 4, atk: 1, mov: 3, range: 3,
            EnemyTrait.Heal, 2, 3, 2);

        EnemyUnitData jammer = CreateUnit(
            "e_jammer", "전파교란기",
            EnemyRole.Supporter, AttackType.Mystic, ArmorType.Special,
            hp: 3, atk: 1, mov: 3, range: 3,
            EnemyTrait.Slow, 1, 2, 1);

        EnemyUnitData praetorian = CreateUnit(
            "e_praetorian", "친위대",
            EnemyRole.Tank, AttackType.Explosive, ArmorType.Heavy,
            hp: 6, atk: 2, mov: 3, range: 2,
            EnemyTrait.Guard, 1, 1, 0);

        EnemyUnitData commander = CreateUnit(
            "e_commander", "지휘관",
            EnemyRole.Supporter, AttackType.Mystic, ArmorType.Special,
            hp: 8, atk: 2, mov: 3, range: 3,
            EnemyTrait.Command, 1, 3, 0, threatModifier: 40);

        // ── 단체 ───────────────────────────────────────
        CreateSquad("Squad_01_순찰반", "능력자자유해방전선 · 순찰반",
            SquadDoctrine.Assault, AIDifficulty.Easy, profile,
            "가장 가까운 표적으로 직진합니다. 역할 구분 없이 움직이는 초반 부대입니다.",
            new[] { (patroller, 2), (charger, 1) });

        CreateSquad("Squad_02_방벽반", "능력자자유해방전선 · 방벽반",
            SquadDoctrine.Hold, AIDifficulty.Normal, profile,
            "3칸 안에 들어오기 전까지 움직이지 않고 고지·엄폐를 점유합니다.",
            new[] { (bulwark, 1), (emplacement, 2), (corpsman, 1) });

        CreateSquad("Squad_03_저격반", "능력자자유해방전선 · 저격반",
            SquadDoctrine.Snipe, AIDifficulty.Normal, profile,
            "체력이 낮거나 상성상 약한 대상을 우선하며 고지를 선점합니다.",
            new[] { (marksman, 2), (spotter, 1), (patroller, 1) });

        CreateSquad("Squad_04_지휘본대", "능력자자유해방전선 · 지휘본대",
            SquadDoctrine.Command, AIDifficulty.Hard, profile,
            "지휘관이 아군을 강화하고 표적을 분배합니다. 위험하면 후퇴합니다.",
            new[] { (commander, 1), (praetorian, 2), (corpsman, 1), (marksman, 1) });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EnemySquadSetup] 능력자자유해방전선 생성 완료 — " +
                  $"캐릭터 10종, 단체 4종. 경로: {EnemyRoot}");

        Object created = AssetDatabase.LoadAssetAtPath<Object>(
            $"{SquadFolder}/Squad_01_순찰반.asset");
        if (created != null) Selection.activeObject = created;
    }

    // ────────────────────────────────────────────────────

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/5.SOdata", "Enemy");
        EnsureFolder(EnemyRoot, "Units");
        EnsureFolder(EnemyRoot, "Squads");
        EnsureFolder(EnemyRoot, "AIProfiles");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent))
        {
            string grandParent = Path.GetDirectoryName(parent).Replace('\\', '/');
            string leaf = Path.GetFileName(parent);
            if (!AssetDatabase.IsValidFolder(grandParent))
                Directory.CreateDirectory(grandParent);
            AssetDatabase.CreateFolder(grandParent, leaf);
        }
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static T CreateOrGet<T>(string folder, string fileName) where T : ScriptableObject
    {
        string path = $"{folder}/{fileName}.asset";
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        T created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static EnemyUnitData CreateUnit(
        string id, string name,
        EnemyRole role, AttackType attack, ArmorType armor,
        int hp, int atk, int mov, int range,
        EnemyTrait trait, int traitValue, int traitRadius, int traitCooldown,
        int threatModifier = 0)
    {
        string path = $"{UnitFolder}/{id}.asset";
        EnemyUnitData existing = AssetDatabase.LoadAssetAtPath<EnemyUnitData>(path);
        if (existing != null) return existing;

        EnemyUnitData data = ScriptableObject.CreateInstance<EnemyUnitData>();

        // 기존 CharacterData 필드는 상속받으므로 그대로 채웁니다.
        // 이미지(battleSprite)와 프리팹은 인스펙터에서 직접 지정하세요.
        data.ConfigureRuntime(
            id, name, null, hp, atk, mov, range,
            new Color(0.85f, 0.25f, 0.25f, 1f));

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("role").enumValueIndex = (int)role;
        so.FindProperty("attackType").enumValueIndex = (int)attack;
        so.FindProperty("armorType").enumValueIndex = (int)armor;
        so.FindProperty("trait").enumValueIndex = (int)trait;
        so.FindProperty("traitValue").intValue = traitValue;
        so.FindProperty("traitRadius").intValue = traitRadius;
        so.FindProperty("traitCooldown").intValue = traitCooldown;
        so.FindProperty("threatModifier").intValue = threatModifier;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void CreateSquad(
        string fileName, string squadName,
        SquadDoctrine doctrine, AIDifficulty difficulty,
        AIWeightProfile profile, string description,
        (EnemyUnitData unit, int count)[] roster)
    {
        string path = $"{SquadFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemySquadData>(path) != null) return;

        EnemySquadData squad = ScriptableObject.CreateInstance<EnemySquadData>();

        SerializedObject so = new SerializedObject(squad);
        so.FindProperty("squadId").stringValue = fileName.ToLowerInvariant();
        so.FindProperty("squadName").stringValue = squadName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("doctrine").enumValueIndex = (int)doctrine;
        so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
        so.FindProperty("weightProfile").objectReferenceValue = profile;

        SerializedProperty members = so.FindProperty("members");
        members.arraySize = roster.Length;
        for (int i = 0; i < roster.Length; i++)
        {
            SerializedProperty element = members.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("unit").objectReferenceValue = roster[i].unit;
            element.FindPropertyRelative("count").intValue = roster[i].count;
            element.FindPropertyRelative("preferredCells").arraySize = 0;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(squad, path);
    }
}
