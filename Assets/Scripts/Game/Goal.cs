using UnityEngine;

public enum Team {
	Spectator,
    Home,
    Away
}

public class Goal : MonoBehaviour
{
    [SerializeField] private Team team;
	[SerializeField] private Transform post0, post1;

	

	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.CompareTag("ball")) {
			GameManager.Instance.Score(team == Team.Home ? Team.Away : Team.Home);
		}
	}

	private void Start() {
		GameManager.Instance.SetPosts(team, new Vector2[]{post0.position, post1.position});
	}
}
