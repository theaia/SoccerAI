using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyOrchestrator : NetworkBehaviour {
	[SerializeField] TMP_InputField joinInputCode;
	[SerializeField] TextMeshProUGUI lobbyCode;

	[SerializeField] GameObject createScreen;
	[SerializeField] GameObject roomScreen;

	private string playerName;
	
	private void Start() {
		playerName = "AIA" + UnityEngine.Random.Range(10, 99);
		UpdatePlayerName(playerName);
		
		createScreen.SetActive(true);
		roomScreen.SetActive(false);

		CreateLobbyScreen.LobbyCreated += CreateLobby;
		RoomScreen.LobbyLeft += OnLobbyLeft;
		RoomScreen.StartPressed += OnGameStart;

		NetworkObject.DestroyWithScene = true;
	}

	#region Create

	private async void CreateLobby(LobbyData data) {
		using (new Load("Creating Lobby...")) {
			try {
				//await MatchmakingService.CreateLobbyWithAllocation(data);
				//var lobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();

				Lobby _lobby = await LobbyService.Instance.CreateLobbyAsync(data.Name, data.MaxPlayers);
				
				createScreen.gameObject.SetActive(false);
				roomScreen.gameObject.SetActive(true);
				UpdateLobbyCode(_lobby.LobbyCode);
				// Starting the host immediately will keep the relay server alive
				NetworkManager.Singleton.StartHost();
			} catch (Exception _e) {
				Debug.LogError(_e);
				CanvasUtilities.Instance.ShowError("Failed creating lobby");
			}
		}
	}

	#endregion

	#region Join

	public async void JoinLobby() {
		using (new Load("Join Lobby...")) {
			try {
				Lobby _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinInputCode.text);
				//await MatchmakingService.JoinLobbyWithAllocation(_lobby.Id);

				createScreen.gameObject.SetActive(false);
				roomScreen.gameObject.SetActive(true);
				UpdateLobbyCode(_lobby.LobbyCode);
				// Starting the host immediately will keep the relay server alive
				NetworkManager.Singleton.StartClient();
			} catch (Exception _e) {
				Debug.LogError(_e);
				CanvasUtilities.Instance.ShowError("Failed joining lobby");
			}
		}
	}

	#endregion
	
	#region Room

	private readonly Dictionary<ulong, bool> _playersInLobby = new();
	public static event Action<Dictionary<ulong, bool>> LobbyPlayersUpdated;
	private float _nextLobbyUpdate;

	public override void OnNetworkSpawn() {
		if (IsServer) {
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
			_playersInLobby.Add(NetworkManager.Singleton.LocalClientId, false);
			UpdateInterface();
		}

		// Client uses this in case host destroys the lobby
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;


	}

	private void OnClientConnectedCallback(ulong playerId) {
		if (!IsServer) return;

		// Add locally
		if (!_playersInLobby.ContainsKey(playerId)) _playersInLobby.Add(playerId, false);

		PropagateToClients();

		UpdateInterface();
	}

	private void PropagateToClients() {
		foreach (var player in _playersInLobby) UpdatePlayerClientRpc(player.Key, player.Value);
	}

	[ClientRpc]
	private void UpdatePlayerClientRpc(ulong clientId, bool isReady) {
		if (IsServer) return;

		if (!_playersInLobby.ContainsKey(clientId)) _playersInLobby.Add(clientId, isReady);
		else _playersInLobby[clientId] = isReady;
		UpdateInterface();
	}

	private void OnClientDisconnectCallback(ulong playerId) {
		if (IsServer) {
			// Handle locally
			if (_playersInLobby.ContainsKey(playerId)) _playersInLobby.Remove(playerId);

			// Propagate all clients
			RemovePlayerClientRpc(playerId);

			UpdateInterface();
		} else {
			// This happens when the host disconnects the lobby
			roomScreen.gameObject.SetActive(false);
			createScreen.gameObject.SetActive(true);
			OnLobbyLeft();
		}
	}

	[ClientRpc]
	private void RemovePlayerClientRpc(ulong clientId) {
		if (IsServer) return;

		if (_playersInLobby.ContainsKey(clientId)) _playersInLobby.Remove(clientId);
		UpdateInterface();
	}

	public void OnReadyClicked() {
		SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetReadyServerRpc(ulong playerId) {
		_playersInLobby[playerId] = true;
		PropagateToClients();
		UpdateInterface();
	}

	private void UpdateInterface() {
		LobbyPlayersUpdated?.Invoke(_playersInLobby);
	}

	private async void OnLobbyLeft() {
		using (new Load("Leaving Lobby...")) {
			_playersInLobby.Clear();
			NetworkManager.Singleton.Shutdown();
			await MatchmakingService.LeaveLobby();
			UpdateLobbyCode();
			roomScreen.gameObject.SetActive(false);
			createScreen.gameObject.SetActive(true);
		}
	}

	public override void OnDestroy() {

		base.OnDestroy();
		CreateLobbyScreen.LobbyCreated -= CreateLobby;
		RoomScreen.LobbyLeft -= OnLobbyLeft;
		RoomScreen.StartPressed -= OnGameStart;

		// We only care about this during lobby
		if (NetworkManager.Singleton != null) {
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		}

	}

	private async void OnGameStart() {
		using (new Load("Starting the game...")) {
			//await MatchmakingService.LockLobby();
			NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
		}
	}

	#endregion

	private void UpdateLobbyCode(string _value = "") {
		if (_value != "") {
			lobbyCode.text = _value;
			lobbyCode.transform.parent.gameObject.SetActive(true);
		} else {
			Debug.Log("Lobby code is empty.");
			lobbyCode.transform.parent.gameObject.SetActive(false);
		}
	}
	
	private void UpdatePlayerName(string _value) {
		AuthenticationService.Instance.UpdatePlayerNameAsync(_value);
		Debug.Log($"Setting player name to {_value}");
	}

	public void CopyCodeToClipboard() {
		GUIUtility.systemCopyBuffer = lobbyCode.text;
	}
	
	public void PasteClipboardToInput() {
		joinInputCode.text = GUIUtility.systemCopyBuffer;
	}
}