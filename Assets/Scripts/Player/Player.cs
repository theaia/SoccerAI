using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum Role {
    Striker,
    Playmaker,
    Defender,
    Goalie,
    Human
}

public class Player : NetworkBehaviour {
    private InputData inputData = new InputData() {
        Move = Vector2.zero,
        Sprint = false,
        Ability = false
    };

    private KeyboardInput[] m_Inputs;
    private List<AnimController> animController = new List<AnimController>();

    private Rigidbody2D rb;
    private bool hasBall;
    private float stamina;
    private float speed = 1f;
    private float shotChargeTime;
    private bool isBallNearby;
    private string lastCollidedObjectTagged;
    private Vector2 formationLocation;

    [SerializeField] private Country country;
    [SerializeField] private Team team;
    [SerializeField] private Role role;
    
    private string skin = "-1";

    private PlayerAgent agent;
    private AIAstar aiaStar;
    private LocalPerception localPerception;
    private CharacterCustomization characterCustomization;

    private bool canShoot = true;
    private bool isCheering = false;

    [HideInInspector] public Vector2 targetBallPosition;
    [SerializeField] private SpriteRenderer chargeSprite, staminaSprite;
    [HideInInspector] public bool m_CanControl;
    [HideInInspector] public bool IsTransitioning;

    private readonly NetworkVariable<PlayerNetworkData>
        netState = new(writePerm: NetworkVariableWritePermission.Owner);
    
    private void Awake() {
        if(GameManager.Instance && GameManager.Instance.GetGameMode() == GameMode.SinglePlayer) SetTeam(team);
        
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<PlayerAgent>();
        aiaStar = GetComponent<AIAstar>();
        localPerception = GetComponentInChildren<LocalPerception>();
        m_Inputs = GetComponents<KeyboardInput>();
        
        characterCustomization = GetComponentInChildren<CharacterCustomization>();
    }

    public PlayerAgent GetAgent() {
        return agent;
    }

    public AIAstar GetAIAStar() {
        return aiaStar;
    }

    public float GetSpeed() {
        return speed;
    }

    public Country GetCountry() {
        return country;
    }

    public bool GetIsBallNearby() {
        return isBallNearby;
    }

    public void SetCosmetics(string _skinId, string _countryId, int _teamInt) {
        if(characterCustomization == null) characterCustomization = GetComponentInChildren<CharacterCustomization>();
        characterCustomization.SetCosmetics(_skinId, _countryId, _teamInt);
    }
    
    public string GetSkin() {
        return skin;
    }
    
    public void SetSkin(string _value) {
        skin = _value;
    }

    public void SetIsCheering(bool _value) {
        isCheering = _value;
    }

    public void Reset() {
        if(rb) rb.velocity = Vector2.zero;
        inputData = new InputData() {
            Move = Vector2.zero,
            Sprint = false,
            Ability = false
        };
        if(aiaStar) aiaStar.Reset();
        canShoot = true;
        stamina = GameManager.Instance.maxStamina;
        shotChargeTime = 0f;
        speed = GameManager.Instance.standardSpeed;
        isBallNearby = false;
        isCheering = false;
        SetHasBall(false);
    }

    public void AddAnimController(AnimController _animController) {
        animController.Add(_animController);
    }

    public float GetDistanceToBall() {
        return Vector2.Distance(transform.position, GameManager.Instance.GetBallLocation());
    }



    IEnumerator Start() {
        yield return new WaitUntil(() => GameManager.Instance);
        gameObject.name = GameManager.Instance.GetRandomName();
        stamina = GameManager.Instance.maxStamina;
        UpdateChargeDisplay();
        UpdateStaminaDisplay();
    }

    private void OnEnable() {
        transform.position = formationLocation;
    }

    public void SetTeam(Team _team) {
        team = _team;
        gameObject.tag = team == Team.Home ? "home" : "away";
        GameManager.Instance.AddPlayer(this, team);
    }

    public string[] GetLocalPerception() {
        return localPerception.GetLocalPerceptionInfo();
    }

    public string GetLocalDirPerception(int _dir) {
        return localPerception.GetLocalPerceptionDirInfo(_dir);
    }

    public void SetHasBall(bool _value) {
        if (!GameManager.Instance) return;
        hasBall = _value;
        gameObject.layer = hasBall ? LayerMask.NameToLayer("BallCarrier") : LayerMask.NameToLayer("Player");
        if (!hasBall) {
            UpdateChargeDisplay();
            canShoot = false;
            Invoke("ResetCanShootTimer", GameManager.Instance.ShotCooldown);
        }
    }

