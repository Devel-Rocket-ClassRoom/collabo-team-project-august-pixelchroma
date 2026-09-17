using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSoundData",menuName ="Scenario/SoundData")]

public class SoundDataSO :ScriptableObject
{
    public List<BGMEvent> BGMEvents = new List<BGMEvent>();
}
[System.Serializable]
public class BGMEvent
{
    public string EventName;
    public int StartID;
    public int EndID;
    public int BGMIndex;
    public AudioClip BGMClip;
    public float FadeDuration = 1.5f;

    
}