using System.Collections;
using UnityEngine;

public enum PlayStyle {
    Striker,
    Playmaker,
    Defender,
    Goalie
}

public class Player : MonoBehaviour
{
    private InputData inputData;
    private KeyboardInput[] m_Inputs;
    private AnimController animController;

    private Rigidbody2D rb;
    private bool hasBall;
    private float stamina;
    private float speed = 1f;
    private float shotChargeTime;
    private bool isBallNearby;
    private string lastCollidedObjectTagged;
    private Vector2 formationLocation;

    [SerializeField] private Team team;
    [SerializeField] private PlayStyle role;

    private PlayerAgent agent;
    private AIAstar aiaStar;
    private LocalPerception localPerception;

    private bool canShoot = true;

    [HideInInspector] public GameObject targetBallPosition;
    [SerializeField] private GameObject[] targetBallPositions;
    [SerializeField] private SpriteRenderer chargeSprite, staminaSprite;
    [HideInInspector] public bool m_CanControl;

    public PlayerAgent GetAgent() {
        return agent;
	}

    public AIAstar GetAIAStar() {
        return aiaStar;
    }

    public float GetSpeed() {
        return speed;
	}

    public bool GetIsBallNearby() {
        return isBallNearby;
    }

    public void Reset() {
        canShoot = true;
        stamina = GameManager.Instance.maxStamina;
        shotChargeTime = 0f;
        speed = GameManager.Instance.standardSpeed;
        isBallNearby = false;
        hasBall = false;
    }

    public void SetAnimController(AnimController _animController) {
        animController = _animController;
    }

    public float GetDistanceToBall() {
        return Vector2.Distance(transform.position, GameManager.Instance.GetBallLocation());
	}

	private void Awake() {
        SetTeam(team);
        m_Inputs = GetComponents<KeyboardInput>();
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<PlayerAgent>();
        aiaStar = GetComponent<AIAstar>();
        localPerception = GetComponentInChildren<LocalPerception>();

        gameObject.name = GameManager.Instance.GetRandomName();
        stamina = GameManager.Instance.maxStamina;
        UpdateChargeDisplay();
        UpdateStaminaDisplay();
    }

    public void SetTeam(Team _team) {
        team = _team;
        gameObject.tag = team == Team.Home ? "home" : "away";
        Instantiate(GameManager.Instance.GetRandomTeamPrefab(_team), transform);
        if (animController != null) {
            Destroy(animController.gameObject);
        }
        animController = GetComponentInChildren<AnimController>();
    }

    public string[] GetLocalPerception() {
        return localPerception.GetLocalPerceptionInfo();
    }

    public string GetLocalDirPerception(int _dir) {
        return localPerception.GetLocalPerceptionDirInfo(_dir);
    }
    public void SetHasBall(bool _value) {
        hasBall = _value;
        gameObject.layer = hasBall ? LayerMask.NameToLayer("BallCarrier") : LayerMask.NameToLayer("Player");
        if (!hasBall) {
            UpdateChargeDisplay();
            canShoot = false;
            Invoke("ResetCanShootTimer", GameManager.Instance.ShotCooldown);
        }
    }