    public void SetFormationLocation(Vector2 _value) {
        Debug.Log($"Formation on {OwnerClientId} set to {_value}");
        formationLocation = _value;
    }

    public Vector2 GetFormationLocation() {
        return formationLocation;
    }

    public void SetMovement(Vector2 _movement) {
        inputData.Move = _movement;
    }

    public void SetAbility(bool _ability) {
        inputData.Ability = _ability;
    }

    public void SetSprint(bool _sprint) {
        inputData.Sprint = _sprint;
    }

    public void SetRole(Role _role) {
        role = _role;
    }

    public void Pass() {
        if (agent) {
            agent.PasserReward();
        }

    }

    public void Giveaway() {
        if (agent) {
            agent.GiveawayerPenalty();
        }
    }

    public Role GetRole() {
        return role;
    }

    public void TackleGiveaway() {
        if (agent) {
            agent.TackleePenalty();
        }
    }

    public void Tackle() {
        if (agent) {
            agent.TacklerReward();
        }
    }

    public void FriendlyTackle() {
        if (agent) {
            agent.FriendlyTacklerPenalty();
        }
    }

    public float GetShotCharge() {
        return shotChargeTime;
    }

    public Team GetTeam() {
        return team;
    }

    public Vector2 GetCurrentMoveInput() {
        return inputData.Move;
    }

    public void GenerateNewInputs(Vector2 _moveDir, bool _ability, bool _sprint) {
        inputData = new InputData {
            Move = _moveDir,
            Ability = _ability,
            Sprint = _sprint,
        };
    }

    public void ProcessInputs(Vector2 _moveDir, bool _ability, bool _sprint, bool _isNetState = false) {
        if(!GameManager.Instance) {
            return;
        }
        //Debug.Log("processing inputs");
        Animate(_moveDir, isCheering);

        CheckOutOfBounds();

        if (!GameManager.Instance.GetCanMove()) {
            //Debug.Log("Can't process input.  Player can't move");
            return;
        }
        

        _moveDir = Utils.V2ToClosestInput(_moveDir).normalized;
        //Debug.Log($"{gameObject.name} current move input: {_moveDir}");
        Vector3 _velocity = speed * _moveDir;
        if(!_isNetState) rb.velocity = _velocity;
        //Debug.Log($"{gameObject.name} current move input: {_moveDir}.  Calc'd velocity: {_velocity}");

        if (_ability && canShoot) {
            if (this == GameManager.Instance.GetBallCarrier()) {
                ChargeShot();
            }
            else if (isBallNearby) {
                Player _ballCarrier = GameManager.Instance.GetBallCarrier();

                #region Tackle

                if (_ballCarrier) {
                    float _ballCarrierStamina = _ballCarrier.GetCurrentStamina();
                    float _currentPlayerStamina = GetCurrentStamina();
                    //Lose a tackle if you have less stamina than the ball carrier
                    if (_ballCarrierStamina > _currentPlayerStamina) {
                        _ballCarrier.ConsumeStamina(_currentPlayerStamina);
                        ConsumeStamina(_currentPlayerStamina);
                        return;
                    }
                    else {
                        _ballCarrier.ConsumeStamina(_ballCarrierStamina);
                        ConsumeStamina(_ballCarrierStamina);
                    }
                }

                #endregion

                GameManager.Instance.SetBallCarrier(this);
                shotChargeTime = 0;
                UpdateChargeDisplay();
            }
        }
        else if (shotChargeTime > GameManager.Instance.minChargeForShot) {
            Shoot();
        }

        if (_sprint && stamina > GameManager.Instance.staminaConsumeRate) {
            if (speed == GameManager.Instance.standardSpeed) {
                StopCoroutine(FadeToSprintSpeed(GameManager.Instance.speedLerp));
                StartCoroutine(FadeToSprintSpeed(GameManager.Instance.speedLerp));
            }
            
            ConsumeStamina(GameManager.Instance.staminaConsumeRate);
        }
        else if (speed != GameManager.Instance.standardSpeed) {
            StopCoroutine(FadeToSprintSpeed(GameManager.Instance.speedLerp));
            speed = GameManager.Instance.standardSpeed;
        }
    }

