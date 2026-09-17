using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StageNodeState
{
    Locked,
    Available,
    Cleared
}

public class StageNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private ChamferGraphic node;
    [SerializeField] private ChamferGraphic ring;
    [SerializeField] private TMP_Text caseLabel;
    [SerializeField] private TMP_Text stateLabel;

    public Button Button => button;

    public void Setup(string label, StageNodeState state, bool isBoss)
    {
        caseLabel.text = label;

        ApplyShape(node, isBoss);
        ApplyShape(ring, isBoss);

        Color highlight = isBoss ? UITheme.Warning : UITheme.Accent;
        switch (state)
        {
            case StageNodeState.Cleared:
                node.color = UITheme.White;
                ring.color = UITheme.WithAlpha(UITheme.White, 0.5f);
                stateLabel.text = "CLEAR";
                stateLabel.color = UITheme.Charcoal;
                caseLabel.color = UITheme.White;
                break;
            case StageNodeState.Available:
                node.color = highlight;
                ring.color = highlight;
                stateLabel.text = isBoss ? "BOSS" : "OPEN";
                stateLabel.color = UITheme.Charcoal;
                caseLabel.color = highlight;
                break;
            default:
                node.color = UITheme.WithAlpha(UITheme.DarkPanel, 0.9f);
                ring.color = UITheme.WithAlpha(UITheme.LightGray, 0.25f);
                stateLabel.text = "LOCK";
                stateLabel.color = UITheme.WithAlpha(UITheme.LightGray, 0.5f);
                caseLabel.color = UITheme.WithAlpha(UITheme.LightGray, 0.5f);
                break;
        }

        button.interactable = state != StageNodeState.Locked;
    }

    // Boss nodes are diamonds; regular nodes are octagons.
    private static void ApplyShape(ChamferGraphic graphic, bool isBoss)
    {
        Rect rect = graphic.rectTransform.rect;
        float half = Mathf.Min(rect.width, rect.height) * 0.5f;
        float cut = isBoss ? half : half * 0.3f;
        graphic.SetCuts(cut, cut, cut, cut);
    }
}
