using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameState{
	Paused,
	Playing,
	Kickoff,
	Goal,
	End,
	Training
}
public class GameManager : MonoBehaviour {
	public static GameManager Instance;
	[SerializeField] private GameState currentGameState;
	[SerializeField] private GameObject homePrefab, awayPrefab;
	[SerializeField] private GameObject homeFormationPrefab, awayFormationPrefab;
	private Formation[] homeFormation, awayFormation;
	private int homeScore, awayScore;
	private Team lastScoringTeam;
	private float timer;
	private float maxTime = 3f * 60f;
	[SerializeField] private TextMeshProUGUI homeScoreText, awayScoreText, timerText;

	public float standardSpeed { get; private set; } = 15;
	public float sprintSpeed { get; private set; } = 20f;
	public float maxStamina { get; private set; } = 100f;
	public float staminaRechargeRate { get; private set; } = .05f;
	public float staminaConsumeRate { get; private set; } = .1f;
	public float staminaRechargeDelay { get; private set; } = 2f;
	public float staminaDisplayTime { get; private set; } = 2f;
	public float speedLerp { get; private set; } = 5f;
	public float minChargeForShot { get; private set; } = .8f;
	public float shotChargeRate { get; private set; } = .1f;
	public float maxChargeTime { get; private set; } = 10f;
	public float ShotMaxStrength { get; private set; } = 100f;
	public float ShotMinStrength { get; private set; } = 1f;
	public float ShotCooldown { get; private set; } = 3f;
	public int animFrameRate { get; private set; } = 24;
	[Space(10)]
	[SerializeField] GameObject[] HomeCharacterSprites;
	[SerializeField] GameObject[] AwayCharacterSprites;

	[Header("Ball")]
	[SerializeField] private Ball ball;


	/// <summary>
	/// Global Game Data
	/// </summary>
	private List<Player> homePlayers = new List<Player>();
	private List<Player> awayPlayers = new List<Player>();
	private Vector2[] homePosts;
	private Vector2[] awayPosts;

	private List<Collider2D> projectedBallCollisionObjects = new List<Collider2D>();

	private Player closestHomeToBall, closestAwayToBall, closestToBall;
	private void Awake() {
		if(Instance != null) {
			Destroy(this);
		} else {
			Instance = this;
		}

		homeFormation = Instantiate(homeFormationPrefab, transform).GetComponentsInChildren<Formation>();
		awayFormation = Instantiate(awayFormationPrefab, transform).GetComponentsInChildren<Formation>();
	}

	private void Start() {
		if (currentGameState != GameState.Training) timer = maxTime;

		UpdateScoreBoard();

		System.Random rnd = new System.Random();
		Team randomTeam = (Team)rnd.Next(Enum.GetNames(typeof(Team)).Length);

		ApplyFormations(randomTeam);
	}

	public List<Player> GetPlayers(Team _team) {
		return _team == Team.Home ? homePlayers : awayPlayers;
	}

	public GameObject GetPlayerPrefab(Team _team) {
		return _team == Team.Home ? homePrefab : awayPrefab;
	}


	private void ApplyFormations(Team _teamKickingOff) {
		ProcessFormation(_teamKickingOff, Team.Home);
		ProcessFormation(_teamKickingOff, Team.Away);
	}



	private void ProcessFormation(Team _teamKickingOff, Team _processTeam) {
		Formation[] _teamFormation = _processTeam == Team.Home ? homeFormation : awayFormation;
		for (int i = 0; i < _teamFormation.Length; i++) {
			if (_teamKickingOff == _processTeam && _teamFormation[i].GetKickoffType() == KickoffType.Attacking) {
				_teamFormation[i].SetFormation(_processTeam);
				return;
			} else if (_teamKickingOff != _processTeam && _teamFormation[i].GetKickoffType() == KickoffType.Defending) {
				_teamFormation[i].SetFormation(_processTeam);
			}
		}
	}
	public void SetPosts(Team team, Vector2[] _values) {
		if (team == Team.Home) {
			homePosts = _values;
		} else {
			awayPosts = _values;
		}
	}

	public Vector2[] GetPosts(Team team) {
		return team == Team.Home ? homePosts : awayPosts;
	}

	private void Update() {
		UpdateTimer();
		UpdateClosestPlayers();
	}



	public float simulationTime = 1.0f;
	public int numPoints = 100;
	public float pointSpacing = 0.1f;
	public LayerMask collisionLayer;
	private Vector2 finalPosition;
	public float lineDuration = 0.1f;

	private void OnDrawGizmos() {
		if (!Application.isPlaying) {
			return;
		}
		DrawTrajectory();
	}