    IEnumerator FadeToSprintSpeed(float _duration) {
        float time = 0;
        while (time < _duration) {
            time += Time.deltaTime;
            speed = Mathf.Lerp(GameManager.Instance.standardSpeed, GameManager.Instance.sprintSpeed, time / _duration);
            yield return null;
        }

        speed = GameManager.Instance.sprintSpeed;
    }

    public void ConsumeStamina(float _value) {
        CancelInvoke("RechargeStamina");
        stamina = Mathf.Clamp(stamina - _value, 0, GameManager.Instance.maxStamina);
        UpdateStaminaDisplay();
        InvokeRepeating("RechargeStamina", GameManager.Instance.staminaRechargeDelay, .01f);
    }

    private void RechargeStamina() {
        stamina = Mathf.Clamp(stamina + GameManager.Instance.staminaRechargeRate, 0, GameManager.Instance.maxStamina);
        UpdateStaminaDisplay();
        if (stamina >= GameManager.Instance.maxStamina) {
            CancelInvoke("RechargeStamina");
        }
    }

    private void UpdateStaminaDisplay() {
        CancelInvoke("DisableStaminaDisplay");
        staminaSprite.transform.parent.gameObject.SetActive(true);
        float _chargePct = Mathf.Clamp01(stamina / GameManager.Instance.maxStamina);
        float _lerp = Mathf.Lerp(0, 1, _chargePct);

        staminaSprite.transform.localScale = new Vector3(_lerp, staminaSprite.transform.localScale.y,
            staminaSprite.transform.localScale.z);
        Invoke("DisableStaminaDisplay", GameManager.Instance.staminaDisplayTime);
    }

    private void DisableStaminaDisplay() {
        staminaSprite.transform.parent.gameObject.SetActive(false);
    }

    public float GetCurrentStamina() {
        return stamina;
    }

    private void ChargeShot() {
        shotChargeTime = Mathf.Clamp(shotChargeTime + GameManager.Instance.shotChargeRate, 0f,
            GameManager.Instance.maxChargeTime);
        if (shotChargeTime > GameManager.Instance.minChargeForShot) {
            UpdateChargeDisplay();
        }
    }

    private void Shoot() {
        if (this == GameManager.Instance.GetBallCarrier()) {
            Vector2 _shot = GetDirection() * Mathf.Lerp(GameManager.Instance.ShotMinStrength,
                GameManager.Instance.ShotMaxStrength, shotChargeTime / GameManager.Instance.maxChargeTime);
            GameManager.Instance.ShootBall(_shot);
        }

        shotChargeTime = 0;
        UpdateChargeDisplay();
    }

    private void ResetCanShootTimer() {
        canShoot = true;
    }

    public bool GetCanShoot() {
        return canShoot;
    }

    private Vector2 GetDirection() {
        State _currentState = animController[0].GetAnimState();
        Vector2 direction;
        switch (_currentState) {
            default:
                direction = Vector2.zero;
                break;
            case State.up:
                direction = Vector2.up;
                break;
            case State.rightup:
                direction = new Vector2(0.707f, 0.707f);
                break;
            case State.right:
                direction = Vector2.right;
                break;
            case State.rightdown:
                direction = new Vector2(0.707f, -0.707f);
                ;
                break;
            case State.down:
                direction = Vector2.down;
                break;
            case State.leftdown:
                direction = new Vector2(-0.707f, -0.707f);
                ;
                break;
            case State.left:
                direction = Vector2.left;
                break;
            case State.leftup:
                direction = new Vector2(-0.707f, 0.707f);
                ;
                break;
        }

        return direction;
    }

    private void LateUpdate() {
        if (IsOwner) {
            //Debug.Log("IsOwner.  Processing Inputs.");
            if (GameManager.Instance.GetIsTransitioning()) {
                aiaStar.SetTarget(GetFormationLocation());
                //Debug.Log("Game is transitioning.  Setting to target.");
            } else if (GetRole() == Role.Human && GameManager.Instance.GetCanMove() &&
                       !GameManager.Instance.GetIsTransitioning()) {
                //Debug.Log($"Can move: {GameManager.Instance.GetCanMove()} IsTransitioning: {GameManager.Instance.GetIsTransitioning()}");
                for (int i = 0; i < m_Inputs.Length; i++) {
                    inputData = m_Inputs[i].GenerateInput();
                }
            }

            ProcessInputs(inputData.Move, inputData.Ability, inputData.Sprint);
            
            netState.Value = new PlayerNetworkData() {
                Move = inputData.Move,
                HasBall = hasBall,
                Charge = shotChargeTime,
                IsCharging = inputData.Ability,
                Stamina = stamina,
                IsSprinting = inputData.Sprint,
            };
            //Debug.Log($"Sending New move data.  {netState.Value.Move}");
        } else {
            //Debug.Log($"Processing Net state. Input {netState.Value.Move}");
            ProcessInputs(netState.Value.Move, netState.Value.IsCharging, netState.Value.IsSprinting, true);
            SetHasBall(netState.Value.HasBall);
            stamina = netState.Value.Stamina;
            shotChargeTime = netState.Value.Charge;
        }
    }

