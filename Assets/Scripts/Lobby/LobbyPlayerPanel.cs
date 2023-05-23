using TMPro;
using UnityEngine;

public class LobbyPlayerPanel : MonoBehaviour {
    [SerializeField] private TMP_Text _nameText, _statusText;

    public ulong PlayerId { get; private set; }

    public void Init(ulong _playerId, LobbyOrchestrator.PlayerLobbyNetworkData _networkData) {
        PlayerId = _playerId;
        SetName(_networkData.PlayerName);
    }

    public void SetReady(bool _value) {
	    _statusText.text = _value ? "Ready" : "Not Ready";
        _statusText.color = _value ? Color.green : Color.white ;
    }
    
    public void SetName(string _value) {
        _nameText.text = _value;
    }
}