	private void DrawTrajectory() {
		projectedBallCollisionObjects.Clear();
		Vector2 currentPosition = ball.transform.position;
		Vector2 currentVelocity = ball.GetVelocity();
		float timeStep = simulationTime / numPoints;
		float drag = ball.GetComponent<Rigidbody2D>().drag;
		
		Collider2D ballCollider = ball.GetComponent<Collider2D>();

		ContactFilter2D contactFilter = new ContactFilter2D();
		contactFilter.useTriggers = true;
		contactFilter.SetLayerMask(collisionLayer);
		contactFilter.useLayerMask = true;

		Gizmos.color = Color.red;

		for (int i = 0; i < numPoints; i++) {
			// Apply drag to the current velocity
			currentVelocity -= currentVelocity * drag * timeStep;

			Vector2 newPosition = currentPosition + currentVelocity * timeStep;
			RaycastHit2D[] hits = new RaycastHit2D[1];
			int hitCount = Physics2D.Raycast(currentPosition, currentVelocity, contactFilter, hits, (newPosition - currentPosition).magnitude);

			float timeStepRemaining = timeStep;
			while (hitCount > 0 && timeStepRemaining > 0) {
				RaycastHit2D hit = hits[0];

				if (hit.collider != ballCollider && !projectedBallCollisionObjects.Contains(hit.collider)) {
					projectedBallCollisionObjects.Add(hit.collider);
				}

				float distanceToCollision = hit.distance;
				float timeToCollision = distanceToCollision / currentVelocity.magnitude;
				currentPosition += currentVelocity * timeToCollision;
				timeStepRemaining -= timeToCollision;

				// Reflect the velocity vector based on the collision normal
				currentVelocity = Vector2.Reflect(currentVelocity, hit.normal);

				// Update the newPosition after the bounce
				newPosition = currentPosition + currentVelocity * timeStepRemaining;
				hitCount = Physics2D.Raycast(currentPosition, currentVelocity, contactFilter, hits, (newPosition - currentPosition).magnitude);
			}

			Gizmos.DrawLine(currentPosition, newPosition);
			currentPosition = newPosition;
		}

		finalPosition = currentPosition;
	}

	private void UpdateClosestPlayers() {
		Player _tempClosestHomePlayer = null;
		Player _tempClosestAwayPlayer = null;
		float _tempClosestHomeDistance = float.PositiveInfinity;
		float _tempClosestAwayDistance = float.PositiveInfinity;
		foreach(Player _player in homePlayers) {
			float _currentHomeDistanceToBall = _player.GetDistanceToBall();
			if (_tempClosestHomePlayer == null || _currentHomeDistanceToBall < _tempClosestHomeDistance) {
				_tempClosestHomePlayer = _player;
				_tempClosestHomeDistance = _currentHomeDistanceToBall;
			}
		}

		foreach (Player _player in awayPlayers) {
			float _currentAwayDistanceToBall = _player.GetDistanceToBall();
			if (_tempClosestAwayPlayer == null || _currentAwayDistanceToBall < _tempClosestAwayDistance) {
				_tempClosestAwayPlayer = _player;
				_tempClosestAwayDistance = _currentAwayDistanceToBall;
			}
		}

		closestHomeToBall = _tempClosestHomePlayer;
		closestAwayToBall = _tempClosestAwayPlayer;
		closestToBall = _tempClosestAwayDistance < _tempClosestHomeDistance ? _tempClosestAwayPlayer : _tempClosestHomePlayer;
	}


	public void Score(Team team) {
		if(currentGameState != GameState.Playing && currentGameState != GameState.Training) {
			return;
		}

		Player _ballCarrier = GetBallCarrier();
		bool _wasOwnGoal = _ballCarrier ? _ballCarrier.GetTeam() != team : false;
		//if carried into own net
		if (_ballCarrier) {
			if (_wasOwnGoal) {
				PenalizeOwnGoal(_ballCarrier);
			}
		} else {
		//if shot into own net
			Player _lastShot = ball.GetLastShot();
			if (_lastShot.GetTeam() != team) {
				PenalizeOwnGoal(_lastShot);
			}
		}

		if (currentGameState != GameState.Training) SetGameState(GameState.Goal);
		Player _scorer;
		if (team == Team.Home) {
			homeScore++;
			_scorer = ball.GetScorer(Team.Home);
		} else {
			awayScore++;
			_scorer = ball.GetScorer(Team.Away);
		}

		if (_scorer) RewardScoringAgent(_scorer);


		if (currentGameState == GameState.Training) ball.Reset();
		UpdateScoreBoard();
	}

	public string GetRandomName() {
		string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		char randomLetter = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
		string randomLastName = lastNames[UnityEngine.Random.Range(0, lastNames.Count)];
		string randomName = randomLetter + ". " + randomLastName;
		return randomName;
	}

	private void UpdateScoreBoard() {
		homeScoreText.text = homeScore.ToString("00");
		awayScoreText.text = awayScore.ToString("00");
	}

	private void RewardScoringAgent(Player _player) {
		PlayerAgent _agent = _player.GetComponent<PlayerAgent>();
		if(_agent) _agent.ScorerReward();
	}

	private void PenalizeOwnGoal(Player _player) {
		PlayerAgent _agent = _player.GetComponent<PlayerAgent>();
		if (_agent) _agent.OwnGoalerPenalty();
	}

	public Vector2 GetBallLocation() {
		return ball.transform.position;
	}

