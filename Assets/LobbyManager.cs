using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
	Lobby currentLobby;
	ILobbyEvents m_LobbyEvents;
	[SerializeField] Button hostBtn;
	[SerializeField] Button joinBtn;
	[SerializeField] TMP_InputField joinInputCode;
	[SerializeField] TextMeshProUGUI lobbyCode;

	[SerializeField] GameObject lobbyCreationMenu;
	[SerializeField] LobbyMenu lobbyMenu;
	private List<LobbyPlayer> homePlayers;
	private List<LobbyPlayer> awayPlayers;

	private string playerName;
	private Team team;
	bool isReady;

	private void Awake() {
		lobbyCode.transform.parent.gameObject.SetActive(false);
		lobbyMenu.gameObject.SetActive(false);

		hostBtn.onClick.AddListener(() => {
			CreateLobby();
		});
		joinBtn.onClick.AddListener(() => {
			JoinLobby();
		});
		;

		playerName = "AIA" + UnityEngine.Random.Range(10, 99);
		Debug.Log(playerName);
	}

	private async void CreateLobby() {
		string lobbyName = "new lobby";
		int maxPlayers = 2;
		CreateLobbyOptions options = new CreateLobbyOptions {
			IsPrivate = false,
			Player = GetNewPlayer()
		};

		Lobby _lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
		OnJoinLobby(_lobby);
	}

	private async void JoinLobby() {
		try {
			JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions {
				Player = GetNewPlayer()
			};
			Lobby _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinInputCode.text, options);
			OnJoinLobby(_lobby);
		} catch (LobbyServiceException e) {
			Debug.Log(e);
			OnJoinLobby();
		}
	}

	private void OnJoinLobby(Lobby _lobby = null) {
		currentLobby = _lobby;
		if (currentLobby != null) {
			lobbyMenu.gameObject.SetActive(true);
			lobbyCreationMenu.SetActive(false);
			lobbyCode.text = currentLobby.LobbyCode;
			lobbyCode.transform.parent.GetChild(0).gameObject.SetActive(true);
			lobbyCode.transform.parent.GetChild(1).gameObject.SetActive(true);
			lobbyCode.transform.parent.gameObject.SetActive(true);
			PrintPlayers(currentLobby);
		} else {
			lobbyCode.text = "Join lobby failed";
			lobbyCode.transform.parent.GetChild(0).gameObject.SetActive(false);
			lobbyCode.transform.parent.GetChild(1).gameObject.SetActive(true);
			lobbyCode.transform.parent.gameObject.SetActive(true);

		}

	}
	public async void LeaveLobby() {
		try {
			await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
			lobbyMenu.gameObject.SetActive(false);
			lobbyCreationMenu.SetActive(true);
			lobbyCode.text = "";
			lobbyCode.transform.parent.GetChild(0).gameObject.SetActive(false);
			lobbyCode.transform.parent.GetChild(1).gameObject.SetActive(false);
			lobbyCode.transform.parent.gameObject.SetActive(false);
		} catch (LobbyServiceException e) {
			Debug.Log(e);
		}
	}

	private async void SubToLobby(Lobby _lobby) {
		var callbacks = new LobbyEventCallbacks();
		callbacks.LobbyChanged += OnLobbyChanged;
		callbacks.KickedFromLobby += OnKickedFromLobby;
		callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
		try {
			m_LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, callbacks);
		} catch (LobbyServiceException ex) {
			switch (ex.Reason) {
				case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{_lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
				case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
				case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
				default: throw;
			}
		}
	}

	private void OnLobbyChanged(ILobbyChanges changes) {
		if (changes.LobbyDeleted) {
			// Handle lobby being deleted
			// Calling changes.ApplyToLobby will log a warning and do nothing
		} else {
			changes.ApplyToLobby(currentLobby);
		}
		// Refresh the UI in some way
		PrintPlayers(currentLobby);
	}

	private void OnKickedFromLobby() {


		// These events will never trigger again, so let’s remove it.
		this.m_LobbyEvents = null;
		// Refresh the UI in some way
	}

	private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state) {
		switch (state) {
			case LobbyEventConnectionState.Unsubscribed: /* Update the UI if necessary, as the subscription has been stopped. */ break;
			case LobbyEventConnectionState.Subscribing: /* Update the UI if necessary, while waiting to be subscribed. */ break;
			case LobbyEventConnectionState.Subscribed: /* Update the UI if necessary, to show subscription is working. */ break;
			case LobbyEventConnectionState.Unsynced: /* Update the UI to show connection problems. Lobby will attempt to reconnect automatically. */ break;
			case LobbyEventConnectionState.Error: /* Update the UI to show the connection has errored. Lobby will not attempt to reconnect as something has gone wrong. */ break;
		}
	}

	public void CopyCode() {
		GUIUtility.systemCopyBuffer = currentLobby.LobbyCode;
	}

	public void PasteClipboardToInput() {
		joinInputCode.text = GUIUtility.systemCopyBuffer;
	}

	private Unity.Services.Lobbies.Models.Player GetNewPlayer() {
		return new Unity.Services.Lobbies.Models.Player {
			Data = new Dictionary<string, PlayerDataObject> {
				{"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
				{"PlayerTeam", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team == Team.Home ? "Home" : "Away") },
				{"PlayerIsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString()) }
			}
		};
	}

	[ContextMenu("Print Players")]
	private void PrintPlayers() {
		PrintPlayers(currentLobby);
	}
	private void PrintPlayers(Lobby lobby) {
		Debug.Log("Players in lobby: " + lobby.Name);
		foreach (var player in lobby.Players) {
			Debug.Log($"{player.Data["PlayerName"].Value} set to {player.Data["PlayerTeam"].Value} and is ready {player.Data["PlayerIsReady"].Value}");
		}
	}

	public async void ToggleIsReady() {
		try {
			isReady = !isReady;
			await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
				Data = new Dictionary<string, PlayerDataObject> {
					{ "PlayerIsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString())  }
				}
			});
		} catch (LobbyServiceException e) {
			Debug.Log(e);
		}
	}

	private LobbyPlayer PlayerToLobbyPlayer(Unity.Services.Lobbies.Models.Player _player) {
		return new LobbyPlayer {
			ID = _player.Id,
			Name = _player.Data["PlayerName"].Value,
			Team = _player.Data["PlayerTeam"].Value == "Home" ? Team.Home : Team.Away,
			IsReady = bool.Parse(_player.Data["PlayerIsReady"].Value)
		};
	}

	private void UpdatePlayerLobbyList(Team _team) {
		for(int i = 0; i < currentLobby.Players.Count; i++) {
			if(currentLobby.Players[i].Data["PlayerTeam"].Value == "Home") {

			}
		}
	}

	private async void UpdateTeam() {
		await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
			Data = new Dictionary<string, PlayerDataObject> {
				{"PlayerTeam", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team == Team.Home ? "Home" : "Away") }
			}
		});
	}
}



public struct LobbyPlayer {
	public string ID;
	public string Name;
	public Team Team;
	public bool IsReady;

}
