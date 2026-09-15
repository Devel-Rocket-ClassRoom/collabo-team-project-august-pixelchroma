using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "OwnedAgentCatalog", menuName = "Game/Owned Agent Catalog")]
public class OwnedAgentCatalog : ScriptableObject
{
    [SerializeField] private List<CharacterData> ownedAgents = new List<CharacterData>();
    [SerializeField] private TMP_FontAsset uiFont;

    public IReadOnlyList<CharacterData> OwnedAgents => ownedAgents;
    public TMP_FontAsset UIFont => uiFont;
}
