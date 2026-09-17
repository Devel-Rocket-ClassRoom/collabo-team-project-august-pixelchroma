using UnityEngine;

public static class UITheme
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    public const float MatchWidthOrHeight = 0.5f;
    public const float SideMargin = 32f;

    public static readonly Color White = new Color32(0xF4, 0xF5, 0xF7, 0xFF);
    public static readonly Color Charcoal = new Color32(0x17, 0x19, 0x1D, 0xFF);
    public static readonly Color DarkPanel = new Color32(0x20, 0x24, 0x2A, 0xFF);
    public static readonly Color LightGray = new Color32(0xD9, 0xDC, 0xE1, 0xFF);
    public static readonly Color Accent = new Color32(0x3C, 0xD6, 0xE6, 0xFF);
    public static readonly Color Warning = new Color32(0xFF, 0x6B, 0x3D, 0xFF);

    public const float PanelAlpha = 0.85f;

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
