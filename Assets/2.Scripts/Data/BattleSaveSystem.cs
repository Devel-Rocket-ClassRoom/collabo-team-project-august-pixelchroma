using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 전투 상황을 JSON 파일로 저장하고 불러옵니다.
/// 저장 위치는 Application.persistentDataPath 이며, 앱을 종료해도 남습니다.
/// </summary>
public static class BattleSaveSystem
{
    private const string FileName = "battle_save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => File.Exists(FilePath);

    public static void Save(BattleSaveData data)
    {
        if (data == null) return;

        data.version = BattleSaveData.CurrentVersion;
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BattleSave] 저장 실패: {e.Message}");
        }
    }

    public static bool TryLoad(out BattleSaveData data)
    {
        data = null;
        if (!HasSave) return false;

        try
        {
            data = JsonUtility.FromJson<BattleSaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BattleSave] 불러오기 실패: {e.Message}");
            Delete();
            return false;
        }

        if (data == null || data.version != BattleSaveData.CurrentVersion)
        {
            // 저장 형식이 바뀌면 이어하기를 포기하고 새 전투를 시작합니다.
            Delete();
            data = null;
            return false;
        }
        return true;
    }

    /// <summary>해당 스테이지에 이어할 전투가 남아 있는지 확인합니다.</summary>
    public static bool HasSaveForStage(int stageIndex)
    {
        if (!TryLoad(out BattleSaveData data)) return false;
        return data.stageIndex == stageIndex && data.players.Count > 0;
    }

    public static void Delete()
    {
        try
        {
            if (HasSave) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BattleSave] 삭제 실패: {e.Message}");
        }
    }
}
