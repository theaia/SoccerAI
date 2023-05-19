using System.Collections;
using UnityEngine;

public class DecisionTemplate : MonoBehaviour {
	public float decisionRate = .1f;
	Player player;
	AIAstar aiaStar;

	private string[] localPerception;
	private Player ballCarrier;

	private void Awake() {
		player = GetComponent<Player>();
		aiaStar = GetComponent<AIAstar>();
	}

	private void Start() {
		StopAllCoroutines();
		StartCoroutine(MakeDecision());
	}

	IEnumerator MakeDecision() {
		while (true) {
			bool _canDecide = GameManager.Instance && player && GameManager.Instance.GetCanMove() && !GameManager.Instance.GetIsTransitioning();
			if (_canDecide) {
				localPerception = player.GetLocalPerception();
				ballCarrier = GameManager.Instance.GetBallCarrier();
				switch (player.GetRole()) {
					default:
					case Role.Defender:
						MakeDecisionAsDefender();
						break;
					case Role.Playmaker:
						MakeDecisionAsPlaymaker();
						break;
					case Role.Striker:
						MakeDecisionAsStriker();
						break;
					case Role.Goalie:
						MakeDecisionAsGoalie();
						break;
				}
			}
			yield return new WaitForSeconds(decisionRate);
		}

	}


	#region Goalie
	private void MakeDecisionAsGoalie() {

	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker() {

	}
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() {

	}
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker() {

	}
	#endregion

	private bool TeammateHasBall() {
		return TeamHasBall() && !ThisPlayerHasBall();
	}

	private bool ThisPlayerHasBall() {
		return ballCarrier == player;
	}

	private bool AnyPlayerHasBall() {
		return ballCarrier != null;
	}

	private bool IsBallLoose() {
		return ballCarrier == null;
	}

	private bool TeamHasBall() {
		return AnyPlayerHasBall() && ballCarrier.GetTeam() == player.GetTeam();
	}


	private bool OpponentHasBall() {
		return AnyPlayerHasBall() && ballCarrier.GetTeam() != player.GetTeam();
	}

	private bool BallCarrierHasLessStamina() {
		if (ballCarrier == null) {
			return true;
		}
		return ballCarrier.GetCurrentStamina() < player.GetCurrentStamina();
	}

	private bool BallCarrierHasMoreStamina() {
		if (ballCarrier == null) {
			return false;
		}
		return ballCarrier.GetCurrentStamina() > player.GetCurrentStamina();
	}

	private bool IsClosestOnTeamToBall() {
		return Utils.GetPlayerNearestBall(player.GetTeam()) == player;
	}

	private bool IsClosestToBall() {
		return Utils.GetPlayerNearestBall() == player;
	}

	private bool IsBallHeadedTowardsTeamGoal() {
		return GameManager.Instance.IsBallHeadedTowardsGoal(player.GetTeam());
	}

	private bool IsBallInAttackingZone() {
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x > 0;
		} else {
			return GameManager.Instance.GetBallLocation().x < 0;
		}
	}

	private bool IsBallInDefendingZone() {
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x < 0;
		} else {
			return GameManager.Instance.GetBallLocation().x > 0;
		}
	}
}
