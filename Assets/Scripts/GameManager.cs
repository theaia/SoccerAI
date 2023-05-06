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
	End
}
public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	[SerializeField] private GameState currentGameState;
	private int homeScore, awayScore;
	private Team lastScoringTeam;
	private float timer;
	private float maxTime = 3f * 60f;
	[SerializeField] private TextMeshProUGUI homeScoreText, awayScoreText, timerText;

	private void Awake() {
		if(Instance != null) {
			Destroy(this);
		} else {
			Instance = this;
		}
	}

	private void Start() {
		timer = maxTime;
		UpdateScoreBoard();
		SetGameState(GameState.Kickoff);
	}

	private void Update() {
		UpdateTimer();
	}

	public void Score(Team team) {
		if(currentGameState != GameState.Playing) {
			return;
		}
		SetGameState(GameState.Goal);
		if (team == Team.Home) {
			homeScore++;
		} else {
			awayScore++;
		}
		UpdateScoreBoard();
	}

	private void UpdateScoreBoard() {
		homeScoreText.text = homeScore.ToString("00");
		awayScoreText.text = awayScore.ToString("00");
	}

	private void UpdateTimer() {
		if(currentGameState != GameState.Playing) {
			return;
		}
		timer -= Time.deltaTime;
		TimeSpan time = TimeSpan.FromSeconds(timer);
		string formattedTime = string.Format("{0}:{1:00}", (int)time.TotalMinutes, time.Seconds);
		timerText.text = formattedTime;
		if(timer >= maxTime) {
			SetGameState(GameState.End);
		}
	}

	[SerializeField] private Ball ball;

	public PlayerController GetBallCarrier() {
		return ball.GetBallCarrier();
	}

	public void SetBallCarrier(PlayerController _value) {
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
}
