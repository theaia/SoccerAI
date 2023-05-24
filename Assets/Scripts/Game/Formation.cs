using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Formation : MonoBehaviour
{
	[SerializeField] KickoffType kickoffType;
	private FormationSpot[] spotList;

	private void Awake() {
		spotList = GetComponentsInChildren<FormationSpot>();
	}

	public FormationSpot[] GetSpotList() {
		return spotList;
	}

	public Vector2 GetFormationLocation(ulong _playerId) {
		Player _currentPlayer = GameManager.NetworkedPlayerList[_playerId];
		List<Player> _players = GameManager.Instance.GetPlayers(_currentPlayer.GetTeam());
		int _playerIndex = _players.IndexOf(_currentPlayer);
		//Debug.Log($"Player {_playerId} at index {_playerIndex} for team");
		if (spotList == null) {
			//Debug.Log($"spotlist is null.  populating.");
			spotList = GetComponentsInChildren<FormationSpot>();
		}
		Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[_playerIndex].transform.position, _currentPlayer.GetTeam());
		return _location;
	}
	
	
	public void SetFormation(Team _team) {
		GameManager.Instance.SetIsTransitioning(true);
		if (!GameManager.Instance.GetIsServer()) {
			return;
		}
		List<Player> _players = GameManager.Instance.GetPlayers(_team);
		
		for(int i = 0; i < spotList.Length; i++) {
			if (_players.Count > i) {
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				//Debug.Log($"{_players[i].name} on {_players[i].GetTeam()} having formation set to {_location}");
				_players[i].SetFormationLocation(_location);
				_players[i].GetAIAStar().SetTarget(_location);
				if(GameManager.Instance.GetGameMode() == GameMode.SinglePlayer) _players[i].SetRole(spotList[i].Role);
			} else if (GameManager.Instance.GetGameMode() == GameMode.SinglePlayer){
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				Player _newPlayer = Instantiate(GameManager.Instance.GetPlayerPrefab(_team), Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team), Quaternion.identity, null).GetComponent<Player>();
				_newPlayer.SetFormationLocation(_location);
				_newPlayer.SetTeam(_team);
				_newPlayer.SetRole(spotList[i].Role);

				GameManager.Instance.AddPlayer(_newPlayer, _team);
			}
		}

		if (_team == Team.Away) {
			Debug.Log("Set formation has finished. Propping to clients");
			GameManager.Instance.PropogateFormationToClients();
		}

		; //Team Away happens second.  So after this happens send the updated locations to the clients.

	}

	
	
	public KickoffType GetKickoffType() {
		return kickoffType;
	}


}


public enum KickoffType {
	Attacking,
	Defending
}
