using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 유닛이 맞았을 때 머리 위에 뜨는 체력 바입니다.
/// 피해 숫자가 먼저 뜨고, 유닛이 빨갛게 깜빡이는 동안(flashDuration) 기다린 뒤 체력이 줄어듭니다.
/// 모양은 프리팹(3.Prefabs/4.MainGame/Resources/HitHealthBar (PlayHere))에서 수정합니다. 배속을 따릅니다.
/// </summary>
public class HitHealthBarView : MonoBehaviour
{
    public const string ResourcePath = "HitHealthBar (PlayHere)";

    [Header("구성")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("유닛 머리 위를 따라다니는 묶음")]
    [SerializeField] private RectTransform panel;
    [Tooltip("현재 체력. 가로 앵커(anchorMax.x)로 길이를 조절합니다.")]
    [SerializeField] private RectTransform healthFill;
    [Tooltip("깎인 만큼 잠깐 남아 있다가 뒤따라 줄어드는 바")]
    [SerializeField] private RectTransform damageFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text damageText;

    [Header("위치")]
    [Tooltip("유닛 위치에서 위로 띄우는 높이 (월드 단위)")]
    [SerializeField] private float worldHeight = 1.3f;

    [Header("타이밍 (초)")]
    [SerializeField] private float drainDuration = 0.35f;
    [SerializeField] private float trailDelay = 0.15f;
    [SerializeField] private float trailDuration = 0.4f;
    [SerializeField] private float holdAfterDrain = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    private Transform followTarget;

    public IEnumerator Play(
        Transform target, int hpBefore, int hpAfter, int maxHp,
        string damageLabel, float flashDuration)
    {
        gameObject.SetActive(true);
        followTarget = target;
        maxHp = Mathf.Max(1, maxHp);
        hpAfter = Mathf.Max(0, hpAfter);

        float before = Mathf.Clamp01((float)hpBefore / maxHp);
        float after = Mathf.Clamp01((float)hpAfter / maxHp);
        SetFill(healthFill, before);
        SetFill(damageFill, before);
        if (healthText != null) healthText.text = $"{hpBefore} / {maxHp}";
        if (damageText != null)
        {
            damageText.text = damageLabel;
            damageText.gameObject.SetActive(!string.IsNullOrEmpty(damageLabel));
        }
        if (group != null) group.alpha = 1f;
        Follow();

        // 1. 피해 숫자가 튀어나오고, 유닛이 깜빡이는 동안 기다립니다.
        for (float t = 0f; t < flashDuration; t += Time.deltaTime)
        {
            if (damageText != null)
            {
                float pop = t < 0.15f ? Mathf.Lerp(1.6f, 1f, t / 0.15f) : 1f;
                damageText.rectTransform.localScale = Vector3.one * pop;
            }
            Follow();
            yield return null;
        }

        // 2. 체력이 줄고, 흰 바가 조금 늦게 뒤따라 줄어듭니다.
        if (healthText != null) healthText.text = $"{hpAfter} / {maxHp}";
        float total = Mathf.Max(drainDuration, trailDelay + trailDuration);
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            SetFill(healthFill, Mathf.Lerp(before, after, EaseOut(t / drainDuration)));
            SetFill(damageFill, Mathf.Lerp(before, after, EaseOut((t - trailDelay) / trailDuration)));
            Follow();
            yield return null;
        }
        SetFill(healthFill, after);
        SetFill(damageFill, after);

        for (float t = 0f; t < holdAfterDrain; t += Time.deltaTime)
        {
            Follow();
            yield return null;
        }

        for (float t = 0f; t < fadeOutDuration; t += Time.deltaTime)
        {
            if (group != null) group.alpha = 1f - t / fadeOutDuration;
            Follow();
            yield return null;
        }

        followTarget = null;
        gameObject.SetActive(false);
    }

    private void Follow()
    {
        if (followTarget == null || panel == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(followTarget.position + Vector3.up * worldHeight);
        if (screen.z < 0f) return;

        RectTransform parent = panel.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local))
            panel.anchoredPosition = local;
    }

    private static void SetFill(RectTransform fill, float amount)
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), fill.anchorMax.y);
    }

    private static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}
