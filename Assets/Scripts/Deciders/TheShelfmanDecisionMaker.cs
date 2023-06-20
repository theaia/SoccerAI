using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TheShelfmanDecisionMaker : MonoBehaviour 
{
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
	private void MakeDecisionAsGoalie() 
	{
		//move to own goal to defend when they have the ball
		if (OpponentHasBall() && IsBallInDefendingZone())
		{
			if (BallCarrierHasLessStamina())
				ApproachToBlock();
			MoveToDefendGoal();
		}

		//if nobody has the ball
		if (!AnyPlayerHasBall() && IsBallInDefendingZone())
		{
			if (IsClosestToBall() && player.GetCurrentStamina() > 50)
			{
				MoveToBall(true);
			}

			else
			{
				MoveToDefendGoal();
			}
        }

		//if this player has the ball
		if (ThisPlayerHasBall())
		{
            TryAttemptPass();
        }

	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker() 
	{
		if (OpponentHasBall())
		{
			MoveToBall(true);
		}

        if (!AnyPlayerHasBall())
        {
            if (IsClosestOnTeamToBall())
                ApproachToBlock();
            else
            {
                if (MoveToBallAnyway())
                {
                    MoveToBall(false);
                }
            }
        }

        if (TeamHasBall() && !ThisPlayerHasBall())
            MoveToBallX();

		if (ThisPlayerHasBall())
		{
			if (player.GetCurrentStamina() < 30)
				TryAttemptPass();
			else
			{
				TryAttemptShot();
			}
		}
    }
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() 
	{
        if (OpponentHasBall())
        {
			if (IsClosestOnTeamToBall())
				ApproachToBlock();
			else
			{
                if (MoveToBallAnyway())
                {
                    MoveToBall(false);
                }
            }
        }
		else if (!AnyPlayerHasBall())
		{
            if (IsClosestOnTeamToBall())
                ApproachToBlock();
            else
            {
                if (MoveToBallAnyway())
                {
                    MoveToBall(false);
                }
            }
        }

		if (TeammateHasBall())
		{
			GoToPassingLocation();
        }

		if (ThisPlayerHasBall())
            TryAttemptPass();
    }
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker() 
	{
        if (OpponentHasBall())
        {
            MoveToBall(false);
        }

        if (!AnyPlayerHasBall())
        {
            if (IsClosestOnTeamToBall())
                ApproachToBlock();
            else
            {
                if (MoveToBallAnyway())
                {
                    MoveToBall(false);
                }
            }
        }

		if (ThisPlayerHasBall())
		{
			TryAttemptPass();
		}

        if (TeammateHasBall())
        {
            GoToPassingLocation();
        }
    }
	#endregion


	//Custom methods

	bool IsGoalieClosest()
	{
        List<Player> playersOnTeamPosition = FindObjectsByType<Player>(FindObjectsSortMode.None).ToList().FindAll(x => x.GetTeam() == player.GetTeam());
        bool goalieClosest = false;
        foreach (Player p in playersOnTeamPosition)
        {
            float myDistanceToBall = Vector2.Distance(transform.position, GameManager.Instance.GetBallLocation());

            if (Vector2.Distance(p.transform.position, GameManager.Instance.GetBallLocation()) < myDistanceToBall && p.GetRole() == Role.Goalie)
                goalieClosest = true;
        }

		return goalieClosest;
    }

	bool MoveToBallAnyway()
	{
        bool moveToBallAnyway = false;

        if (IsGoalieClosest())
        {
            if (Random.Range(0, 2) == 1)
                moveToBallAnyway = true;
        }

		return moveToBallAnyway;
    }

	void MoveToBallX()
	{
        aiaStar.SetTarget(new Vector2(GameManager.Instance.GetBallLocation().y, transform.position.y));
    }

	//Defensive
	void MoveToDefendGoal()
	{
		float minYPos = -0.125f;
		float maxYPos = 0.125f;
		//player.SetSprint(true);
		float xPos = player.GetTeam() == Team.Away ? 1.375f : -1.375f;
		float yPos = GameManager.Instance.GetBallLocation().y;
		if (yPos < minYPos)
			yPos = minYPos;
		if (yPos > maxYPos)
			yPos = maxYPos;
        aiaStar.SetTarget(new Vector2(xPos, yPos));
    }

	void ApproachToBlock()
	{
		player.SetSprint(false);
        aiaStar.SetTarget(GameManager.Instance.GetBallLocation());

		//if within tackling distance, tackle
		if (Vector2.Distance(transform.position, GameManager.Instance.GetBallLocation()) <= 0.65f)
			player.SetAbility(true);
    }

	void MoveToBall(bool sprint)
	{
		player.SetSprint(sprint);
        aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
    }


    //Offensive
    private void TryAttemptKick()
    {
        int _newDir = Utils.GetDirWithoutTags(player, localPerception, new string[] { "stadium", Utils.GetOpposingPlayerTag(player) }, DirectionType.ForwardPreferred);
        if (_newDir == -1) _newDir = Utils.GetForwardDirPerTeam(player.GetTeam());
        AttemptKick(_newDir, GameManager.Instance.maxChargeTime * .5f);
    }

    private void TryAttemptShot()
    {
        //Check if the player has a clear shot on goal;
        int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetOpponentGoalTag(player) }, DirectionType.All);

        //If there is no clear shot on goal, move in a new dir.
        if (_newDir == -1)
        {
            string[] _tag = new string[] { Utils.GetOpposingPlayerTag(player), "stadium" };

            int _dirToMove = Utils.GetDirWithoutTags(player, localPerception, _tag, DirectionType.ForwardPreferredNeutral);
            player.SetMovement(Utils.IntDirToInput(_dirToMove));
            return;
        }

        TryAttemptKick();
    }

    private void TryAttemptPass()
    {
        //Check if the player has a clear shot on goal;
        DirectionType _dirType = IsBallInAttackingZone() ? DirectionType.All : DirectionType.ForwardPreferredNeutral;
        int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetTeamPlayerTag(player) }, _dirType);

        //If there is no clear pass, try to take a shot
        if (_newDir == -1)
        {
            TryAttemptShot();
        }

        TryAttemptKick();
    }

    private void AttemptKick(int _dir, float _minCharge)
    {
        //Debug.Log("Attempting Kick");
        if (player.GetShotCharge() >= _minCharge)
        {
            KickInDir(_dir);
        }

    }


    private void KickInDir(int _dir)
    {
        //Debug.Log($"Attempting to kick in {_dir}");
        player.SetMovement(Utils.IntDirToInput(_dir));
        player.SetAbility(false);
    }

    private void GoToPassingLocation()
    {
        Vector2 _arenaLeftMostFormation = new Vector2(-1.5f, player.GetFormationLocation().y);
        Vector2 _arenaDir = Vector2.right;
        Vector2 _carrierLocation = ballCarrier ? ballCarrier.transform.position : GameManager.Instance.GetBallLocation();
        Vector2 _forwardDirFromBall = Utils.IntDirToInput(Utils.GetForwardAngleFromBall(player, player.GetFormationLocation().y));
        Vector2? target = Utils.Intersection(_arenaLeftMostFormation, _arenaDir, _carrierLocation, _forwardDirFromBall);
        if (target.HasValue)
        {
            //Debug.Log($"Going to passing location {target.Value}");
            Vector2 _clampedPos = Utils.ClampedArenaPos(target.Value, .4f);
            //DebugExtension.DebugPoint(_clampedPos, .5f, .1f);
            aiaStar.SetTarget(_clampedPos);
        }
    }


    //Default methods

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
