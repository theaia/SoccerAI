using UnityEngine;
using System.Collections;
// Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
// This line should always be present at the top of scripts which use pathfinding
using Pathfinding;

public class AstarAI : MonoBehaviour {
   public Transform targetPosition;
   private bool shouldMove;
   Seeker seeker;
    AIPath path;
    Player player;

    public void Start () {
        // Get a reference to the Seeker component we added earlier
        seeker = GetComponent<Seeker>();
        path = GetComponent<AIPath>();
        player = GetComponent<Player>();


        // Start to calculate a new path to the targetPosition object, return the result to the OnPathComplete method.
        // Path requests are asynchronous, so when the OnPathComplete method is called depends on how long it
        // takes to calculate the path. Usually it is called the next frame.
        seeker.StartPath(transform.position, targetPosition.position, OnPathComplete);
    }

    private void Update() {
        // Check if the left mouse button was clicked and call the conversion function
        if (Input.GetMouseButtonDown(0)) {
            targetPosition.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

		if (shouldMove) {
            Vector2 dir = path.steeringTarget;
            player.SetMovement(ConvertDirToInput(dir.normalized));
		}
    }

    public void SetDesintation(Vector2 _desintation) {
        seeker.StartPath(transform.position, targetPosition.position, OnPathComplete);
    }

    public void OnPathComplete (Path p) {
      Debug.Log("Yay, we got a path back. Did it have an error? " + p.error);
      shouldMove = false;
    }

    public Vector2 ConvertDirToInput(Vector2 normalizedDirection) {
        Vector2 closestDirection = Vector2.zero;

        closestDirection.x = Mathf.Round(normalizedDirection.x);
        closestDirection.y = Mathf.Round(normalizedDirection.y);

        closestDirection.x = Mathf.Clamp(closestDirection.x, -1, 1);
        closestDirection.y = Mathf.Clamp(closestDirection.y, -1, 1);

        return closestDirection;
    }
}