using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
	Rigidbody2D rb;
	Collider2D col;

	[SerializeField] Sprite[] activeAnim;
	[SerializeField] int MaxBallSpeedFrameRate = 24;
	[SerializeField] int MinBallSpeedFrameRate = 60;
	[SerializeField] int CappedBallSpeed = 1;
	[SerializeField] int BallSpeedAtMaxFrameRate = 1;
	[SerializeField] float ballCarryingLerpSpeed = 1;
	[SerializeField] float maxDifferentalForShot = .1f;
	private int currentFrame;
	private int currentAnimFrame;
	private SpriteRenderer rend;
	private Player ballCarrier;
	private Player lastHomeTouch;
	private Player lastAwayTouch;
	private Player lastTouch;
	private Player lastShot;
	private Vector2? shotVector;
	bool canPickup;

	private float timeout = 25f;
	private float timeoutTimer;
	
	private readonly NetworkVariable<BallNetworkData>
		netState = new(writePerm: NetworkVariableWritePermission.Server);

	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		rend = GetComponent<SpriteRenderer>();
	}

	public void Start() {
		Reset(true);
	}

	public void Reset(bool _enablePhysics) {
		//Debug.Log("Resetting ball");
		if(!IsServer) return;
		SetBallCarrier(null);
		timeoutTimer = 0;
		col.enabled = false;
		transform.position = new Vector2(.004f, 0f);
		rb.velocity = Vector2.zero;
		rb.isKinematic = !_enablePhysics;
		lastAwayTouch = null;
		lastHomeTouch = null;
		lastShot = null;
		lastTouch = null;
		shotVector = null;
		canPickup = true;
		col.enabled = _enablePhysics;
		/*foreach(Player _player in GameManager.Instance.GetPlayers()) {
			_player.Reset();
		}*/
	}

	public void SetBallCarrier(Player _value) {
		GameState _gameState = GameManager.Instance.GetGameState();
		if (!(_gameState == GameState.Playing || _gameState == GameState.Training || _gameState == GameState.Overtime || _gameState == GameState.Kickoff || _gameState == GameState.Goal)) {
			//Debug.Log($"Trying to set ball holder to {_value} during non-playing state");
			return;
		}

		if (_value != null && (_gameState == GameState.Kickoff || _gameState == GameState.Overtime || _gameState == GameState.Goal)) {
			//Debug.Log($"Trying to set ball holder to {_value.name} during a transition state");
			return;
		}

		//Register Pass & Giveaway
		if (/*shotVector != null &&*/ _value && lastShot) {
			if (lastShot.GetTeam() == _value.GetTeam() && lastShot != _value) {
				lastShot.Pass();
			} else if (lastShot != _value) {
				lastShot.Giveaway();
			}
			shotVector = null;
		}

		if (ballCarrier) {
			if (_value) {
				if (ballCarrier.GetTeam() == _value.GetTeam()) {
					_value.FriendlyTackle();
				} else {
					_value.Tackle();
					ballCarrier.TackleGiveaway();
				}
			}

			ballCarrier.SetHasBall(false);
		} else if (GetLastShot() && _value && GetLastShot() != _value) {
			PlayerAgent _agent = _value.GetAgent();
			if(_agent) _agent.GotLooseBallReward();
		}
	
		ballCarrier = _value;

		if (ballCarrier) {
			lastTouch = ballCarrier;
			shotVector = null;
			timeoutTimer = 0;
			SetLastTouch(ballCarrier);
			ballCarrier.SetHasBall(true);
			rb.isKinematic = true;
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0;
			if (GameManager.Instance.GetGameState() == GameState.Kickoff) {
				GameManager.Instance.SetGameState(GameState.Playing);
			}

		} else {
			rb.isKinematic = false;
		}
	}

	private void Update() {
		if(IsServer){
			netState.Value = new BallNetworkData() {
				Position = transform.position
			};
			
			CheckOutOfBounds();

			if (rb.velocity.magnitude < 1f /*&& !GameManager.Instance.GetIsTraining()*/) {
				timeoutTimer += Time.deltaTime;
				if (timeoutTimer > timeout) {
					GameManager.Instance.SetGameState(GameState.Whistle);
				}
			} else {
				timeoutTimer = 0f;
			}

			if (rb.velocity.magnitude < .01f && !ballCarrier) {
				return;
			}

			currentFrame++;
			//Debug.Log("Ball Speed: " + rb.velocity.magnitude);
			float _ballSpeed = ballCarrier ? ballCarrier.GetVelocityMagnitude() : rb.velocity.magnitude;
			float _cappedSpeed = Mathf.Clamp(Mathf.Abs(_ballSpeed), 0, CappedBallSpeed);
			float _framerate = Mathf.Lerp(MinBallSpeedFrameRate, MaxBallSpeedFrameRate, _cappedSpeed / BallSpeedAtMaxFrameRate);
			if (currentFrame >= _framerate) {
				currentAnimFrame = currentAnimFrame == activeAnim.Length - 1 ? 0 : currentAnimFrame + 1;
				rend.sprite = activeAnim[currentAnimFrame];
				currentFrame = 0;
			}
			
		} else {
			transform.position = netState.Value.Position;
		}
	}

	public Player GetScorer(Team team) {
		return team == Team.Home ? lastHomeTouch : lastAwayTouch;
	}

	public Player GetBallCarrier() {
		return ballCarrier;
	}

	public Player GetLastShot() {
		return lastShot;
	}

	public Vector2? GetShotVector() {
		return shotVector;
	}

	public Vector2 GetVelocity() {
		return rb.velocity;
	}

	private void FixedUpdate() {
		if (!IsServer) return;
		if (ballCarrier != null && ballCarrier.targetBallPosition != null) {
			rb.MovePosition(Vector3.Lerp(transform.position, (Vector2)ballCarrier.transform.position + ballCarrier.targetBallPosition, ballCarryingLerpSpeed * Time.fixedDeltaTime));
		}
		if(shotVector != null && (/*Vector2.Distance(shotVector.Value, rb.velocity.normalized) > maxDifferentalForShot ||*/ rb.velocity.magnitude < .02f)) {
			shotVector = null;
		}

	}

	public void Shoot(Vector2 _force) {
		if(ballCarrier == null) {
			return;
		}
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0;
		lastShot = ballCarrier;
		PlayerAgent _agent = ballCarrier.GetAgent();
		if (_agent) _agent.ShooterReward(_force.magnitude);
		SetBallCarrier(null);
		rb.AddForce(_force);
		shotVector = rb.velocity.normalized;
		if (rb.velocity.magnitude > 50f) { //Don't allow players to pickup the ball of it's shot as a high velocity
			canPickup = false;
			Invoke("EnableCanPickup", GameManager.Instance.ShotCooldown);
		}
	}

	private void EnableCanPickup() {
		canPickup = true;
	}


	private void OnCollisionEnter2D(Collision2D collision) {
		if(GameManager.Instance.GetGameState() == GameState.Kickoff) {
			GameManager.Instance.SetGameState(GameState.Playing);
		}

		if(collision.gameObject.CompareTag("home") || collision.gameObject.CompareTag("away")) {
			Player _touchedPlayer = collision.gameObject.GetComponent<Player>();
			if(_touchedPlayer) SetLastTouch(_touchedPlayer);
		}
	}

	private void SetLastTouch(Player _player) {
		lastTouch = _player;
		if (_player.GetTeam() == Team.Home) {
			lastHomeTouch = ballCarrier;
		} else {
			lastAwayTouch = ballCarrier;
		}
	}

	private void CheckOutOfBounds() {
		if (transform.position.x < GameManager.Instance.ArenaWidth.x ||
			transform.position.x > GameManager.Instance.ArenaWidth.y ||
			transform.position.y < GameManager.Instance.ArenaHeight.x ||
			transform.position.y > GameManager.Instance.ArenaHeight.y) {
			Reset(true);
		}
	}
	
	struct BallNetworkData : INetworkSerializable {
		private float posX;
		private float posY;

		internal Vector2 Position {
			get => new Vector2(posX, posY);
			set {
				posX = value.x;
				posY = value.y;
			}
		}


		public void NetworkSerialize<T>(BufferSerializer<T> _serializer) where T : IReaderWriter {
			_serializer.SerializeValue(ref posX);
			_serializer.SerializeValue(ref posY);
		}
	}
	
}


