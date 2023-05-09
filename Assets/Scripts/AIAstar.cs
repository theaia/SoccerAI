using UnityEngine;
using System.Collections;
// Note this line, if it is left out, the script won't know that the class 'Path' exists and it will throw compiler errors
// This line should always be present at the top of scripts which use pathfinding
using Pathfinding;
using Pathfinding.RVO;

public class AIAstar : MonoBehaviour {
    private Vector3 targetPosition;
    private bool positionHasBeenSet;
    Seeker seeker;
    Player player;
    Path path;

    private float repathRate = 10f;
    private float lastRepath = float.NegativeInfinity;

    int currentWaypoint = 0;
    [HideInInspector] public bool reachedEndOfPath;
    private float nextWaypointDistance = .01f;

    public void Start() {
        // Get a reference to the Seeker component we added earlier
        seeker = GetComponent<Seeker>();
        player = GetComponent<Player>();
    }

    public void SetTarget(Vector2 _targetPosition) {
        if(Vector2.Distance(_targetPosition, targetPosition) < .01f) {
            return;
		}
        targetPosition = _targetPosition;
        positionHasBeenSet = true;
        SetDesintation(targetPosition);
    }

	private void Update() {
		if (!positionHasBeenSet) {
            return;
		}
        ProcessPath();

        if (!reachedEndOfPath && path != null) {
            Vector3 _target = path.vectorPath[currentWaypoint];
            Vector3 dir = (_target - transform.position).normalized;
            dir = Utils.DirToClosestInput(dir);
            //Debug.DrawLine(_target, _target + Vector3.left * .1f, Color.cyan);
            //Debug.Log(dir);
            player.SetMovement(dir);
        } else {
            player.SetMovement(Vector2.zero);
        }
    }

    public void Repath() {
        Debug.Log("Repath");
        SetDesintation(targetPosition);
    }

	private void ProcessPath() {
        if (Time.time > lastRepath + repathRate && seeker.IsDone()) {
            lastRepath = Time.time;

            // Start a new path to the targetPosition, call the the OnPathComplete function
            // when the path has been calculated (which may take a few frames depending on the complexity)
            seeker.StartPath(transform.position, targetPosition, OnPathComplete);
        }

        if (path == null) {
            // We have no path to follow yet, so don't do anything
            return;
        }

        // Check in a loop if we are close enough to the current waypoint to switch to the next one.
        // We do this in a loop because many waypoints might be close to each other and we may reach
        // several of them in the same frame.
        reachedEndOfPath = false;
        // The distance to the next waypoint in the path
        float distanceToWaypoint;
        while (true) {
            distanceToWaypoint = Vector3.Distance(transform.position, path.vectorPath[currentWaypoint]);
            if (distanceToWaypoint < nextWaypointDistance) {
                // Check if there is another waypoint or if we have reached the end of the path
                if (currentWaypoint + 1 < path.vectorPath.Count) {
                    currentWaypoint++;
                } else {
                    // Set a status variable to indicate that the agent has reached the end of the path.
                    // You can use this to trigger some special code if your game requires that.
                    reachedEndOfPath = true;
                    break;
                }
            } else {
                break;
            }
        }

        // Direction to the next waypoint
        // Normalize it so that it has a length of 1 world unit
    }

    private void SetDesintation(Vector2 _desintation) {
        seeker.StartPath(transform.position, targetPosition, OnPathComplete);
    }

    public void SprintUntilTargetReached(Vector2 _target) {
        StopAllCoroutines();
        StartCoroutine(SprintUntilTarget(_target));
    }

    IEnumerator SprintUntilTarget(Vector2 _target) {
        SetTarget(_target);
        while (!reachedEndOfPath) {
            player.SetSprint(true);
            yield return null;
        }
        player.SetSprint(false);
    }

    public void OnPathComplete(Path p) {
        //Debug.Log("A path was calculated. Did it fail with an error? " + p.error);

        // Path pooling. To avoid unnecessary allocations paths are reference counted.
        // Calling Claim will increase the reference count by 1 and Release will reduce
        // it by one, when it reaches zero the path will be pooled and then it may be used
        // by other scripts. The ABPath.Construct and Seeker.StartPath methods will
        // take a path from the pool if possible. See also the documentation page about path pooling.
        p.Claim(this);
        if (!p.error) {
            if (path != null) path.Release(this);
            path = p;
            // Reset the waypoint counter so that we start to move towards the first point in the path
            currentWaypoint = 0;
        } else {
            p.Release(this);
        }
    }
}