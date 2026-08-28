using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackPreviewUIView : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    public RectTransform Panel => panel;
    public TMP_Text PreviewText => previewText;
    public Button CancelButton => cancelButton;
    public Button ConfirmButton => confirmButton;
}
