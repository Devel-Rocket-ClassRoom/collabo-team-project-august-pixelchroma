using System;
using UnityEngine;

// Placed in 3.Stage List: decides which scenario (if any) plays before each stage.
public class StageStoryTable : MonoBehaviour
{
    [Serializable]
    public class StageStory
    {
        public string label;
        public bool playStory;
        [Tooltip("스토리만 있는 스테이지. 스토리가 끝나면 전투 없이 스테이지 선택으로 돌아갑니다.")]
        public bool noBattle;
        public DialogueDataSO scenario;
    }

    [SerializeField] private StageStory[] stages = new StageStory[0];

    public bool TryGetStory(int stageIndex, out DialogueDataSO scenario, out bool noBattle)
    {
        scenario = null;
        noBattle = false;
        if (stageIndex < 0 || stageIndex >= stages.Length) return false;

        StageStory stage = stages[stageIndex];
        if (stage == null || !stage.playStory || stage.scenario == null) return false;

        scenario = stage.scenario;
        noBattle = stage.noBattle;
        return true;
    }
}

public static class StorySelection
{
    public static DialogueDataSO Scenario { get; set; }
    public static bool NoBattle { get; set; }
}
