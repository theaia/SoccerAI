using System.Collections;
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
		List<Player> _players = GameManager.Instance.GetPlayers(_team);
		
		for(int i = 0; i < spotList.Length; i++) {
			if (_players.Count > i) {
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				_players[i].SetFormationLocation(_location);
				_players[i].GetAIAStar().SetTarget(_location);
				_players[i].SetRole(spotList[i].Role);
			} else {
				Vector2 _location = Utils.GetDefendingZoneBasedLocation(spotList[i].transform.position, _team);
				Player _newPlayer = Instantiate(GameManager.Instance.GetPlayerPrefab(_team), _location, Quaternion.identity, null).GetComponent<Player>();
				_newPlayer.SetTeam(_team);
				_newPlayer.SetRole(spotList[i].Role);
				_newPlayer.SetFormationLocation(_location);
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
