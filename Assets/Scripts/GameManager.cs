using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameState{
	Paused,
	Playing,
	Kickoff,
	Goal,
	End,
	Overtime,
	Training,
	Whistle
}
public class GameManager : MonoBehaviour {
	public static GameManager Instance;
	[SerializeField] private GameState currentGameState;

	[Header("Teams")]
	[SerializeField] private GameObject homePrefab;
	[SerializeField] private GameObject awayPrefab;
	[SerializeField] private GameObject homeFormationPrefab;
	[SerializeField] private GameObject awayFormationPrefab;
	private Formation[] homeFormation;
	private Formation[] awayFormation;


	private int homeScore;
	private int awayScore;
	private Team lastScoringTeam;
	private float timer;
	private float maxTime = 3f * 60f;
	[Header("General")]
	[SerializeField] private TextMeshProUGUI homeScoreText;
	[SerializeField] private TextMeshProUGUI awayScoreText;
	[SerializeField] private TextMeshProUGUI timerText;
	[SerializeField] private TextMeshProUGUI homeAbrvText;
	[SerializeField] private TextMeshProUGUI awayAbrvText;
	[SerializeField] private Image homeFlag;
	[SerializeField] private Image awayFlag;
	[SerializeField] private GameObject[] skins;
	public LayerMask DirectionLayersToCheck;
	public Vector2 ArenaWidth, ArenaHeight;
	public float TimeScale = 1f;

	public float standardSpeed { get; private set; } = 20f;
	public float sprintSpeed { get; private set; } = 27f;
	public float returnToFormationSpeed { get; private set; } = 50f;
	public float maxStamina { get; private set; } = 100f;
	public float staminaRechargeRate { get; private set; } = .1f;
	public float staminaConsumeRate { get; private set; } = .1f;
	public float staminaRechargeDelay { get; private set; } = 2f;
	public float staminaDisplayTime { get; private set; } = 2f;
	public float speedLerp { get; private set; } = 5f;
	public float minChargeForShot { get; private set; } = .8f;
	public float shotChargeRate { get; private set; } = .1f;
	public float maxChargeTime { get; private set; } = 10f;
	public float ShotMaxStrength { get; private set; } = 100f;
	public float ShotMinStrength { get; private set; } = 1f;
	public float ShotCooldown { get; private set; } = 2f;
	public int animFrameRate { get; private set; } = 24;
	public float PerceptionLength { get; private set; } = .75f;

	[Header("Ball")]
	[SerializeField] private Ball ball;

	private bool isOvertime = false;
	/// <summary>
	/// Global Game Data
	/// </summary>
	private List<Player> homePlayers = new List<Player>();
	private List<Player> awayPlayers = new List<Player>();
	private List<Player> allPlayers = new List<Player>();
	private Vector2[] homePosts;
	private Vector2[] awayPosts;
	private bool canMove;
	private bool TransitioningToFaceoffPos = false;

	private float dataCollectionRate = .1f;
	private Player homePlayerNearestBall, awayPlayerNearestBall, playerNearestBall;
	private bool isTraining;

	private CountryInfo homeCountryInfo, awayCountryInfo;

	private List<Collider2D> projectedBallCollisionObjects = new List<Collider2D>();
	private void Awake() {
		if(Instance != null) {
			Destroy(this);
		} else {
			Instance = this;
		}

		homeFormation = Instantiate(homeFormationPrefab, transform).GetComponentsInChildren<Formation>();
		awayFormation = Instantiate(awayFormationPrefab, transform).GetComponentsInChildren<Formation>();

		Application.targetFrameRate = 60;
		Time.timeScale = TimeScale;
	}
	[ContextMenu("Update Timescale")]
	private void UpdateTimeScale() {
		Time.timeScale = TimeScale;
	}

	public CountryInfo GetCountryInfo(Team _team) {
		return _team == Team.Home ? homeCountryInfo : awayCountryInfo;
	}

