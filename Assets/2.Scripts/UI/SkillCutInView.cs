using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬을 누르면 나오는 컷인 연출입니다.
/// 가운데 띠가 깔리고, 이미지와 스킬 이름이 오른쪽에서 들어와 잠깐 머문 뒤 왼쪽으로 빠져나갑니다.
/// 모양은 프리팹(3.Prefabs/4.MainGame/Resources/SkillCutIn (PlayHere))에서 수정하고,
/// 이미지는 스킬마다 SkillData의 Cut-in Image로 바꿉니다. 배속(Time.timeScale)을 따릅니다.
/// </summary>
public class SkillCutInView : MonoBehaviour
{
    public const string ResourcePath = "SkillCutIn (PlayHere)";

    [Header("구성")]
    [Tooltip("화면 가운데 깔리는 띠. 들어올 때 나타나고 나갈 때 사라집니다.")]
    [SerializeField] private CanvasGroup band;
    [Tooltip("오른쪽에서 왼쪽으로 지나가는 묶음 (이미지 + 글자)")]
    [SerializeField] private RectTransform slider;
    [SerializeField] private Image cutInImage;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text characterNameText;

    [Header("타이밍 (초)")]
    [SerializeField, Min(0.01f)] private float enterDuration = 0.22f;
    [SerializeField, Min(0f)] private float holdDuration = 0.6f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.2f;
    [Tooltip("머무는 동안 왼쪽으로 천천히 흐르는 거리(px)")]
    [SerializeField] private float holdDrift = 60f;

    private Vector2 restPosition;
    private bool initialized;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (slider != null) restPosition = slider.anchoredPosition;
    }

    public IEnumerator Play(Sprite image, string skillName, string characterName)
    {
        gameObject.SetActive(true);
        Initialize();

        if (cutInImage != null)
        {
            // 이미지를 비워 두면 프리팹에 넣어 둔 기본 이미지를 그대로 씁니다.
            if (image != null) cutInImage.sprite = image;
            cutInImage.enabled = cutInImage.sprite != null;
        }
        if (skillNameText != null) skillNameText.text = skillName;
        if (characterNameText != null) characterNameText.text = characterName;

        Canvas.ForceUpdateCanvases();
        RectTransform area = slider != null ? slider.parent as RectTransform : null;
        float travel = area != null ? area.rect.width : 1080f;

        Vector2 start = restPosition + Vector2.right * travel;
        Vector2 holdEnd = restPosition + Vector2.left * holdDrift;
        Vector2 end = holdEnd + Vector2.left * travel;

        if (band != null) band.alpha = 0f;
        if (slider != null) slider.anchoredPosition = start;

        // 오른쪽에서 들어오기
        for (float t = 0f; t < enterDuration; t += Time.deltaTime)
        {
            float p = t / enterDuration;
            if (band != null) band.alpha = Mathf.Clamp01(p * 2f);
            if (slider != null) slider.anchoredPosition = Vector2.LerpUnclamped(start, restPosition, EaseOutCubic(p));
            yield return null;
        }
        if (band != null) band.alpha = 1f;

        // 잠깐 머물며 천천히 흐르기
        for (float t = 0f; t < holdDuration; t += Time.deltaTime)
        {
            if (slider != null)
                slider.anchoredPosition = Vector2.Lerp(restPosition, holdEnd, t / holdDuration);
            yield return null;
        }

        // 왼쪽으로 빠져나가기
        for (float t = 0f; t < exitDuration; t += Time.deltaTime)
        {
            float p = t / exitDuration;
            if (band != null) band.alpha = 1f - p;
            if (slider != null) slider.anchoredPosition = Vector2.LerpUnclamped(holdEnd, end, EaseInCubic(p));
            yield return null;
        }

        if (slider != null) slider.anchoredPosition = restPosition;
        gameObject.SetActive(false);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t) => t * t * t;
}