    private void UpdateChargeDisplay() {
        if (!GameManager.Instance) {
            return;
        }
        float _chargePct = Mathf.Clamp01(shotChargeTime / GameManager.Instance.maxChargeTime);
        float _lerp = Mathf.Lerp(0, 1, _chargePct);

        chargeSprite.transform.localScale = new Vector3(_lerp, chargeSprite.transform.localScale.y,
            chargeSprite.transform.localScale.z);
        chargeSprite.transform.parent.gameObject.SetActive(shotChargeTime > 0 && hasBall);
    }

    public void SetBallAround(bool _value) {
        isBallNearby = _value;
    }

    public float GetVelocityMagnitude() {
        return rb.velocity.magnitude;
    }

    private void Animate(Vector2 input, bool _cheering = false) {
        if (!GameManager.Instance) {
            return;
        }
        
        State _currentState;
        if (_cheering) {
            _currentState = State.cheer;
        }
        else if (!GameManager.Instance.GetCanMove()) {
            _currentState = State.idle;
        }
        else {
            _currentState = Utils.GetState(Utils.V2ToClosestInput(input));
            if (hasBall) {
                targetBallPosition = Utils.GetTargetBallPosFromState(_currentState);
            }
        }

        foreach (AnimController _anim in animController) {
            _anim.SetAnimState(_currentState);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision) {
        lastCollidedObjectTagged = collision.gameObject.tag;
    }

    public void ClearLastCollidedObject() {
        lastCollidedObjectTagged = string.Empty;
    }

    public string GetLastCollidedObject() {
        return lastCollidedObjectTagged;
    }

    private void CheckOutOfBounds() {
        if(!GameManager.Instance){
            return;
        }
        if (transform.position.x < GameManager.Instance.ArenaWidth.x ||
            transform.position.x > GameManager.Instance.ArenaWidth.y ||
            transform.position.y < GameManager.Instance.ArenaHeight.x ||
            transform.position.y > GameManager.Instance.ArenaHeight.y) {
            transform.position = formationLocation;
        }
    }

    private void OnDestroy() {
        GameManager.Instance.RemovePlayer(this);
    }

    public void OnNetworkSpawn() {
        if (!IsOwner) Destroy(this);
    }

    struct PlayerNetworkData : INetworkSerializable {
        private float moveX;
        private float moveY;
        private bool hasBall;
        private float speed;
        private byte charge;
        private bool isCharging;
        private byte stamina;
        private bool isSprinting;

        internal Vector2 Move {
            get => new Vector2(moveX, moveY);
            set {
                moveX = value.x;
                moveY = value.y;
            }
        }
        internal bool HasBall {
            get => hasBall;
            set { hasBall = value; }
        }
        
        internal float Speed {
            get => speed;
            set { speed = value; }
        }
        internal float Charge {
            get => charge;
            set { charge = (byte)value; }
        }
        
        internal bool IsCharging {
            get => isCharging;
            set { isCharging = value; }
        }

        internal float Stamina {
            get => stamina;
            set { stamina = (byte)value; }
        }

        internal bool IsSprinting {
            get => isSprinting;
            set { isSprinting = value; }
        }
        

        public void NetworkSerialize<T>(BufferSerializer<T> _serializer) where T : IReaderWriter {
            _serializer.SerializeValue(ref moveX);
            _serializer.SerializeValue(ref moveY);
            _serializer.SerializeValue(ref hasBall);
            _serializer.SerializeValue(ref speed);
            _serializer.SerializeValue(ref charge);
            _serializer.SerializeValue(ref isCharging);
            _serializer.SerializeValue(ref stamina);
            _serializer.SerializeValue(ref isSprinting);
        }
    }
}