using System.Collections.Generic;
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

	public void SetFormation(Team _team) {
		GameManager.Instance.SetIsTransitioning(true);
		List<Player> _players = GameManager.Instance.GetPlayers(_team);
		
		for(int i = 0; i < spotList.Length; i++) {
			if (_players.Count > i) {
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				//Debug.Log($"{_players[i].name} on {_players[i].GetTeam()} having formation set to {_location}");
				_players[i].SetFormationLocation(_location);
				_players[i].GetAIAStar().SetTarget(_location);
				_players[i].SetRole(spotList[i].Role);
			} else {
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				Player _newPlayer = Instantiate(GameManager.Instance.GetPlayerPrefab(_team), Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team), Quaternion.identity, null).GetComponent<Player>();
				_newPlayer.SetFormationLocation(_location);
				_newPlayer.SetTeam(_team);
				_newPlayer.SetRole(spotList[i].Role);

				GameManager.Instance.AddPlayer(_team, _newPlayer);
			}
		}
	}

	public KickoffType GetKickoffType() {
		return kickoffType;
	}


}


public enum KickoffType {
	Attacking,
	Defending
}
