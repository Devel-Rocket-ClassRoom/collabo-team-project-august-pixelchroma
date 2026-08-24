using UnityEngine;

[CreateAssetMenu(fileName = "NewMap", menuName = "SRPG/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Info")]
    public string mapName;

    [Header("Background")]
    public GameObject backgroundPrefab;
    public Vector3 backgroundPosition;
    public Vector3 backgroundRotation;
    public Vector3 backgroundScale = Vector3.one;

    [Header("Camera")]
    public float cameraAngle = 60f;
    public float cameraZoom = 0.7f;
    public float cameraFOV = 50f;

    [Header("Grid")]
    public int gridWidth = 5;
    public int gridHeight = 6;
}