	public void SetCountryInfo(Team _team, CountryInfo _countryInfo) {
		Debug.Log($"Setting country info for {_team} to {_countryInfo.name}");
		if(_countryInfo == null) {
			return;
		}

		if(_team == Team.Home) {
			homeCountryInfo = _countryInfo;
			homeAbrvText.text = homeCountryInfo.Abbreviation;
			homeAbrvText.color = homeCountryInfo.HomeJerseyColor;
			homeFlag.sprite = homeCountryInfo.Flag;
		} else {
			awayCountryInfo = _countryInfo;
			awayAbrvText.text = awayCountryInfo.Abbreviation;
			awayAbrvText.color = awayCountryInfo.AwayJerseyColor;
			awayFlag.sprite = awayCountryInfo.Flag;
		}
	}

	private void Start() {
		if (currentGameState != GameState.Training) timer = maxTime;

		UpdateScoreBoard();

		System.Random rnd = new System.Random();
		Team randomTeam = (Team)rnd.Next(Enum.GetNames(typeof(Team)).Length);

		lastScoringTeam = randomTeam;
		StartCoroutine(CollectInformation());
		if(currentGameState == GameState.Training) {
			SetGameState(GameState.Training);
		} else {
			StartCoroutine(SetGameStateAfterDelay(GameState.Kickoff, .1f));
		}


		ApplyFormations(lastScoringTeam == Team.Home ? Team.Away : Team.Home);
	}

	IEnumerator CollectInformation() {
		while (true) {
			yield return new WaitForSeconds(dataCollectionRate);
			UpdatePlayersNearestBall();
		}
	}
	public GameObject GetRandomSkin() {
		return skins[UnityEngine.Random.Range(0, skins.Length)];
	}
	public Team GetLastScoringTeam() {
		return lastScoringTeam;
	}

	public bool GetIsTransitioning() {
		return TransitioningToFaceoffPos;
	}

	public void SetIsTransitioning(bool _value) {
		//Debug.Log($"Trying to set transitioning to {_value}");
		if(TransitioningToFaceoffPos == _value) {
			//Debug.Log($"Exiting transition because they're equal.");
			return;
		}
		TransitioningToFaceoffPos = _value;
		if (TransitioningToFaceoffPos) {
			StopCoroutine(CheckPlayersHaveTransitioned());
			StartCoroutine(CheckPlayersHaveTransitioned());
		}
	}

	IEnumerator CheckPlayersHaveTransitioned() {
		//Debug.Log("Starting Check for players have transitioned");
		yield return new WaitUntil(() => allPlayers.Count > 0);
		//Debug.Log("At least one player found.  Checking if has transitioned");
		bool _haveAllPlayersTransitioned = false;
		while (!_haveAllPlayersTransitioned) {
			for (int i = 0; i < allPlayers.Count; i++) {
				float _distanceToFormationLocation = Vector2.Distance(allPlayers[i].GetFormationLocation(), allPlayers[i].transform.position);
				if (_distanceToFormationLocation > .05f) {
					//Debug.Log($"{allPlayers[i].name} at {allPlayers[i].transform.position} has not transitioned to {allPlayers[i].GetFormationLocation()}");
					break;
				}

				if (i == allPlayers.Count - 1 && Vector2.Distance(allPlayers[i].GetFormationLocation(), allPlayers[i].transform.position) < .05f) {
					_haveAllPlayersTransitioned = true;
					break;
				}
			}

			yield return new WaitForSeconds(dataCollectionRate);
		}
		EndAllAgentEpisodes();
		yield return new WaitForSeconds(.3f);
		//Debug.Log("All players have transitioned");
		SetIsTransitioning(false);
	}

	private void EndAllAgentEpisodes() {
		for (int i = 0; i < allPlayers.Count; i++) {
			PlayerAgent _agent = allPlayers[i].GetAgent();
			if (_agent) {
				_agent.EndEpisode();
			}
		}
	}

