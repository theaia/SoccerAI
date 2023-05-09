using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIADecisionMaker : MonoBehaviour
{
	public float decisionRate = .1f;
	Player player;
	AIAstar aiaStar;

	private bool isClearing;

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
			switch (player.GetRole()) {
				default:
				case PlayStyle.Defender:
					break;
				case PlayStyle.Playmaker:
					break;
				case PlayStyle.Striker:
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
		if (player.GetIsBallNearby()) {
			Player _ballCarrier = GameManager.Instance.GetBallCarrier();

			//Attempt to take the ball from the opposing player or a freeball
			if (!_ballCarrier || (_ballCarrier && _ballCarrier.GetTeam() != player.GetTeam())) {
				player.SetAbility(true);
				player.SetMovement(Utils.IntDirToInput(Utils.GetForwardDirPerTeam(player.GetTeam()))); //Do this so that ball doesn't end up in own net
			}
		}

		//Goalie has ball
		if (GameManager.Instance.GetBallCarrier() == player) {
			ClearTheBall();
		} else {
		//Goalie doesn't have ball
			if (GameManager.Instance.IsBallHeadedTowardsGoal(player.GetTeam())) {
				MoveToBlockBall();
			} else {
				GeneralGoaliePositioning();
			}
		}
		//if shot is on target sprint to get to position
		//if ball is near, && not being held by opponent, challenge them.
	}

	private void ClearTheBall() {
		if (isClearing) {
			return;
		}

		string _tag = Utils.GetOpposingPlayerTag(player);
		List<int> _list = Utils.GetForwardDirsPerTeam(player.GetTeam());

		int _dirToMove = Utils.GetRandomCollisionWithoutTag(player.transform.position, _tag, _list);
		if(_dirToMove == -1){
			return;
		}
		StartCoroutine(MoveInDirAndClear(_dirToMove, GameManager.Instance.maxChargeTime));
	}

	IEnumerator MoveInDirAndClear(int _dirToMove, float _targetValue) {
		isClearing = true;
		while (player.GetShotCharge() < _targetValue) {
			player.SetMovement(Utils.IntDirToInput(_dirToMove));
			if (player != GameManager.Instance.GetBallCarrier()) {
				isClearing = false;
				yield break;
			}
			player.SetAbility(true);
			yield return null;
		}

		player.SetAbility(false); //This releases the ball

		/*for(int i = 0; i < 3; i++) {
			player.SetMovement(Vector2.zero);
			yield return new WaitForEndOfFrame();
		}*/

		isClearing = false;
	}

	private void GeneralGoaliePositioning() {
		Vector2[] _postsToDefend = GameManager.Instance.GetPosts(player.GetTeam());
		Vector2 _defendPos = Utils.V3CenterPoint(_postsToDefend[0], _postsToDefend[1], GameManager.Instance.GetBallLocation());
		aiaStar.SprintUntilTargetReached(_defendPos);
	}

	private void MoveToBlockBall() {
		aiaStar.SetTarget(new Vector2(player.transform.position.x, GameManager.Instance.GetBallLocation().y));
	}
	#endregion


}
