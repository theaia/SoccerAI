using UnityEngine;
using Pathfinding;

public class AIAstar : MonoBehaviour {
    private Vector2? targetPosition = null;
    Seeker seeker;
    Player player;
    Path path;

    private float repathRate = 10f;
    private float lastRepath = float.NegativeInfinity;
    Vector2? lastMove = null;

    int currentWaypoint = 0;
    [HideInInspector] public bool reachedEndOfPath;
    [SerializeField] private float nextWaypointDistance = .02f;

    public void Start() {
        seeker = GetComponent<Seeker>();
        player = GetComponent<Player>();
    }

    public void CancelCurrentPath() {
        targetPosition = null;
	}

    public void SetTarget(Vector2 _targetPosition) {
        //Debug.Log($"Setting transition to: {_targetPosition}");
        if(GameManager.Instance && !GameManager.Instance.GetIsTransitioning() && targetPosition.HasValue && Vector2.Distance(_targetPosition, targetPosition.Value) < .04f) {
            //Debug.Log($"Exiting Set target because target: {targetPosition.HasValue} && target disance to existing is less than .01");
            return;
		}

        if(_targetPosition == targetPosition) {
            return;
		}

        if(GameManager.Instance && GameManager.Instance.GetIsTransitioning()) {
            //Debug.Log($"Setting interior transition to: {player.GetFormationLocation()}");
            if(player) _targetPosition = player.GetFormationLocation();
        }

        targetPosition = _targetPosition;
        //Debug.Log($"Target position for {gameObject.name} on {player.GetTeam()} set to {targetPosition}");
        GoToTargetPosition();
    }


	public void Reset() {
        targetPosition = null;
    }

	private void Update() {
		if (targetPosition == null || !GameManager.Instance.GetCanMove()) {
            return;
		}
        ProcessPath();
        if(transform)
        if (!reachedEndOfPath && path != null) {
            Vector3 _target = path.vectorPath[currentWaypoint];
            Vector3 dir = _target - transform.position;
            //Debug.Log($"Raw Dir: {dir}");
            dir = Utils.V2ToClosestInput(dir);
            //Debug.DrawLine(_target, _target + dir * .1f, Color.cyan);
            //Debug.Log($"{player.name} | Target: {_target} | Transform: {transform.position} | Dir: {dir}");
            //Debug.Log(dir);
            lastMove = dir;
            player.SetMovement(dir);
        } else {
            lastMove = Vector2.zero;
            player.SetMovement(Vector2.zero);
        }
    }

    public void Repath() {
        GoToTargetPosition();
    }

	private void ProcessPath() {
		if (!seeker) {
            return;
		}

        /*if (Time.time > lastRepath + repathRate && seeker.IsDone()) {
            lastRepath = Time.time;

            // Start a new path to the targetPosition, call the the OnPathComplete function
            // when the path has been calculated (which may take a few frames depending on the complexity)
            seeker.StartPath(transform.position, targetPosition.Value, OnPathComplete);
        }*/

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
                    targetPosition = null;
                    break;
                }
            } else {
                break;
            }
        }

        // Direction to the next waypoint
        // Normalize it so that it has a length of 1 world unit
    }

    private void GoToTargetPosition() {
        if(seeker != null && targetPosition.HasValue) seeker.StartPath(transform.position, targetPosition.Value, OnPathComplete);
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