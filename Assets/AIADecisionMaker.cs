using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIADecisionMaker : MonoBehaviour
{
	public float decisionRate = .1f;
	Player player;
	AIAstar aiaStar;

	private bool isClearing;
	private string[] localPerception;

	private void Awake() {
		player = GetComponent<Player>();
		aiaStar = GetComponent<AIAstar>();
	}

	IEnumerator Start() {
		yield return new WaitForSeconds(1f);
		StartCoroutine(MakeDecision());
	}

	IEnumerator MakeDecision() {
		while (true) {
			if (!GameManager.Instance || !player) {
				yield return null;
			}
			localPerception = player.GetLocalPerception();
			switch (player.GetRole()) {
				default:
				case PlayStyle.Defender:
					MakeDecisionAsDefender();
					break;
				case PlayStyle.Playmaker:
					MakeDecisionAsPlaymaker();
					break;
				case PlayStyle.Striker:
					MakeDecisionAsStriker();
					break;
				case PlayStyle.Goalie:
					MakeDecisionAsGoalie();
					break;
			}
			yield return new WaitForSeconds(decisionRate);
		}

	}


	#region Goalie
	private void MakeDecisionAsGoalie() {
		TryGetBall();

		Player _ballCarrier = GameManager.Instance.GetBallCarrier();
		//Goalie has ball
		if (_ballCarrier == player) {
			ClearTheBall();
		} else {
			//Loose ball is headed towards net
			if (GameManager.Instance.IsBallHeadedTowardsGoal(player.GetTeam())) {
				MoveToBlockBall();
			//Ball is headed towards net
			} else { 
				GeneralGoaliePositioning();
			}
		}

		//Player is closest on team to loose ball or opponent with ball
		if ((!_ballCarrier || (_ballCarrier && _ballCarrier.GetTeam() != player.GetTeam())) && Utils.GetPlayerNearestBall(player.GetTeam()) == player) {
			SprintToBall();
			return;
		}
	}

	private void GeneralGoaliePositioning() {
		Vector2[] _postsToDefend = GameManager.Instance.GetPosts(player.GetTeam());
		Vector2 _defendPos = Utils.V3CenterPoint(_postsToDefend[0], _postsToDefend[1], GameManager.Instance.GetBallLocation());
		aiaStar.SprintUntilTargetReached(_defendPos);
	}
	#endregion

	#region Striker
	private void MakeDecisionAsStriker() {
		TryGetBall(true);

		//If Player Nearest The Ball
		if (GameManager.Instance.GetBallCarrier() != player && Utils.GetPlayerNearestBall(player.GetTeam()) == player) {
			MoveToBall();
		}
	}
	#endregion

	#region Defender
	private void MakeDecisionAsDefender() {
		TryGetBall(true);

		Player _ballCarrier = GameManager.Instance.GetBallCarrier();
		//Player _teammateNearestBall = Utils.GetPlayerNearestBall(player.GetTeam());

		//Player is closest on team to loose ball or opponent with ball
		if ((!_ballCarrier || (_ballCarrier && _ballCarrier.GetTeam() != player.GetTeam())) && Utils.GetPlayerNearestBall(player.GetTeam()) == player) {
			MoveToBall();
			return;
		}

		//When ball is loose in own zone
		bool _IsBallInOwnZone = Utils.IsV2LocationInZone(GameManager.Instance.GetBallLocation(), player.GetTeam());
		//Debug.Log($"Is ball in own zone for {gameObject.name}? {_IsBallInOwnZone}");
		if (!_ballCarrier && _IsBallInOwnZone) {
			MoveToBall();
			return;
		} else if(!_ballCarrier && !_IsBallInOwnZone) {
			MoveToFormation();
			return;
		}

		//Has ball
		if (_ballCarrier == player) {
			player.SetAbility(true); //Charge shot
			AvoidOpponent();
			return;
		}

		//Team has ball
		if (_ballCarrier && _ballCarrier.GetTeam() == player.GetTeam() && _ballCarrier != player) {
			aiaStar.SetTarget(Utils.GetDefendingZoneBasedLocation(new Vector2(.1f, 0f), player.GetTeam()));
			return;
		}

	}
	#endregion

	#region Playmaker
	private void MakeDecisionAsPlaymaker() {
		Player _ballCarrier = GameManager.Instance.GetBallCarrier();

		//If Player Nearest The Ball
		if (_ballCarrier != player && Utils.GetPlayerNearestBall(player.GetTeam()) == player) {
			MoveToBall();
		}

		//If player Has ball
		if (_ballCarrier == player) {
			player.SetAbility(true); //charge shot
			//AvoidAll();
		}

		TryGetBall();
	}
	#endregion
	private void ClearTheBall() {
		string[] _tag = new string[] { Utils.GetOpposingPlayerTag(player), "stadium" };
		List<int> _list = Utils.GetForwardDirsPerTeam(player.GetTeam());

		int _dirToMove = Utils.GetDirWithoutTags(player, localPerception, _tag, DirectionType.ForwardPreferredNeutral);
		if (_dirToMove == -1) {
			return;
		}

		AttemptKick(_dirToMove);
	}

	private void TryGetBall(bool _staminaCheck = false) {
		//Debug.Log($"{player.name} is trying to get ball");
		if (player.GetIsBallNearby()) {
			Player _ballCarrier = GameManager.Instance.GetBallCarrier();

			//Attempt to take the ball from the opposing player or a freeball
			if (!_ballCarrier || (_ballCarrier && _ballCarrier.GetTeam() != player.GetTeam())) {
				if(_staminaCheck && _ballCarrier && _ballCarrier.GetCurrentStamina() > player.GetCurrentStamina()) {
					return;
				}
				player.SetAbility(true);
				player.SetMovement(Utils.IntDirToInput(Utils.GetForwardDirPerTeam(player.GetTeam()))); //Do this so that ball doesn't end up in own net
				//player.SetAbility(false);
			}
		}
	}

	private void MoveToBlockBall() {
		aiaStar.SetTarget(new Vector2(player.transform.position.x, GameManager.Instance.GetBallLocation().y));
	}

	private void MoveToBall() {
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}

	private void MoveToFormation() {
		aiaStar.SetTarget(player.GetFormationLocation());
	}

	private void SprintToBall() {
		player.SetSprint(true);
		aiaStar.SetTarget(GameManager.Instance.GetBallLocation());
	}

	private void AvoidOpponent() {
		Debug.Log($"{gameObject.name} avoiding opponents");
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
				Utils.DebugDir(_dir, transform.position);
				player.SetMovement(_currentMoveInput);
				return;
			} else {
				TryAttemptPass();
			}

		} else {
			TryAttemptPass();
		}


	
	}

	private void TryAttemptPass() {
		int _newDir = Utils.GetDirWithoutTags(player, localPerception, new string[] { "stadium", Utils.GetOpposingPlayerTag(player) }, DirectionType.ForwardPreferred);
		if (_newDir == -1) _newDir = Utils.GetForwardDirPerTeam(player.GetTeam());
		AttemptKick(_newDir);
	}

	private void AttemptKick(int _dir) {
		Debug.Log("Attempting Kick");
		float _minShootChargeStrength = GameManager.Instance.maxChargeTime / 2f;
		if (player.GetShotCharge() >= _minShootChargeStrength) {
			KickInDir(_dir);
		}

	}

	private void KickInDir(int _dir) {
		Debug.Log($"Attempting to kick in {_dir}");
		player.SetMovement(Utils.IntDirToInput(_dir));
		player.SetAbility(false);
	}
}
