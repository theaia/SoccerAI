using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyMarsDecisionMaker : MonoBehaviour
{
    public float decisionRate = .1f;
	Player player;
	AIAstar aiaStar;

	private string[] localPerception;
	private Player ballCarrier;
    private Vector2 previousBallPos;
	private Vector2 currentBallPos;

    float yOffsetFromStriker = -1;
    float defenseX = -1;

    public static Player PolyMarsStrikerInstance;

    private void Awake() {
		player = GetComponent<Player>();
		aiaStar = GetComponent<AIAstar>();
	}

	private void Start() {
		// Initialize the previous and current ball positions
    	previousBallPos = GameManager.Instance.GetBallLocation();
    	currentBallPos = previousBallPos;        
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
	private void MakeDecisionAsStriker()
	{
        if (PolyMarsStrikerInstance == null)
		{
            PolyMarsStrikerInstance = this.player;
        }
		if (defenseX == -1)
		{
            defenseX = player.transform.position.x;
        }

        // if (IsBallInDefendingZone())
        // {
            player.SetSprint(true);
            //float targetY = Mathf.Clamp(GameManager.Instance.GetBallLocation().y, -.1f, .1f);
            aiaStar.SetTarget(new Vector2(defenseX, GameManager.Instance.GetBallLocation().y));
        // }
		// else
		// {
		// 	player.SetSprint(true);
		// 	player.SetMovement(Utils.IntDirToInput(GetForwardDirPerTeam()));
        // }
    }
	
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() {
        player.SetSprint(true);
        FormPaddle(PolyMarsStrikerInstance);
		if (ThisPlayerHasBall())
		{
            TryAttemptShot();
        }
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

	/*New methods*/

	private void FormPaddle(Player target)
	{
		if (yOffsetFromStriker == -1)
		{
            yOffsetFromStriker = PolyMarsStrikerInstance.transform.position.y - player.transform.position.y;
        }
        aiaStar.SetTarget(new Vector2(PolyMarsStrikerInstance.transform.position.x, PolyMarsStrikerInstance.transform.position.y - yOffsetFromStriker));

    }

	private int GetForwardDirPerTeam()
	{
		if (player.GetTeam() == Team.Home)
		{
			return 1; //positive x-direction is forward for the team
		}
		else
		{
			return -1; //negative x-direction is forward for the team
		}
	}
	
	//Methods from example

	private void KickInDir(int _dir) {
		//Debug.Log($"Attempting to kick in {_dir}");
		player.SetMovement(Utils.IntDirToInput(_dir));
		player.SetAbility(false);
	}

	private void TryAttemptShot() {
		//Check if the player has a clear shot on goal;
		int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetOpponentGoalTag(player) }, DirectionType.All);

		//If there is no clear shot on goal, move in a new dir.
		if (_newDir == -1) {
			string[] _tag = new string[] { Utils.GetOpposingPlayerTag(player), "stadium" };

			int _dirToMove = Utils.GetDirWithoutTags(player, localPerception, _tag, DirectionType.ForwardPreferredNeutral);
			player.SetMovement(Utils.IntDirToInput(_dirToMove));
			return;
		}

		AttemptKick(_newDir, GameManager.Instance.maxChargeTime * .5f);
	}

	private void TryAttemptPass() {
		//Check if the player has a clear shot on goal;
		DirectionType _dirType = IsBallInAttackingZone() ? DirectionType.All : DirectionType.ForwardPreferredNeutral;
		int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetTeamPlayerTag(player) }, _dirType);

		//If there is no clear pass, try to take a shot
		if (_newDir == -1) {
			TryAttemptShot();
		}

		AttemptKick(_newDir, GameManager.Instance.maxChargeTime * .5f);
	}

	private void AttemptKick(int _dir, float _minCharge) {
		//Debug.Log("Attempting Kick");
		if (player.GetShotCharge() >= _minCharge) {
			KickInDir(_dir);
		}

	}

}
