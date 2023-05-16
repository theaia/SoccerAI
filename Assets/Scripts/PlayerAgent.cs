using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.MLAgents.Policies;

public enum AgentMode {
    Training,
    Inferencing
}

public class PlayerAgent : Agent {
    #region Training Modes
    [Tooltip("Are we training the agent or is the agent production ready?")]
    public AgentMode Mode = AgentMode.Training;
    private BehaviorParameters behaviorParameters;
    public ActionSegment<int> DiscreteActions { get; }
    #endregion


    #region Rewards
    [Header("Rewards"), Tooltip("What penatly is given when the agent crashes?")]
    [SerializeField] float HasBallReward;
    [SerializeField] float ShootReward;
    [SerializeField] float PassReward;
    [SerializeField] float GoalReward;
    [SerializeField] float TackleReward;
    [SerializeField] float LooseBallReward;
    [SerializeField] float ClumpingPenalty;
    [SerializeField] float GiveawayPenalty;
    [SerializeField] float OwnGoalPenalty;
    [SerializeField] float TackledPenalty;
    [SerializeField] float FriendlyTacklePenalty;
    [SerializeField] float TimeoutPenalty;

    [SerializeField] float Timeout;
    float timer;
    #endregion

    Player m_PlayerController;
    [HideInInspector] public DecisionRequester m_DecisionRequester;

    void Awake() {
        m_PlayerController = GetComponent<Player>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        m_DecisionRequester = GetComponent<DecisionRequester>();
    }

	private void Update() {
        timer += Time.deltaTime;
        if (timer > Timeout) {
            //AddReward(TimeoutPenalty);
            EndEpisode();
        }
    }

	public override void OnEpisodeBegin() {
        transform.position = m_PlayerController.GetFormationLocation();
        timer = 0;
	}


    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(new Vector2(transform.position.x, transform.position.y));
        sensor.AddObservation(GameManager.Instance.GetBallLocation());

        sensor.AddObservation(GameManager.Instance.GetBallCarrier() == m_PlayerController);
        sensor.AddObservation(ConvertPlayerTeamToInt(m_PlayerController));
        sensor.AddObservation(ConvertPlayerTeamToInt(GameManager.Instance.GetBallCarrier()));

        sensor.AddObservation(GameManager.Instance.GetBallCarrier() ? GameManager.Instance.GetBallCarrier().GetCurrentStamina() : -1f);
        sensor.AddObservation(m_PlayerController.GetCurrentStamina());
        sensor.AddObservation(m_PlayerController.GetShotCharge());
        //sensor.AddObservation(m_PlayerController.GetVelocityMagnitude());


    }

    private int ConvertPlayerTeamToInt(Player _ballCarrier) {
        int _teamInt = 0;

        if (_ballCarrier != null) {
            _teamInt = _ballCarrier.GetTeam() == Team.Home ? 1 : 2;
        }

        return _teamInt;
    }

    private void ProcessRewards() {
        if(GameManager.Instance.GetBallCarrier() == m_PlayerController) {
            AddReward(HasBallReward);
        }
	}

    public void ScorerReward() {
        Debug.Log($"{gameObject.name} SCORED!");
        AddReward(GoalReward);
        EndEpisode();
    }

    public void OwnGoalerPenalty() {
        Debug.Log($"{gameObject.name} just own goaled");
        AddReward(OwnGoalPenalty);
    }

    public void GotLooseBallReward() {
        Debug.Log($"{gameObject.name} picked up a loose ball!");
        AddReward(LooseBallReward);
    }

    public void PasserReward() {
        Debug.Log($"{gameObject.name} completed a successful pass");
        //AddReward(PassReward);
    }

    public void TacklerReward() {
        Debug.Log($"{gameObject.name} successfully tackled");
        //AddReward(TackleReward);
    }

    public void ShooterReward(float _shotForce) {
        if (_shotForce < 10f) {
            return;
        }
        Debug.Log($"{gameObject.name} attempted a shot or pass of {_shotForce} speed");
        //AddReward(ShootReward * _shotForce);
    }

    public void ShooterOnNetReward() {

    }

    public void TackleePenalty() {
        //Debug.Log($"{gameObject.name} was tackled");
        //AddReward(TackledPenalty);
    }

    public void FriendlyTacklerPenalty() {
        //Debug.Log($"{gameObject.name} stole the ball from their own teammate. Oof.");
        //AddReward(FriendlyTacklePenalty);
    }

    public void ClumpPenalty() {
        AddReward(ClumpingPenalty);
    }

    public void GiveawayerPenalty() {
        //Debug.Log($"{gameObject.name} gave away the ball");
        //AddReward(GiveawayPenalty);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (GameManager.Instance.GetIsTransitioning()) {
            return;
        }
        int _horMoveDir = actions.DiscreteActions[0]; // 0 = none // 1 = right // 2 = left
        int _vertMoveDir = actions.DiscreteActions[1]; // 0 = none // 1 = up // 2 = down
        int _ability = actions.DiscreteActions[2]; // 0 = off // 1 = on
        int _sprint = actions.DiscreteActions[3]; // 0 = off // 1 = on

        //Debug.Log($"{_horMoveDir} {_vertMoveDir} {_ability} {_sprint}");

        if (_horMoveDir == 2) {
            _horMoveDir = -1;
        }

        if (_vertMoveDir == 2) {
            _vertMoveDir = -1;
        }

        m_PlayerController.SetMovement(new Vector2(_horMoveDir, _vertMoveDir));
        m_PlayerController.SetAbility(_ability == 1 ? true : false);
        m_PlayerController.SetSprint(_sprint == 1 ? true : false);
        ProcessRewards();
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        if (GameManager.Instance.GetIsTransitioning()) {
            return;
        }
        Debug.Log("Running Heuristic movement");
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        int _horMoveDir = (int)Input.GetAxis("Horizontal");
        int _vertMoveDir = (int)Input.GetAxis("Vertical");
        int _ability = Input.GetButton("Ability") ? 1 : 0;
        int _sprint = Input.GetButton("Sprint") ? 1 : 0;

        if(_horMoveDir > 0) {
            _horMoveDir = 1;
        } else if (_horMoveDir < 0) {
            _horMoveDir = 2;
        }

        if (_vertMoveDir > 0) {
            _vertMoveDir = 1;
        } else if (_vertMoveDir < 0) {
            _vertMoveDir = 2;
        }

        discreteActions[0] = _horMoveDir; //horizontal
        discreteActions[1] = _vertMoveDir; //horizontal
        discreteActions[2] = _ability; //horizontal
        discreteActions[3] = _sprint; //horizontal
    }

}