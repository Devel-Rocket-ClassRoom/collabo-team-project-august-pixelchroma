using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SquadFormationUIView : MonoBehaviour
{
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button startButton;
    [SerializeField] private List<Button> squadSlots = new List<Button>();
    [SerializeField] private List<TMP_Text> squadNames = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> squadDetails = new List<TMP_Text>();
    [SerializeField] private List<Button> rosterCards = new List<Button>();
    [SerializeField] private List<TMP_Text> rosterNames = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> rosterStats = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> rosterStatuses = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> rosterMonograms = new List<TMP_Text>();

    public RectTransform SafeAreaRoot => safeAreaRoot;
    public TMP_Text CountText => countText;
    public TMP_Text PowerText => powerText;
    public Button BackButton => backButton;
    public Button StartButton => startButton;
    public IReadOnlyList<Button> SquadSlots => squadSlots;
    public IReadOnlyList<TMP_Text> SquadNames => squadNames;
    public IReadOnlyList<TMP_Text> SquadDetails => squadDetails;
    public IReadOnlyList<Button> RosterCards => rosterCards;
    public IReadOnlyList<TMP_Text> RosterNames => rosterNames;
    public IReadOnlyList<TMP_Text> RosterStats => rosterStats;
    public IReadOnlyList<TMP_Text> RosterStatuses => rosterStatuses;
    public IReadOnlyList<TMP_Text> RosterMonograms => rosterMonograms;
}
