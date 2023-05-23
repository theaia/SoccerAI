using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
///     NetworkBehaviours cannot easily be parented, so the network logic will take place
///     on the network scene object "NetworkLobby"
/// </summary>
public class RoomScreen : MonoBehaviour {
    [SerializeField] private LobbyPlayerPanel _playerPanelPrefab;
    [SerializeField] private Transform _homePlayerPanelParent;
    [SerializeField] private Transform _awayPlayerPanelParent;
    [SerializeField] private TMP_Text _waitingText;
    [SerializeField] private GameObject _startButton, _readyButton;

    private readonly List<LobbyPlayerPanel> _playerPanels = new();
    private bool _allReady;
    private bool _ready;

    public static event Action StartPressed; 

    private void OnEnable() {
        foreach (Transform _child in _homePlayerPanelParent) Destroy(_child.gameObject);
        foreach (Transform _child in _awayPlayerPanelParent) Destroy(_child.gameObject);
        _playerPanels.Clear();

        LobbyOrchestrator.LobbyPlayersUpdated += NetworkLobbyPlayersUpdated;
        MatchmakingService.CurrentLobbyRefreshed += OnCurrentLobbyRefreshed;
        _startButton.SetActive(false);

        _ready = false;
    }

    private void OnDisable() {
		LobbyOrchestrator.LobbyPlayersUpdated -= NetworkLobbyPlayersUpdated;
        MatchmakingService.CurrentLobbyRefreshed -= OnCurrentLobbyRefreshed;
    }

    public static event Action LobbyLeft;

    public void OnLeaveLobby() {
        LobbyLeft?.Invoke();
    }

    private void NetworkLobbyPlayersUpdated(Dictionary<ulong, LobbyOrchestrator.PlayerLobbyNetworkData> players) {
        var allActivePlayerIds = players.Keys;

        // Remove all inactive panels
        var toDestroy = _playerPanels.Where(p => !allActivePlayerIds.Contains(p.PlayerId)).ToList();
        foreach (var panel in toDestroy) {
            _playerPanels.Remove(panel);
            Destroy(panel.gameObject);
        }

        foreach (var player in players) {
            var currentPanel = _playerPanels.FirstOrDefault(p => p.PlayerId == player.Key);
            if (currentPanel != null) {
                //Debug.Log($"Updating Player {player.Key} panel because it's not null");
                currentPanel.SetReady(player.Value.IsReady);
                currentPanel.SetName(player.Value.PlayerName);
            }
            else {
                //Debug.Log($"Creating new Player {player.Key} panel because it is null");
                var panel = Instantiate(_playerPanelPrefab, player.Value.Team == 1 ? _homePlayerPanelParent : _awayPlayerPanelParent);
                panel.Init(player.Key, player.Value);
                _playerPanels.Add(panel);
            }
        }

        _startButton.SetActive(NetworkManager.Singleton.IsHost && players.All(p => p.Value.IsReady));
    }

    private void OnCurrentLobbyRefreshed(Lobby lobby) {
        _waitingText.text = $"Waiting on players... {lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void OnReadyClicked() {
        /*_ready = !_ready;
        _readyButton.SetActive(_ready);*/
    }

    public void OnStartClicked() {
        StartPressed?.Invoke();
    }
}