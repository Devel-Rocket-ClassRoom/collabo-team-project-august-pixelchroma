using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메인 로비 대표 요원의 말풍선 대사입니다.
/// 로비에 들어올 때는 인사 대사 중 하나, 캐릭터를 터치하면 터치 대사 중 하나를 보여줍니다.
/// </summary>
[CreateAssetMenu(fileName = "LobbyTalk", menuName = "Game/Lobby Talk Data")]
public class LobbyTalkData : ScriptableObject
{
    [Tooltip("로비에 들어왔을 때 말풍선에 나오는 대사 (여러 개면 무작위)")]
    [TextArea(1, 4)] public List<string> greetingLines = new List<string>();

    [Tooltip("캐릭터를 터치할 때마다 나오는 대사")]
    [TextArea(1, 4)] public List<string> touchLines = new List<string>();

    [Tooltip("켜면 터치 대사를 목록 순서대로, 끄면 무작위로 보여줍니다. (무작위일 때 같은 대사가 연달아 나오지 않음)")]
    public bool playInOrder;

    [Header("연출")]
    [Tooltip("한 글자씩 나오는 속도 (초당 글자 수). 0이면 한 번에 표시")]
    [Min(0f)] public float charactersPerSecond = 40f;
    [Tooltip("연속 터치를 막는 간격 (초)")]
    [Min(0f)] public float touchCooldown = 0.35f;
}
