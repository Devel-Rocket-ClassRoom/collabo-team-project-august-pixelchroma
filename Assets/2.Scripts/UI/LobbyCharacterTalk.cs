using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비의 대표 요원을 터치하면 말풍선(지휘 안내) 대사가 바뀝니다.
/// 대사는 LobbyTalkData SO에서 수정합니다. 대표 요원 오브젝트에 붙입니다.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class LobbyCharacterTalk : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private LobbyTalkData talkData;
    [Tooltip("대사가 들어갈 글자 (지휘 안내/문구)")]
    [SerializeField] private TMP_Text bubbleText;
    [Tooltip("대사가 바뀔 때 통통 튀는 말풍선 (지휘 안내)")]
    [SerializeField] private RectTransform bubble;

    private int lastIndex = -1;
    private int orderIndex;
    private float nextTouchTime;
    private Coroutine typing;
    private Vector3 characterScale;

    private void Awake()
    {
        // 캐릭터 이미지가 터치를 받도록 켭니다.
        GetComponent<Graphic>().raycastTarget = true;
        characterScale = transform.localScale;
    }

    private void Start()
    {
        if (talkData != null && talkData.greetingLines.Count > 0)
            Say(talkData.greetingLines[Random.Range(0, talkData.greetingLines.Count)], false);
    }

    private void OnDisable()
    {
        transform.DOKill();
        if (bubble != null) bubble.DOKill();
        transform.localScale = characterScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (talkData == null || talkData.touchLines.Count == 0) return;
        if (Time.unscaledTime < nextTouchTime) return;
        nextTouchTime = Time.unscaledTime + talkData.touchCooldown;

        Say(PickTouchLine(), true);
    }

    private string PickTouchLine()
    {
        var lines = talkData.touchLines;
        if (talkData.playInOrder)
        {
            string line = lines[orderIndex % lines.Count];
            orderIndex++;
            return line;
        }

        int index = Random.Range(0, lines.Count);
        if (lines.Count > 1 && index == lastIndex)
            index = (index + 1 + Random.Range(0, lines.Count - 1)) % lines.Count;
        lastIndex = index;
        return lines[index];
    }

    private void Say(string line, bool reactToTouch)
    {
        if (bubbleText == null) return;

        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(line));

        if (bubble != null)
        {
            bubble.DOKill(true);
            bubble.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.8f).SetUpdate(true);
        }

        if (reactToTouch)
        {
            transform.DOKill(true);
            transform.localScale = characterScale;
            transform.DOPunchScale(new Vector3(0.02f, 0.035f, 0f), 0.3f, 5, 0.6f).SetUpdate(true);
        }
    }

    private IEnumerator TypeLine(string line)
    {
        bubbleText.text = line;
        float speed = talkData != null ? talkData.charactersPerSecond : 0f;
        if (speed <= 0f)
        {
            bubbleText.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        bubbleText.ForceMeshUpdate();
        int total = bubbleText.textInfo.characterCount;
        float shown = 0f;
        bubbleText.maxVisibleCharacters = 0;
        while (shown < total)
        {
            shown += speed * Time.unscaledDeltaTime;
            bubbleText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        bubbleText.maxVisibleCharacters = int.MaxValue;
        typing = null;
    }
}
