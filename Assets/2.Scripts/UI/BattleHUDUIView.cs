using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button confirmButton;

    public TMP_Text InfoText => infoText;
    public Button UndoButton => undoButton;
    public Button ConfirmButton => confirmButton;
}