	public void GetClosestPlayerToBall(Team? _team = null) {

	}

	private void UpdateTimer() {
		if(currentGameState != GameState.Playing && currentGameState != GameState.Training) {
			return;
		}
		string formattedTime;
		if (currentGameState == GameState.Training) {
			timer += Time.deltaTime;
			TimeSpan time = TimeSpan.FromSeconds(timer);
			formattedTime = string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
		} else {
			timer -= Time.deltaTime;
			TimeSpan time = TimeSpan.FromSeconds(timer);
			formattedTime = string.Format("{0}:{1:00}", (int)time.TotalMinutes, time.Seconds);
		}

		timerText.text = formattedTime;
		if(timer == 0) {
			SetGameState(GameState.End);
		}
	}

	public Player GetBallCarrier() {
		return ball.GetBallCarrier();
	}

	private List<string> lastNames = new List<string>() {
		"Messi",
		"Ronaldo",
		"Neymar Jr.",
		"Salah",
		"Mbappe",
		"van Dijk",
		"Ramos",
		"De Bruyne",
		"Lewandowski",
		"Neuer",
		"Suárez",
		"Agüero",
		"Modric",
		"Hazard",
		"Sterling",
		"Kane",
		"Kroos",
		"Aubameyang",
		"Dybala",
		"Oblak",
	};

	public GameObject GetRandomTeamPrefab(Team team) {
		GameObject _randomPrefab = team == Team.Home ? HomeCharacterSprites[UnityEngine.Random.Range(0, HomeCharacterSprites.Length - 1)] : AwayCharacterSprites[UnityEngine.Random.Range(0, AwayCharacterSprites.Length - 1)];
		return _randomPrefab;
	}

	public void SetBallCarrier(Player _value) {
		ball.SetBallCarrier(_value);
	}

	public void SetGameState(GameState _value) {
		currentGameState = _value;
		switch (currentGameState) {
			case GameState.Goal:
				//5 second cele
				//run back to center
				StartCoroutine(SetGameStateAfterDelay(GameState.Kickoff, 6f));
				break;
			case GameState.Kickoff:
				ball.Reset();
				break;
		}
	}

	IEnumerator SetGameStateAfterDelay(GameState _value, float _delay) {
		yield return new WaitForSeconds(_delay);
		SetGameState(_value);
	}
	public GameState GetGameState() {
		return currentGameState;
	}


	public void ShootBall(Vector2 _value) {
		ball.Shoot(_value);
	}
	#region Global Information
	public void AddPlayer(Team _team, Player _player) {
		if (_team == Team.Home) {
			homePlayers.Add(_player);
		} else {
			awayPlayers.Add(_player);
		}
	}

	public Player GetPlayerNearestBall() {
		Player _playerNearestBall = homePlayers[0]; //default value
		float _currentClosestDistanceToBall = float.PositiveInfinity;
		for (int i = 0; i < homePlayers.Count; i++) {
			if (Vector3.Distance(homePlayers[i].transform.position, ball.transform.position) < _currentClosestDistanceToBall) {
				_playerNearestBall = homePlayers[i];
			};
		}

		for (int i = 0; i < awayPlayers.Count; i++) {
			if (Vector3.Distance(awayPlayers[i].transform.position, ball.transform.position) < _currentClosestDistanceToBall) {
				_playerNearestBall = awayPlayers[i];
			};
		}

		return _playerNearestBall;
	}

	public void DrawPredictedBallStoppingLocation() {
		projectedBallCollisionObjects.Clear();
		Vector2 predictedPos = ball.transform.position;
		Vector2 predictedVelocity = ball.GetVelocity();

		int currentBounces = 0;
		int maxBounces = 3;
		int trajectoryResolution = 30;
		float bounceThreshold = 0.1f;

		for (int i = 0; i < trajectoryResolution; i++) {
			Vector2 nextPos = predictedPos + predictedVelocity * Time.fixedDeltaTime;

			RaycastHit2D hit = Physics2D.Linecast(predictedPos, nextPos);
			if (hit.collider != null && currentBounces < maxBounces && predictedVelocity.magnitude > bounceThreshold) {
				Vector2 hitNormal = hit.normal;
				predictedVelocity = Vector2.Reflect(predictedVelocity, hitNormal) * hit.collider.sharedMaterial.bounciness;

				if (!projectedBallCollisionObjects.Contains(hit.collider)) {
					projectedBallCollisionObjects.Add(hit.collider);
				}

				currentBounces++;
			} else {
				predictedVelocity += Physics2D.gravity * Time.fixedDeltaTime;
			}

			Debug.DrawLine(predictedPos, nextPos, Color.red);
			predictedPos = nextPos;
		}
	}

	public bool IsBallHeadedTowardsGoal(Team team) {
		foreach(Collider2D col in projectedBallCollisionObjects) {
			if (col.CompareTag(team == Team.Home ? "homegoal" : "awaygoal")) {
				return true;
			}
		}
		return false;
	}
	#endregion
}
