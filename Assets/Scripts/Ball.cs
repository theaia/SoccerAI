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
	[SerializeField] Cursor cursor;
	private int currentFrame;
	private int currentAnimFrame;
	private SpriteRenderer renderer;
	private PlayerController ballCarrier;

	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		renderer = GetComponent<SpriteRenderer>();
	}

	public void Start() {
		Reset();
	}

	public void Reset() {
		col.enabled = false;
		transform.position = new Vector2(.004f, 0f);
		rb.velocity = Vector2.zero;
		col.enabled = true;
		rb.isKinematic = false;
		SetBallCarrier(null);
	}
	public void SetBallCarrier(PlayerController _value) {
		if (ballCarrier) {
			ballCarrier.SetHasBall(false);
		}
		ballCarrier = _value;
		if (ballCarrier) {
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
		if(rb.velocity.magnitude < .01f && !ballCarrier) {
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

	public PlayerController GetBallCarrier() {
		return ballCarrier;
	}

	private void FixedUpdate() {
		if (ballCarrier != null) {
			rb.MovePosition(Vector3.Lerp(transform.position, ballCarrier.targetBallPosition.transform.position, ballCarryingLerpSpeed * Time.fixedDeltaTime));
		}
	}

	public void Shoot(Vector2 _force) {
		if(ballCarrier == null) {
			return;
		}
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0;
		SetBallCarrier(null);
		rb.AddForce(_force);
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if(GameManager.Instance.GetGameState() == GameState.Kickoff) {
			GameManager.Instance.SetGameState(GameState.Playing);
		}
	}

}