	private void UpdatePlayersNearestBall() {
		float _currentClosestHomeDistanceToBall = float.PositiveInfinity;
		float _currentClosestAwayDistanceToBall = float.PositiveInfinity;

		Player _currentClosestHomePlayerToBall = null;
		Player _currentClosestAwayPlayerToBall = null;

		foreach (Player _player in homePlayers) {
			float _currentDistance = Vector2.Distance(_player.transform.position, GetBallLocation());
			if (_currentDistance < _currentClosestHomeDistanceToBall) {
				_currentClosestHomeDistanceToBall = _currentDistance;
				_currentClosestHomePlayerToBall = _player;
			}
		}

		homePlayerNearestBall = _currentClosestHomePlayerToBall;

		foreach (Player _player in awayPlayers) {
			float _currentDistance = Vector2.Distance(_player.transform.position, GetBallLocation());
			if (_currentDistance < _currentClosestAwayDistanceToBall) {
				_currentClosestAwayDistanceToBall = _currentDistance;
				_currentClosestAwayPlayerToBall = _player;
			}
		}

		awayPlayerNearestBall = _currentClosestAwayPlayerToBall;

		if(homePlayerNearestBall && awayPlayerNearestBall) {
			playerNearestBall = _currentClosestHomeDistanceToBall < _currentClosestAwayDistanceToBall ? homePlayerNearestBall : awayPlayerNearestBall;
		}

	}

	public List<Player> GetPlayers(Team? _team = null) {
		if (_team != null) {
			return _team == Team.Home ? homePlayers : awayPlayers;
		} else {
			List<Player> _combinedPlayerList = new List<Player>();
			_combinedPlayerList.AddRange(homePlayers);
			_combinedPlayerList.AddRange(awayPlayers);
			return _combinedPlayerList;
		}
		
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
		if(currentGameState == GameState.End) {
			if (Input.GetKeyDown(KeyCode.R)) {
				Rematch();
			}
		}
	}



	public float ballSimulationTime = 1.0f;
	public int numPoints = 100;
	public float pointSpacing = 0.1f;
	public LayerMask ballSimCollisionLayers;
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
		float timeStep = ballSimulationTime / numPoints;
		float drag = ball.GetComponent<Rigidbody2D>().drag;
		
		Collider2D ballCollider = ball.GetComponent<Collider2D>();

		ContactFilter2D contactFilter = new ContactFilter2D();
		contactFilter.useTriggers = true;
		contactFilter.SetLayerMask(ballSimCollisionLayers);
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

	public Player GetCachedPlayerNearestBall(Team? team = null) {
		if (team == Team.Home) {
			return homePlayerNearestBall;
		} else if (team == Team.Away) {
			return awayPlayerNearestBall;
		} else {
			return playerNearestBall;
		}
	}

	public void Score(Team team) {
		if(currentGameState != GameState.Playing && !isTraining) {
			return;
		}
		lastScoringTeam = team;

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
			if (_lastShot && _lastShot.GetTeam() != team) {
				PenalizeOwnGoal(_lastShot);
			}
		}

		Player _scorer;
		if (team == Team.Home) {
			homeScore++;
			_scorer = ball.GetScorer(Team.Home);
		} else {
			awayScore++;
			_scorer = ball.GetScorer(Team.Away);
		}

		if (_scorer) RewardScoringAgent(_scorer);

		if (isOvertime) {
			SetGameState(GameState.End);
		} else {
			SetGameState(GameState.Goal);
		}

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

