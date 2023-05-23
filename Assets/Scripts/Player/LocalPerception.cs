using UnityEngine;

public class LocalPerception : MonoBehaviour {
	private string[] currentCollisionInfo;

	public string GetLocalPerceptionDirInfo(int _dir) {
		if(_dir < 0 || _dir > currentCollisionInfo.Length - 1) {
			Debug.Log("No valid info found for _dir");
			return string.Empty;
		}

		return currentCollisionInfo[_dir];
	}

	public string[] GetLocalPerceptionInfo() {
		return currentCollisionInfo;
	}

	private void FixedUpdate() {
		if (!GameManager.Instance) {
			return;
		}
		string[] _newCollisionInfo = new string[8];
		for (int i = 0; i < _newCollisionInfo.Length; i++) {
			Vector2 _input = Utils.IntDirToInput(i);
			//Debug.DrawRay(_origin, _input, Color.red);
			Vector2 _origin = transform.position;
			//Debug.DrawLine(_origin, _origin + (_input * 1f), Color.white, .1f);
			RaycastHit2D _hit = Physics2D.Raycast(_origin, _input, GameManager.Instance.PerceptionLength, GameManager.Instance.DirectionLayersToCheck);
			if (_hit.collider) {
				//Debug.DrawLine(_origin, _hit.point, Color.red, .1f);
				_newCollisionInfo[i] = _hit.collider.tag;
			} else {
				//Debug.DrawLine(_origin, _origin + (_input * 1f), Color.white, .1f);
				_newCollisionInfo[i] = string.Empty;
			}
		}
		currentCollisionInfo = _newCollisionInfo;
	}

}
