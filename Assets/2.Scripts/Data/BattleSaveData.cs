using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>전투 중 유닛 한 명의 상태입니다.</summary>
[Serializable]
public class BattleUnitSave
{
    public string characterId;
    public int x;
    public int y;
    public int hp;
    public bool hasActed;
    public int sp;
    public int guardTurns;
    public int buffTurns;
    public bool sentryAvailable;
    public bool phased;
    public int phaseAttackPercent;
    public int phasePenaltyTurns;
    public int buffAttackPercent;
    public int buffDefensePercent;
    public int debuffAttackPercent;
    public int debuffDefensePercent;
    public int debuffTurns;

    public Vector2Int Position => new Vector2Int(x, y);
}

/// <summary>
/// 진행 중인 전투를 그대로 복원하기 위한 데이터입니다.
/// 난수 상태까지 저장하므로 이어서 해도 판정이 똑같이 재현됩니다.
/// </summary>
[Serializable]
public class BattleSaveData
{
    // 2: 쿨타임 대신 SP와 유체화 상태를 저장합니다. 1 버전 저장은 이어하기 없이 폐기됩니다.
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public string sceneName;
    public int stageIndex;
    public int turnCount;

    public uint randomSeed;
    public uint randomState;
    public int randomCallCount;

    public List<BattleUnitSave> players = new List<BattleUnitSave>();
    public List<BattleUnitSave> enemies = new List<BattleUnitSave>();

    public string savedAt;
}