	private void UpdateTimer() {
		if(currentGameState != GameState.Playing && currentGameState != GameState.Training) {
			return;
		}
		string formattedTime;
		if (isTraining || isOvertime) {
			timer += Time.deltaTime;
			TimeSpan time = TimeSpan.FromSeconds(timer);
			formattedTime = string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
		} else {
			timer -= Time.deltaTime;
			TimeSpan time = TimeSpan.FromSeconds(timer);
			formattedTime = string.Format("{0}:{1:00}", (int)time.TotalMinutes, time.Seconds);
		}

		timerText.text = formattedTime;
		if(timer <= 0) {
			if (awayScore != homeScore) {
				SetGameState(GameState.End);
			} else {
				SetGameState(GameState.Overtime);
			}
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

	public void SetBallCarrier(Player _value) {
		ball.SetBallCarrier(_value);
	}

	public bool GetIsTraining() {
		return isTraining;
	}

	public void SetGameState(GameState _value) {
		if((_value == GameState.Kickoff && timer != maxTime) && (_value == currentGameState && _value != GameState.Training)) {
			return;
		}
		currentGameState = _value;
		Debug.Log($"Changing game state to {currentGameState}");
		switch (currentGameState) {
			case GameState.Goal:
				ball.Reset(false);
				StartCoroutine(GoalCelebration());
				break;
			case GameState.Kickoff:
				ball.Reset(true);
				StartCoroutine(SetGameStateAfterTransition(GameState.Playing));
				break;
			case GameState.Playing:
				canMove = true;
				break;
			case GameState.Whistle:
				ball.Reset(false);
				StartCoroutine(Whistle());
				break;
			case GameState.Overtime:
				ball.Reset(false);
				isOvertime = true;
				timer = 0;

				System.Random rnd = new System.Random();
				Team randomTeam = (Team)rnd.Next(Enum.GetNames(typeof(Team)).Length);
				lastScoringTeam = randomTeam;

				ApplyFormations(lastScoringTeam);
				StartCoroutine(SetGameStateAfterTransition(GameState.Kickoff));
				break;
			case GameState.End:
				canMove = false;
				ball.Reset(false);
				List<Player> _winningTeamPlayers = homeScore > awayScore ? homePlayers : awayPlayers;
				foreach (Player _player in allPlayers) {
					_player.Reset();
				}
				foreach (Player _player in _winningTeamPlayers) {
					_player.SetIsCheering(true);
				}
				break;
			case GameState.Training:
				isTraining = true;
				canMove = true;
				ball.Reset(true);
				timer = 0;
				break;
		}
	}



	public bool GetCanMove() {
		return canMove;
	}

	IEnumerator SetGameStateAfterDelay(GameState _value, float _delay) {
		yield return new WaitForSeconds(_delay);
		SetGameState(_value);
	}
	IEnumerator SetGameStateAfterTransition(GameState _value) {
		yield return new WaitUntil(() => !GetIsTransitioning());
		SetGameState(_value);
	}

	IEnumerator GoalCelebration() {
		List<Player> _scoringTeamPlayers = lastScoringTeam == Team.Home ? homePlayers : awayPlayers;
		canMove = false;
		foreach (Player _player in allPlayers) {
			_player.Reset();
		}
		foreach (Player _player in _scoringTeamPlayers) {
			_player.SetIsCheering(true);
		}
		yield return new WaitForSeconds(3f);
		foreach (Player _player in _scoringTeamPlayers) {
			_player.SetIsCheering(false);
		}
		canMove = true;
		ApplyFormations(lastScoringTeam == Team.Home ? Team.Away : Team.Home);
		float _savedSpeed = standardSpeed;
		standardSpeed = returnToFormationSpeed;
		yield return new WaitUntil(() => !GetIsTransitioning());
		StartCoroutine(SetGameStateAfterDelay(GameState.Kickoff, .1f));
		standardSpeed = _savedSpeed;
	}

	IEnumerator Whistle() {
		ball.Reset(false);
		canMove = false;
		foreach (Player _player in allPlayers) {
			_player.Reset();
		}
		canMove = true;
		ball.Reset(false);
		ApplyFormations(lastScoringTeam == Team.Home ? Team.Away : Team.Home);
		float _savedSpeed = standardSpeed;
		standardSpeed = returnToFormationSpeed;
		ball.Reset(false);
		yield return new WaitUntil(() => !GetIsTransitioning());
		ball.Reset(false);
		StartCoroutine(SetGameStateAfterDelay(GameState.Kickoff, .1f));
		standardSpeed = _savedSpeed;
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
		allPlayers.Add(_player);
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

	private void Rematch() {
		canMove = true;
		homeScore = 0;
		awayScore = 0;
		timer = maxTime;
		UpdateScoreBoard();
		StartCoroutine(Whistle());
	}
}
