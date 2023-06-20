using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcosoDecisionMaker : MonoBehaviour
{
	public float decisionRate = .1f;
	Player player;
	AIAstar aiaStar;
	bool away = false;

	private string[] localPerception;
	private Player ballCarrier;

	private void Awake()
	{
		player = GetComponent<Player>();
		aiaStar = GetComponent<AIAstar>();
	}

	private Vector2 hiAIA;
	private void Start()
	{
		hiAIA = Random.insideUnitCircle * 0.1f;
		if(player.GetTeam() == Team.Away)
        {
			away = true;
        }
		Mathf.Abs(hiAIA.x);
		StopAllCoroutines();
		StartCoroutine(MakeDecision());
	}

    IEnumerator MakeDecision()
	{
		while (true)
		{
			bool _canDecide = GameManager.Instance && player && GameManager.Instance.GetCanMove() && !GameManager.Instance.GetIsTransitioning();
			if (_canDecide)
			{
				localPerception = player.GetLocalPerception();
				ballCarrier = GameManager.Instance.GetBallCarrier();
				switch (player.GetRole())
				{
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
				//Debug.Log($"{player.name} making decisions as {player.GetRole()}. Is transitioning: {GameManager.Instance.GetIsTransitioning()}");
			}
			yield return new WaitForSeconds(decisionRate);
		}

	}


	#region Goalie
	private void MakeDecisionAsGoalie()
	{
		TryGetBall();
		//Goalie has ball
		if (ThisPlayerHasBall())
		{
			player.SetAbility(true); //charge shot?? idk just get the ball away from the goal pls
			MoveForwardAvoidingOpponents();
			return;
		}
		else
		{
			GeneralGoaliePositioning();
			return;
		}
	}

	private void GeneralGoaliePositioning()
	{
		int s = 1;
		player.SetSprint(false);
		Vector2[] _postsToDefend = GameManager.Instance.GetPosts(player.GetTeam());
		Vector2 _defendPos = Utils.V2CenterPoint(_postsToDefend[0], _postsToDefend[1]);
		float differenceFromBallYPos = transform.position.y - GameManager.Instance.GetBallLocation().y;
		if(away == true)
        {
			s = -1;
        }
		aiaStar.SetTarget(_defendPos + new Vector2(Random.Range(0.1f, 0.16f) * s, Random.Range(-0.13f, 0.13f) + differenceFromBallYPos * -0.5f) + hiAIA);
	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker()
	{
		TryGetBall(true);

		if (OpponentHasBall() || IsBallLoose())
		{
			MoveToBall();
			return;
		}
		else
		{
			if (ThisPlayerHasBall())
			{
				player.SetSprint(player.GetCurrentStamina() > GameManager.Instance.maxStamina / 2);
				TryAttemptShot();
				return;
			}
			else
			{
				GoToPassingLocation();
				return;
			}

		}
	}
	#endregion

	#region Defender
	private void MakeDecisionAsDefender()
	{
		TryGetBall(true);

		//Player is closest on team to loose ball or opponent with ball
		if ((IsBallLoose() && IsClosestOnTeamToBall()) || OpponentHasBall())
		{
			MoveToBall();
			return;
		}

		//When ball is loose in own zone
		if (IsBallInDefendingZone() && !TeamHasBall())
		{
			MoveToBall();
			return;
		}
		else if (!ThisPlayerHasBall())
		{
			MoveToFormation();
			return;
		}

		//Has ball
		if (ThisPlayerHasBall())
		{
			player.SetAbility(true); //Charge shot
			MoveForwardAvoidingOpponents();
			return;
		}

		//Team has ball
		if (TeammateHasBall())
		{
			MoveToFormation();
			return;
		}

	}
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker()
	{

		TryGetBall(true);

		//Player is closest on team to loose ball or opponent with ball
		if (!TeamHasBall() && IsClosestOnTeamToBall())
		{
			MoveToBall();
			return;
		}

		if (ThisPlayerHasBall())
		{
			TryAttemptPass();
			return;
		}

		if (TeammateHasBall() || !TeamHasBall())
		{
			GoToPassingLocation();
			return;
		}

		TryGetBall();
	}
	#endregion

	private void TryGetBall(bool _staminaCheck = false)
	{
		//Debug.Log($"{player.name} is trying to get ball");
		if (player.GetIsBallNearby())
		{
			Player _ballCarrier = GameManager.Instance.GetBallCarrier();

			//Attempt to take the ball from the opposing player or a freeball
			if (IsBallLoose() || OpponentHasBall())
			{
				if (_staminaCheck && BallCarrierHasMoreStamina())
				{
					return;
				}
				player.SetAbility(true);
				player.SetMovement(Utils.IntDirToInput(Utils.GetForwardDirPerTeam(player.GetTeam()))); //Do this so that ball doesn't end up in own net
			}
		}
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

	private void MoveToBlockBall()
	{
		aiaStar.SetTarget(new Vector2(player.transform.position.x, GameManager.Instance.GetBallLocation().y));
	}

	private void MoveToBall()
	{
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}

	private void MoveToFormation()
	{
		aiaStar.SetTarget(player.GetFormationLocation());
	}

	private void SprintToBall()
	{
		player.SetSprint(true);
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}

	private void MoveForwardAvoidingOpponents()
	{
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
		if (_currentDirIsClearOfOpponentOrWall)
		{
			List<int> _avoidDirs = Utils.GetDirs(player, DirectionType.BackwardOnly);
			bool _currentDirIsToBeAvoided = false;

			foreach (int _avoidDir in _avoidDirs)
			{
				if (_dir == _avoidDir || _dir == -1)
				{
					_currentDirIsToBeAvoided = true;
					break;
				}
			}

			if (_currentDirIsToBeAvoided == false)
			{
				//Utils.DebugDir(_dir, transform.position);
				player.SetMovement(_currentMoveInput);
				return;
			}
			else
			{
				TryAttemptKick();
			}

		}
		else
		{
			TryAttemptKick();
		}



	}

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

		AttemptKick(_newDir, GameManager.Instance.maxChargeTime * .5f);
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

		AttemptKick(_newDir, GameManager.Instance.maxChargeTime * .5f);
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

	private bool TeammateHasBall()
	{
		return TeamHasBall() && !ThisPlayerHasBall();
	}

	private bool ThisPlayerHasBall()
	{
		return ballCarrier == player;
	}

	private bool AnyPlayerHasBall()
	{
		return ballCarrier != null;
	}

	private bool IsBallLoose()
	{
		return ballCarrier == null;
	}

	private bool TeamHasBall()
	{
		return AnyPlayerHasBall() && ballCarrier.GetTeam() == player.GetTeam();
	}


	private bool OpponentHasBall()
	{
		return AnyPlayerHasBall() && ballCarrier.GetTeam() != player.GetTeam();
	}

	private bool BallCarrierHasLessStamina()
	{
		if (ballCarrier == null)
		{
			return true;
		}
		return ballCarrier.GetCurrentStamina() < player.GetCurrentStamina();
	}

	private bool BallCarrierHasMoreStamina()
	{
		if (ballCarrier == null)
		{
			return false;
		}
		return ballCarrier.GetCurrentStamina() > player.GetCurrentStamina();
	}

	private bool IsClosestOnTeamToBall()
	{
		return Utils.GetPlayerNearestBall(player.GetTeam()) == player;
	}

	private bool IsClosestToBall()
	{
		return Utils.GetPlayerNearestBall() == player;
	}

	private bool IsBallHeadedTowardsTeamGoal()
	{
		return GameManager.Instance.IsBallHeadedTowardsGoal(player.GetTeam());
	}

	private bool IsBallInAttackingZone()
	{
		if (player.GetTeam() == Team.Home)
		{
			return GameManager.Instance.GetBallLocation().x > 0;
		}
		else
		{
			return GameManager.Instance.GetBallLocation().x < 0;
		}
	}

	private bool IsBallInDefendingZone()
	{
		if (player.GetTeam() == Team.Home)
		{
			return GameManager.Instance.GetBallLocation().x < 0;
		}
		else
		{
			return GameManager.Instance.GetBallLocation().x > 0;
		}
	}
}
