using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField] private string characterId = "character";
    [SerializeField] private string displayName = "Character";
    [SerializeField] private GameObject battlePrefab;
    [SerializeField, Min(1)] private int maxHP = 3;
    [SerializeField, Min(0)] private int attackPower = 1;
    [SerializeField, Min(1)] private int moveRange = 3;
    [SerializeField, Min(1)] private int attackRange = 1;
    [SerializeField] private Color teamColor = new Color(0.15f, 0.4f, 1f, 1f);

    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public GameObject BattlePrefab => battlePrefab;
    public int MaxHP => maxHP;
    public int AttackPower => attackPower;
    public int MoveRange => moveRange;
    public int AttackRange => attackRange;
    public Color TeamColor => teamColor;

    public void ConfigureRuntime(
        string id,
        string name,
        GameObject prefab,
        int hp,
        int attack,
        int movement,
        int range,
        Color color)
    {
        characterId = id;
        displayName = name;
        battlePrefab = prefab;
        maxHP = hp;
        attackPower = attack;
        moveRange = movement;
        attackRange = range;
        teamColor = color;
    }
}