    public void SetFormationLocation(Vector2 _value) {
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

    public void SetRole(PlayStyle _role) {
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

    public PlayStyle GetRole() {
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

    void FixedUpdate()
    {
        //Only do this if there are human inputs
		if (m_Inputs == null) {
            return;
		}
        for (int i = 0; i < m_Inputs.Length; i++) {
            inputData = m_Inputs[i].GenerateInput();
        }

        ProcessInputs(inputData.Move, inputData.Ability, inputData.Sprint);
    }

    public Vector2 GetCurrentMoveInput() {
        return inputData.Move;
	}

    public void GenerareNewInputs(Vector2 _moveDir, bool _ability, bool _sprint) {
        inputData = new InputData {
            Move = _moveDir,
            Ability = _ability,
            Sprint = _sprint,
        };
    }

    public void ProcessInputs(Vector2 _moveDir, bool _ability, bool _sprint) {
        Animate(GameManager.Instance.GetGameState() == GameState.Goal && GameManager.Instance.GetLastScoringTeam() == team);

		if (!GameManager.Instance.GetCanMove()) {
            return;
		}
        //Debug.Log($"{gameObject.name} current move input: {_moveDir}");
        rb.velocity = _moveDir.normalized * speed * Time.fixedDeltaTime;

        if (_ability && canShoot) {
            if (this == GameManager.Instance.GetBallCarrier()) {
                ChargeShot();
            } else if (isBallNearby) {
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
                    } else {
                        _ballCarrier.ConsumeStamina(_ballCarrierStamina);
                        ConsumeStamina(_ballCarrierStamina);
                    }
                }
                #endregion
                GameManager.Instance.SetBallCarrier(this);
                shotChargeTime = 0;
                UpdateChargeDisplay();
            }
        } else if (shotChargeTime > GameManager.Instance.minChargeForShot) {
            Shoot();
        }
        if (_sprint && stamina > GameManager.Instance.staminaConsumeRate) {
            if (speed == GameManager.Instance.standardSpeed) {
                StartCoroutine(FadeToSprintSpeed(GameManager.Instance.speedLerp));
            };
            ConsumeStamina(GameManager.Instance.staminaConsumeRate);
        } else if (speed != GameManager.Instance.standardSpeed) {
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

        staminaSprite.transform.localScale = new Vector3(_lerp, staminaSprite.transform.localScale.y, staminaSprite.transform.localScale.z);
        Invoke("DisableStaminaDisplay", GameManager.Instance.staminaDisplayTime);
    }

    private void DisableStaminaDisplay() {
        staminaSprite.transform.parent.gameObject.SetActive(false);
    }

    public float GetCurrentStamina() {
        return stamina;
    }

    private void ChargeShot() {
        shotChargeTime = Mathf.Clamp(shotChargeTime + GameManager.Instance.shotChargeRate, 0f , GameManager.Instance.maxChargeTime);
        if (shotChargeTime > GameManager.Instance.minChargeForShot) {
            UpdateChargeDisplay();
        }
    }

    private void Shoot() {
        if (this == GameManager.Instance.GetBallCarrier()) {
            Vector2 _shot = GetDirection() * Mathf.Lerp(GameManager.Instance.ShotMinStrength, GameManager.Instance.ShotMaxStrength, shotChargeTime / GameManager.Instance.maxChargeTime);
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
        State _currentState = animController.GetAnimState();
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
                direction = new Vector2(0.707f, -0.707f); ;
                break;
            case State.down:
                direction = Vector2.down;
                break;
            case State.leftdown:
                direction = new Vector2(-0.707f, -0.707f); ;
                break;
            case State.left:
                direction = Vector2.left;
                break;
            case State.leftup:
                direction = new Vector2(-0.707f, 0.707f); ;
                break;
        }

        return direction;
	}

    private void UpdateChargeDisplay() {
        float _chargePct = Mathf.Clamp01(shotChargeTime / GameManager.Instance.maxChargeTime);
        float _lerp = Mathf.Lerp(0, 1, _chargePct);

        chargeSprite.transform.localScale = new Vector3(_lerp, chargeSprite.transform.localScale.y, chargeSprite.transform.localScale.z);
        chargeSprite.transform.parent.gameObject.SetActive(shotChargeTime > 0 && hasBall);
    }

    public void SetBallAround(bool _value) {
        isBallNearby = _value;
	}

    public float GetVelocityMagnitude() {
        return rb.velocity.magnitude;
	}

    private void Animate(bool _cheering = false) {
		if (_cheering) {
            animController.SetAnimState(State.cheer);
            return;
		}

		if (!GameManager.Instance.GetCanMove()) {
            return;
		}

        if (inputData.Move.x == 0 && inputData.Move.y > 0) {
            animController.SetAnimState(State.up);
            if (hasBall) {
                targetBallPosition = targetBallPositions[0];
            }

        } else if(inputData.Move.x > 0 && inputData.Move.y > 0) {
            animController.SetAnimState(State.rightup);
            if (hasBall) {
                targetBallPosition = targetBallPositions[1];
            }

        } else if (inputData.Move.x > 0 && inputData.Move.y == 0) {
            animController.SetAnimState(State.right);
            if (hasBall) {
                targetBallPosition = targetBallPositions[2];
            }

        } else if (inputData.Move.x > 0 && inputData.Move.y < 0) {
            animController.SetAnimState(State.rightdown);
            if (hasBall) {
                targetBallPosition = targetBallPositions[3];
            }

        } else if (inputData.Move.x == 0 && inputData.Move.y < 0) {
            animController.SetAnimState(State.down);
            if (hasBall) {
                targetBallPosition = targetBallPositions[4];
            }

        } else if (inputData.Move.x < 0 && inputData.Move.y < 0) {
            animController.SetAnimState(State.leftdown);
            if (hasBall) {
                targetBallPosition = targetBallPositions[5];
            }

        } else if (inputData.Move.x < 0 && inputData.Move.y == 0) {
            animController.SetAnimState(State.left);
            if (hasBall) {
                targetBallPosition = targetBallPositions[6];
            }

        } else if (inputData.Move.x < 0 && inputData.Move.y > 0) {
            animController.SetAnimState(State.leftup);
            if (hasBall) {
                targetBallPosition = targetBallPositions[7];
            }

        } else {
            animController.SetAnimState(State.idle);
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
}
