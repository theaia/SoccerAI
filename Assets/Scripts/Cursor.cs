using UnityEngine;

public class Cursor : MonoBehaviour
{
	private void Update() {
		if (GameManager.Instance.GetBallCarrier()) {
			transform.position = GameManager.Instance.GetBallCarrier().transform.position;
			transform.GetChild(0).gameObject.SetActive(true);
		} else {
			transform.GetChild(0).gameObject.SetActive(false);
		}
	}
}
