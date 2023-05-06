using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team {
    Home,
    Away
}

public class Goal : MonoBehaviour
{
    [SerializeField] private Team team;

	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.CompareTag("ball")) {
			GameManager.Instance.Score(team == Team.Home ? Team.Away : Team.Home);
		}
	}
}
