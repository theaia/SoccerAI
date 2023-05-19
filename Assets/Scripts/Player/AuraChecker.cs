using UnityEngine;

public class AuraChecker : MonoBehaviour
{	
	private Player playerController;
	private int teammatesNearby;

	private void Awake() {
		playerController = GetComponentInParent<Player>();
	}

	private void FixedUpdate() {
		if(teammatesNearby > 0) {
			PlayerAgent _agent = playerController.GetAgent();
			if (_agent) _agent.ClumpPenalty();
		}
	}
	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("ball")) {
			playerController.SetBallAround(true);
		}

		if (GameManager.Instance.GetGameState() == GameState.Training && collision.gameObject.tag == transform.parent.tag) {
			teammatesNearby++;
		}
	}

	private void OnTriggerExit2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("ball")) {
			playerController.SetBallAround(false);
		}

		if (GameManager.Instance.GetGameState() == GameState.Training && collision.gameObject.tag == transform.parent.tag) {
			teammatesNearby--;
		}
	}
}
