using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VAVDecisionMaker : MonoBehaviour
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
	private void MakeDecisionAsGoalie() {
		
		if(OpponentHasBall() && IsBallInDefendingZone())
		{
			
			if(!isBallInGoalieZone(player.GetTeam()))
			{
				TryGetBall();
				GeneralGoaliePositioning();
			}
			else
			{
				TryGetBall();
				SprintToBall();
			}
			
		}
		else if(IsBallHeadedTowardsTeamGoal())
		{	
			TryGetBall();
			MoveToBlockBall();
		}
		else if (IsBallLoose() && IsClosestToBall() && IsBallInDefendingZone())
		{
			TryGetBall();
			SprintToBall();
		}
		else
		{
			MoveToFormation();
		}


		if (ThisPlayerHasBall())
		{
			if(TryAttemptPass() == -1)
			{
				MoveForwardAvoidingOpponents();
				TryAttemptShot();
			}
			else
			{
				AttemptKick(TryAttemptPass(), GameManager.Instance.maxChargeTime * .1f);
			}

		}
		
	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker()
    {
		//TryGetBall();
		//aiaStar.SetTarget(Utils.IntDirToInput(GetBotDirPerTeam(player.GetTeam())));

		if(TeamHasBall() && !ThisPlayerHasBall())
		{
			TryGetBall();
			aiaStar.SetTarget(Utils.IntDirToInput(GetBotDirPerTeam(player.GetTeam())));
		}
		else if(TeamHasBall() && ThisPlayerHasBall())
		{
			player.SetSprint(true);
            MoveForwardAvoidingOpponents();
			TryAttemptShot();
		}
		else if(!TeamHasBall() && IsBallInAttackingZone() || IsClosestToBall())
		{
			MoveToBall();
			TryGetBall();
		}
		else
		{
			player.SetSprint(false);
			TryGetBall(true);
			MoveToFormation();
		}
	}
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() {
        
        if(!TeamHasBall()) // if the team does not have the ball
        {
			//Debug.Log("other team has the ball");
            if(IsBallInDefendingZone()) //if the ball is in our zone
		    {	
				//Debug.Log("The ball is in out zone");
				if(!IsBallPassedFormationSpot() || BallPassedThePlayer() || (IsBallLoose() && IsClosestToBall())) // if the ball passed the formation spot
				{
					//Debug.Log("the ball passed " + player.gameObject.name + " Formation zone");
					SprintToBall();
					TryGetBall();
				}
				else // if the ball is between our side and the formation zone
				{
					MoveToBlockBall();
					TryGetBall();
				}	
			}
		    else // if the ball is in opponents side
		    {
				MoveToFormation();
		    }
        }

        else if(TeamHasBall() && !ThisPlayerHasBall())
        {
            MoveToFormation();
        }

        if (ThisPlayerHasBall()) {

			MoveForwardAvoidingOpponents();
			player.SetSprint(player.GetCurrentStamina() > GameManager.Instance.maxStamina / 2);

			if(IsBallInAttackingZone())
			{
				if(TryAttemptPass() == -1)
				{
					MoveForwardAvoidingOpponents();
					TryAttemptShot();
				}
				else
				{
					AttemptKick(TryAttemptPass(), GameManager.Instance.maxChargeTime * .5f);
				}
			}
		}
	}
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker() {
		//Debug.Log(isBallInTopPart());

		if(!TeamHasBall() || (IsBallLoose()))
		{
			MoveToBall();
			TryGetBall();
		}

		else if(!ThisPlayerHasBall())
		{
			player.SetSprint(player.GetCurrentStamina() > GameManager.Instance.maxStamina * 0.7f);
				GoToPlayMakerPassingPosition();
				//aiaStar.SetTarget(Utils.IntDirToInput(GetBotDirPerTeam(player.GetTeam())));
		}

		if (ThisPlayerHasBall()) {
			MoveForwardAvoidingOpponents();
			player.SetSprint(true);

			if(isBallInOpponentGoalieZone(player.GetTeam()))
			{
				if(CheckIfSeesGoal() != -1)
				{
					TryAttemptShot();
				}
				else
				{
					if(TryAttemptPass() == -1)
					{
						MoveForwardAvoidingOpponents();
						TryAttemptShot();
					}
					else
					{
						AttemptKick(TryAttemptPass(), GameManager.Instance.maxChargeTime * .1f);
					}
				}
			}
			
			return;
		}
	}
	#endregion


    
    private void TryGetBall(bool _staminaCheck = false) {
		//Debug.Log($"{player.name} is trying to get ball");
		if (player.GetIsBallNearby()) {
			Player _ballCarrier = GameManager.Instance.GetBallCarrier();

			//Attempt to take the ball from the opposing player or a freeball
			if (IsBallLoose() || OpponentHasBall()) {
				if(_staminaCheck && BallCarrierHasMoreStamina()) {
					return;
				}
				player.SetAbility(true);
				player.SetMovement(Utils.IntDirToInput(Utils.GetForwardDirPerTeam(player.GetTeam()))); //Do this so that ball doesn't end up in own net
			}
		}
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

    private int TryAttemptPass() {
		//Check if the player has a clear shot on goal;
		DirectionType _dirType = DirectionType.All;
		int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetTeamPlayerTag(player) }, _dirType);

		return _newDir;		
	}

	private int CheckIfSeesGoal() {
		//Check if the player has a clear shot on goal;
		DirectionType _dirType = IsBallInAttackingZone() ? DirectionType.All : DirectionType.ForwardPreferredNeutral;
		int _newDir = Utils.GetDirWithTags(player, localPerception, new string[] { Utils.GetOpponentGoalTag(player) }, _dirType);
		//If newDir is -1 , means he doesn't see the goal
		return _newDir;
	}

    private void AttemptKick(int _dir, float _minCharge) {
		//Debug.Log("Attempting Kick");
		if (player.GetShotCharge() >= _minCharge) {
			KickInDir(_dir);
		}
	}

	private void SprintToBall() {
		player.SetSprint(true);
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}

	private void KickInDir(int _dir) {
		//Debug.Log($"Attempting to kick in {_dir}");
		player.SetMovement(Utils.IntDirToInput(_dir));
		player.SetAbility(false);
	}

    private void MoveToBlockBall() {
       // Debug.Log("MoveToBlockBall doing");
		aiaStar.SetTarget(new Vector2(player.transform.position.x, GameManager.Instance.GetBallLocation().y));
	}

    private void MoveToFormation() {
		player.SetSprint(false);
		aiaStar.SetTarget(player.GetFormationLocation());
	}


    private void MoveToBall() {
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}
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

	private bool isBallInTopPart()
	{
		return GameManager.Instance.GetBallLocation().y > 0;
	}
	private bool IsBallPassedFormationSpot()
	{
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x > player.GetFormationLocation().x;
		} else {
			return GameManager.Instance.GetBallLocation().x < player.GetFormationLocation().x;
		}
	}

	private bool BallPassedThePlayer()
	{
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x < player.transform.position.x;
		} else {
			return GameManager.Instance.GetBallLocation().x > player.transform.position.x;
		}
	}
	private bool IsBallInAttackingZone() {
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x > 0;
		} else {
			return GameManager.Instance.GetBallLocation().x < 0;
		}
	}

	private void GeneralGoaliePositioning() {
		player.SetSprint(false);
		Vector2[] _postsToDefend = GameManager.Instance.GetPosts(player.GetTeam());
		Vector2 _defendPos = Utils.V3CenterPoint(_postsToDefend[0], _postsToDefend[1], GameManager.Instance.GetBallLocation());
		aiaStar.SetTarget(_defendPos);
	}

	private void GoToPlayMakerPassingPosition()
	{
		Vector2 _passPos;
		if(isBallInTopPart())
		{
			_passPos = Utils.V3CenterPoint(Utils.IntDirToInput(GetBotDirPerTeam(player.GetTeam())), GameManager.Instance.GetBallLocation(),  GetOpponentGoalCenterPos(player.GetTeam()));
		}
		else
		{
			_passPos = Utils.V3CenterPoint(Utils.IntDirToInput(GetTopDirPerTeam(player.GetTeam())), GameManager.Instance.GetBallLocation(), GetOpponentGoalCenterPos(player.GetTeam()));
		}

		aiaStar.SetTarget(_passPos);
	}

	private bool IsBallInDefendingZone() {
		if (player.GetTeam() == Team.Home) {
			return GameManager.Instance.GetBallLocation().x < 0;
		} else {
			return GameManager.Instance.GetBallLocation().x > 0;
		}
	}

	public int GetTopDirPerTeam(Team _team) {
        if (_team == Team.Home) {
            return 1;
        } else {
            return 7;
        }
    }

	public int GetBotDirPerTeam(Team _team) {
        if (_team == Team.Home) {
            return 3;
        } else {
            return 5;
        }
    }

	private void MoveForwardAvoidingOpponents() {
		//Debug.Log($"{gameObject.name} avoiding opponents");
		aiaStar.CancelCurrentPath();
		Vector2 _currentMoveInput = player.GetCurrentMoveInput();
		//Debug.Log($"Avoiding opponent have movement {_currentMoveInput}");
		Vector2 _origin = transform.position;
		int _dir = Utils.InputToDir(_currentMoveInput);
		bool _currentDirIsClearOfOpponentOrWall = Utils.CheckDirClearOfTags(
			localPerception,
			new string[] { "stadium", Utils.GetOpposingPlayerTag(player) },
			_dir
		);
		if (_currentDirIsClearOfOpponentOrWall) {
			List<int> _avoidDirs = Utils.GetDirs(player, DirectionType.BackwardOnly);
			bool _currentDirIsToBeAvoided = false;

			foreach(int _avoidDir in _avoidDirs) {
				if(_dir == _avoidDir || _dir == -1) {
					_currentDirIsToBeAvoided = true;
					break;
				}
			}

			if (_currentDirIsToBeAvoided == false) {
				//Utils.DebugDir(_dir, transform.position);
				player.SetMovement(_currentMoveInput);
				return;
			} else {
				TryAttemptShot();
			}

		} else {
			TryAttemptShot();
		}
	}

	public bool isBallInGoalieZone(Team _team)
	{	
		if (_team == Team.Home) {
            return(GameManager.Instance.GetBallLocation().x < GameManager.Instance.ArenaWidth[0] / 2);
        }
		else {
            return(GameManager.Instance.GetBallLocation().x > GameManager.Instance.ArenaWidth[1] / 2);
        }
	}

	public bool isBallInOpponentGoalieZone(Team _team)
	{	
		if (_team == Team.Away) {
            return(GameManager.Instance.GetBallLocation().x < GameManager.Instance.ArenaWidth[0] * 0.5f);
        }
		else {
            return(GameManager.Instance.GetBallLocation().x > GameManager.Instance.ArenaWidth[1] * 0.5f);
        }
	}

	public Vector2 GetOpponentGoalCenterPos(Team _team)
	{
		if(_team == Team.Home)
		{
			return new Vector2(GameManager.Instance.ArenaWidth[1], 0);
		}
		else
		{
			return new Vector2(GameManager.Instance.ArenaWidth[0], 0);
		}
	}
}
