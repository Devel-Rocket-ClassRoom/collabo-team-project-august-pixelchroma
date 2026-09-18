using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private CharacterCommandPanelView commandPanel;

    public TMP_Text InfoText => infoText;
    public Button UndoButton => undoButton;
    public Button ConfirmButton => confirmButton;
    public CharacterCommandPanelView CommandPanel => commandPanel;
}
