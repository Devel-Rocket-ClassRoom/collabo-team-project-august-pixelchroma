using UnityEngine;

[CreateAssetMenu(fileName = "GameTextData", menuName = "Game/TextData")]
public class GameTextData : ScriptableObject
{
    [Header("Deployment")]
    [Tooltip("{0} = deployed count, {1} = max units")]
    public string deployFormat = "DEPLOY: {0} / {1}\nTap green tiles";

    [Header("Ready")]
    public string readyToStart = "All units ready!";

    [Header("Player Turn")]
    [Tooltip("{0} = turn count")]
    public string playerTurnHeader = "YOUR TURN - Turn {0}";
    public string idle = "Tap your unit";
    public string unitSelected = "Blue=Move  Red=Attack";
    public string unitMoved = "Red=Attack  UNDO/SKIP";

    [Tooltip("{0} = HP, {1} = ATK, {2} = MOV")]
    public string unitStatsFormat = "\nHP:{0}  ATK:{1}  MOV:{2}";

    [Header("Enemy Turn")]
    public string enemyTurn = "ENEMY TURN...";

    [Header("Battle Result")]
    public string victory = "VICTORY!";
    public string defeat = "DEFEAT...";
}
