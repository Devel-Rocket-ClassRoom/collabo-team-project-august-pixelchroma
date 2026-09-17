using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine.Video;


[CreateAssetMenu(fileName = "NewScenario", menuName = "Scenario/DialogueData")] //t시나리오 에디터
public class DialogueDataSO : ScriptableObject
{
    public List<DialogueGroup> groups = new List<DialogueGroup>();

    //public List<DialogueEntry> entries = new List<DialogueEntry>();
    [Header("챕터내 다음 시나리오 설정 * 씬안에서만 들어갈 시나리오를 뜻 합니다")]
    [Tooltip("어느 씬에 넣을지 확인후 다음 이야기의 SO파일을 넣어주세요!")]
    public DialogueDataSO nextStorySO;
#if UNITY_EDITOR

    [ContextMenu("ID 일괄 재정렬")]
    public void ReorderIDs()
    {
        int currentID = 0;
        foreach (var group in groups)
        {
            foreach (var entry in group.entries)
            {
                entry.id = currentID++;
            }
        }
        EditorUtility.SetDirty(this);
        Debug.Log("<color=lime>모든 대사 ID가 순차적으로 재정렬되었습니다!</color>");
    }
#endif
}




[System.Serializable]
public class DialogueGroup
{
    public int id;
    public string GroupName;
    public List<DialogueEntry> entries = new List<DialogueEntry>();
}

[System.Serializable]
public class DialogueEntry
{
    [Header("챕터 id 번호를 꼭 넣어주세요")]
    public int id;
    [Header(" 캐릭터 이름 플레이어 '나'는 자동 묵음 처리됩니다 ")]
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    [Header("캐릭터 통 일러스트(1980x1080)")]
    public Sprite characterIllust;




    //public Sprite CharacterPNG;
    [Header(" 뒷 배경 전용")]
    public Sprite BackGroundSprit;
    [Header("ChatUI 관련 체크시 켜져 있고 체크해제시 꺼져있습니다")]
    public bool showChatUI = true; // 캐릭터 채팅창 켜고 끄기

    [Header("효과음")]
    public AudioClip EffectSound;
    [Range(0f, 1f)] public float seVolune = 1f;

    [Header(" 선택지 전용칸 해당 id숫자를 넣으면 클릭시 이동합니다")]
    public List<ChoiceData> choices = new List<ChoiceData>();

    [Header("캐릭터 개별 데이터")]
    public CharaterData Char1;
    public CharaterData Char2;

    [Header("이 대사 이후 이동할 번호 (기본 값 -1은 순차진행)")]
    public int nextIndexOverride = -1;
    [Header("영상 및 오브젝트 연출")]
    public VideoClip effectVideoClip;

}
    [System.Serializable]
public class CharaterData // 캐릭터를 더 추가해야 할수있기 떄문에 따로 분류해둠
{
    [Header("캐릭터 전용 PNG(배경투명도 꼭 확인!)")]
    public Sprite CharacterPNG;
    [Header("캐릭터 애니메이션칸 X는 +하면 오른쪽으로이동 Y는+하면 위로이동합니다 ")]
    public Vector2 CharacterPos = new Vector2(0, -100);
    [Header("캐릭터 크기조절")]
    [Range(0f, 3f)]
    public float CharacterScale = 1f;
    [Header("캐릭터의 회전을 넣을 수있습니다")]
    public float CharacterRotation;
    [Header(" 왼쪽에 가까우면 속도가 빨라지고 오른쪽에 당기면 속도가 느려집니다")]
    [Range(0f, 2f)]
    public float moveDuration = 0.5f;
}





[System.Serializable]
public class ChoiceData
{
    public string choiceText;
    public int choiceIndex;
}



