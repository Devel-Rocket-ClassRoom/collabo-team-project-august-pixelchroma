using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 튜토리얼을 끝냈는지 기록합니다.
/// 끝까지 마치면 persistentDataPath에 "Tutorial Clear.json"이 생기고,
/// 이 파일이 지워지지 않는 한 타이틀에서 튜토리얼로 다시 보내지 않습니다.
/// </summary>
public static class TutorialProgress
{
    public const string FileName = "Tutorial Clear.json";

    [Serializable]
    private class ClearData
    {
        public bool cleared = true;
        public string clearedAt;
        public string gameVersion;
    }

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool IsCleared => File.Exists(FilePath);

    public static void MarkCleared()
    {
        var data = new ClearData
        {
            clearedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameVersion = Application.version
        };
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[Tutorial] 튜토리얼 완료 기록: {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tutorial] 완료 파일을 쓰지 못했습니다: {e.Message}");
        }
    }

    /// <summary>튜토리얼을 다시 보고 싶을 때 (개발용) 완료 기록을 지웁니다.</summary>
    public static void Reset()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
