using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuraChecker : MonoBehaviour
{	
	private PlayerController playerController;

	private void Awake() {
		playerController = GetComponentInParent<PlayerController>();
	}
	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("ball")) {
			playerController.SetBallAround(true);
		}
	}

	private void OnTriggerExit2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("ball")) {
			playerController.SetBallAround(false);
		}
	}
}
