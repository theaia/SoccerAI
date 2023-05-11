using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRay : MonoBehaviour
{
	public LayerMask mask;
	public float width;
	public float distance;
	private void FixedUpdate() {
		RaycastHit2D[] _hits = Physics2D.CircleCastAll(transform.position, width, transform.up, distance, mask);
		Debug.Log(_hits.Length);
		foreach(RaycastHit2D hit in _hits) {
			Debug.DrawLine(transform.position, hit.point);
		}
	}
}
