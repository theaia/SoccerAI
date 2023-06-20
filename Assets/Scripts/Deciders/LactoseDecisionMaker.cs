using System.Collections;
using UnityEngine;

public class LactoseDecisionMaker : MonoBehaviour {
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
    player.SetIsCheering(true);
	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker() {

        
        if(ThisPlayerHasBall())
        {//I hate readable code
                aiaStar.CancelCurrentPath();
		        Vector2 _currentMoveInput = player.GetCurrentMoveInput();
		        Vector2 _origin = transform.position;
		        int _dir = Utils.InputToDir(_currentMoveInput);
		        bool _currentDirIsClearOfOpponentOrWall = Utils.CheckDirClearOfTags(
			    localPerception,
			    new string[] { "stadium", Utils.GetOpposingPlayerTag(player) },
			    _dir
		        );
                GameObject goal = GameObject.FindGameObjectWithTag(Utils.GetOpponentGoalTag(player));
                player.SetMovement(Utils.IntDirToInput(_dir));
                aiaStar.SetTarget(goal.transform.position);
                
        }
        else
        {
            aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
            player.SetAbility(true);
            
        }

        
	}
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() {
        player.SetIsCheering(true);

	}
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker() {
        player.SetIsCheering(true);
    //aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
        // if(ThisPlayerHasBall())
        // {
        //     if(Utils.GetPlayerNearestBall().GetRole() == Role.Striker)
        //     {
        //         player.SetSprint(false);
        //         MoveToSpot(Utils.GetPlayerNearestBall().gameObject.transform.position);
        //         ballCarrier.Giveaway();
        //     }
        //     else
        //     {
        //         if(player.GetCurrentStamina() > GameManager.Instance.maxStamina * 0.1f)
        //         {
        //             player.SetSprint(true);
        //         }
        //         else
        //         {
        //             player.SetSprint(false);
        //         }
        //         int direction = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetOpponentGoalTag(player) }, DirectionType.All);
        //         MoveToSpot(Utils.IntDirToInput(direction));
        //     }
        // }
        // else
        // {
        //     if(player.GetCurrentStamina() > GameManager.Instance.maxStamina * 0.1f)
        //     {
        //        player.SetSprint(true);
        //     }
        //     else
        //     {
        //         player.SetSprint(false);
        //     }
        

        //     MoveToSpot(GameManager.Instance.GetBallLocation());
        // }
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
