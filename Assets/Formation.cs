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
				_players[i].transform.position = Utils.GetTeamBasedLocation(spotList[i].transform.position, _team);
				_players[i].SetRole(spotList[i].Role);
				_players[i].Reset();
			} else {
				Player _newPlayer = Instantiate(GameManager.Instance.GetPlayerPrefab(_team), Utils.GetTeamBasedLocation(spotList[i].transform.position, _team), Quaternion.identity, null).GetComponent<Player>();
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
