using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeploymentUIView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform characterContainer;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject cancelHint;

    public GameObject Panel => panel;
    public RectTransform CharacterContainer => characterContainer;
    public TMP_Text InfoText => infoText;
    public Button StartButton => startButton;
    public GameObject CancelHint => cancelHint;
}
