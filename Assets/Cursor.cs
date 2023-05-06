using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
	private void Update() {
		if (GameManager.Instance.GetBallCarrier()) { 
			transform.position = GameManager.Instance.GetBallCarrier().transform.position; 
		} else {
			gameObject.SetActive(false);
		}
	}
}
