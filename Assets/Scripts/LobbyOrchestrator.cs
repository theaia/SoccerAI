using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LobbyOrchestrator : NetworkBehaviour {
	[SerializeField] TMP_InputField joinInputCode;
	[SerializeField] TextMeshProUGUI lobbyCode;

	[SerializeField] GameObject createScreen;
	[SerializeField] GameObject roomScreen;
	
	private string currentLobbyId;
	[SerializeField] private bool serverAuth;
	private PlayerLobbyNetworkData playerData;

	private void Awake() {
		playerData = new PlayerLobbyNetworkData(){};
	}

	private void Start() {
		UpdatePlayerName("AIA" + Random.Range(0,99));


		createScreen.SetActive(true);
		roomScreen.SetActive(false);

		CreateLobbyScreen.LobbyCreated += CreateLobby;
		RoomScreen.LobbyLeft += OnLobbyLeft;
		RoomScreen.StartPressed += OnGameStart;

		NetworkObject.DestroyWithScene = false;
	}

	#region Create

	private async void CreateLobby(LobbyData _data) {
		using (new Load("Creating Lobby...")) {
			try {
				/*CreateLobbyOptions _options = new CreateLobbyOptions {
					Player = GetThisPlayer()
				};*/

				Lobby _lobby = await LobbyService.Instance.CreateLobbyAsync(_data.Name, _data.MaxPlayers);
				
				createScreen.gameObject.SetActive(false);
				roomScreen.gameObject.SetActive(true);

				currentLobbyId = _lobby.Id;
				
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
				
				/*JoinLobbyByCodeOptions _options = new JoinLobbyByCodeOptions {
					Player = GetThisPlayer()
				};*/
				
				Lobby _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinInputCode.text);
				//await MatchmakingService.JoinLobbyWithAllocation(_lobby.Id);

				createScreen.gameObject.SetActive(false);
				roomScreen.gameObject.SetActive(true);
				
				currentLobbyId = _lobby.Id;
				
				UpdateLobbyCode(_lobby.LobbyCode);
				// Starting the host immediately will keep the relay server alive
				NetworkManager.Singleton.StartClient();
				StartCoroutine(SendNameWhenConnected(GetRawUsername()));
			} catch (Exception _e) {
				Debug.LogError(_e);
				CanvasUtilities.Instance.ShowError("Failed joining lobby");
			}
		}
	}

	#endregion
	
	#region Room

	public static readonly Dictionary<ulong, PlayerLobbyNetworkData> PlayersInLobby = new();
	public static event Action<Dictionary<ulong, PlayerLobbyNetworkData>> LobbyPlayersUpdated;
	private float nextLobbyUpdate;

	private int TeamPlacement() {
		int _homePlayers = 0;
		int _awayPlayers = 0;
		foreach(PlayerLobbyNetworkData _player in PlayersInLobby.Values)
		{
			if (_player.Team == 1) {
				_homePlayers++;
			}
			if (_player.Team == 2) {
				_awayPlayers++;
			}
		}

		return _awayPlayers < _homePlayers ? 2 : 1;

	}
	
	public override void OnNetworkSpawn() {
		if (IsServer) {
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
			PlayersInLobby.Add(NetworkManager.Singleton.LocalClientId, new PlayerLobbyNetworkData() {
				PlayerName = GetRawUsername(),
				IsReady = false,
				Team = TeamPlacement()
			});
			UpdateInterface();
		}

		// Client uses this in case host destroys the lobby
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
	}

	private void OnClientConnectedCallback(ulong _playerId) {
		if (!IsServer) return;
		Debug.Log($"Server detected new player with id {_playerId} connected");
		// Add locally
		if (!PlayersInLobby.ContainsKey(_playerId)) {
			PlayersInLobby.Add(_playerId, new PlayerLobbyNetworkData() {
				Team = TeamPlacement()
			});
			Debug.Log($"Player {_playerId} added to server lobby list");
		}

		PropagateToClients();
		//UpdateInterface();
	}

	private void PropagateToClients() {
		foreach (var _player in PlayersInLobby) UpdatePlayerClientRpc(_player.Key, _player.Value);
	}

	[ClientRpc]
	private void UpdatePlayerClientRpc(ulong _clientId, PlayerLobbyNetworkData _lobbyNetworkData) {
		if (IsServer) return;

		if (!PlayersInLobby.ContainsKey(_clientId)) {
			PlayersInLobby.Add(_clientId, _lobbyNetworkData);
			Debug.Log($"Adding new player {_clientId} to client lobby");
		} else {
			Debug.Log($"Updating existing player {_clientId} in client lobby");
			PlayersInLobby[_clientId] = _lobbyNetworkData;
		}
		UpdateInterface();
	}

	private void OnClientDisconnectCallback(ulong _playerId) {
		if (IsServer) {
			// Handle locally
			if (PlayersInLobby.ContainsKey(_playerId)) PlayersInLobby.Remove(_playerId);

			// Propagate all clients
			RemovePlayerClientRpc(_playerId);

			UpdateInterface();
		} else {
			// This happens when the host disconnects the lobby
			roomScreen.gameObject.SetActive(false);
			createScreen.gameObject.SetActive(true);
			OnLobbyLeft();
		}
	}

	[ClientRpc]
	private void RemovePlayerClientRpc(ulong _clientId) {
		if (IsServer) return;

		if (PlayersInLobby.ContainsKey(_clientId)) PlayersInLobby.Remove(_clientId);
		UpdateInterface();
	}

	public void OnReadyClicked() {
		ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
	}

	IEnumerator SendNameWhenConnected(string _value) {
		yield return new WaitUntil(() => NetworkManager.Singleton.IsConnectedClient);
		UpdatePlayerNameServerRpc(NetworkManager.Singleton.LocalClientId, _value);
	}

	[ServerRpc(RequireOwnership = false)]
	private void UpdatePlayerNameServerRpc(ulong _playerId, string _value) {
		var _playerLobbyNetworkData = PlayersInLobby[_playerId];
		_playerLobbyNetworkData.PlayerName = _value;
		PlayersInLobby[_playerId] = _playerLobbyNetworkData;
		
		Debug.Log($"Player {_playerId} set name to {PlayersInLobby[_playerId].PlayerName}");
		PropagateToClients();
		UpdateInterface();
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void ToggleReadyServerRpc(ulong _playerId) {
		var _playerLobbyNetworkData = PlayersInLobby[_playerId];
		_playerLobbyNetworkData.IsReady = !_playerLobbyNetworkData.IsReady;
		PlayersInLobby[_playerId] = _playerLobbyNetworkData;
		Debug.Log($"Player {_playerId} named {PlayersInLobby[_playerId].PlayerName} set to {_playerLobbyNetworkData.IsReady}");
		PropagateToClients();
		UpdateInterface();
	}

	private void UpdateInterface() {
		LobbyPlayersUpdated?.Invoke(PlayersInLobby);
	}

	private async void OnLobbyLeft() {
		using (new Load("Leaving Lobby...")) {
			PlayersInLobby.Clear();
			NetworkManager.Singleton.Shutdown();
			//await MatchmakingService.LeaveLobby();
			await LobbyService.Instance.RemovePlayerAsync(currentLobbyId, AuthenticationService.Instance.PlayerId);
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

	private Unity.Services.Lobbies.Models.Player GetThisPlayer() {
		return new Unity.Services.Lobbies.Models.Player {
			Data = new Dictionary<string, PlayerDataObject>() {
				{ "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetRawUsername()) },
				{ "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") },
				{ "Team", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "Home") },
			}
		};
	}

	private string GetRawUsername() {
		string _username = AuthenticationService.Instance.PlayerName;
		int _hashIndex = _username.IndexOf('#');
		if (_hashIndex != -1) {
			_username = _username.Substring(0, _hashIndex);
		}
		_username = _username.Substring(0, Mathf.Min(_username.Length, 11));
		return _username;
	}
	
	public struct PlayerLobbyNetworkData : INetworkSerializable {
		private ulong id;
		private FixedString32Bytes playerName;
		private bool isReady;
		private int team;
	
		internal ulong Id {
			get => id;
			set => id = value;
		}

		internal string PlayerName {
			get => playerName.ToString();
			set => playerName = (FixedString32Bytes)value;
		}
		
		internal bool IsReady {
			get => isReady;
			set => isReady = value;
		}
		
		internal int Team {
			get => team;
			set => team = value;
		}
	
		public void NetworkSerialize<T>(BufferSerializer<T> _serializer) where T : IReaderWriter {
			_serializer.SerializeValue(ref id);
			_serializer.SerializeValue(ref playerName);
			_serializer.SerializeValue(ref isReady);
			_serializer.SerializeValue(ref team);
		}
	}
}



