using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
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
	[SerializeField] Cursor cursor;
	private int currentFrame;
	private int currentAnimFrame;
	private SpriteRenderer renderer;
	private Player ballCarrier;
	private Player lastHomeTouch;
	private Player lastAwayTouch;
	private Player lastTouch;
	private Player lastShot;
	private Vector2? shotVector;

	private float timeout = 25f;
	private float timer;

	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		renderer = GetComponent<SpriteRenderer>();
	}

	public void Start() {
		Reset();
	}

	public void Reset() {
		timer = 0;
		col.enabled = false;
		transform.position = new Vector2(.004f, 0f);
		rb.velocity = Vector2.zero;
		col.enabled = true;
		rb.isKinematic = false;
		lastAwayTouch = null;
		lastHomeTouch = null;
		lastShot = null;
		lastTouch = null;
		shotVector = null;
		SetBallCarrier(null);
	}
	public void SetBallCarrier(Player _value) {
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
			timer = 0;
			SetLastTouch(ballCarrier);
			ballCarrier.SetHasBall(true);
			rb.isKinematic = true;
			rb.velocity = Vector2.zero;
			rb.angularVelocity = 0;
			cursor.gameObject.SetActive(true);
			if (GameManager.Instance.GetGameState() == GameState.Kickoff) {
				GameManager.Instance.SetGameState(GameState.Playing);
			}

		} else {
			rb.isKinematic = false;
			cursor.gameObject.SetActive(false);
		}
	}

	private void Update() {
		if (GameManager.Instance.GetGameState() == GameState.Training && !ballCarrier) {
			timer += Time.deltaTime;
			if(timer > timeout) {
				Reset();
			}
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
			renderer.sprite = activeAnim[currentAnimFrame];
			currentFrame = 0;
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
		if (ballCarrier != null && ballCarrier.targetBallPosition != null) {
			rb.MovePosition(Vector3.Lerp(transform.position, ballCarrier.targetBallPosition.transform.position, ballCarryingLerpSpeed * Time.fixedDeltaTime));
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

}
