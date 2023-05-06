using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayStyle {
    Striker,
    Playmaker,
    Defender,
    Goalie
}

public class PlayerController : MonoBehaviour
{
    private InputData inputData;
    private KeyboardInput[] m_Inputs;
    private AnimController animController;

    private Rigidbody2D rb;
    private bool hasBall;
    private float stamina;
    private float shotChargeTime;
    private bool ballAround;

    [SerializeField] private Team team;
    [SerializeField] private PlayStyle role;


    [SerializeField] private float speed = 1f;
    [SerializeField] private float standardSpeed = 15f;
    [SerializeField] private float sprintSpeed = 17f;
    [SerializeField] private float maxStamina = 1f;
    [SerializeField] private float staminaRechargeRate = 1f;
    [SerializeField] private float staminaConsumeRate = 1f;
    [SerializeField] private float staminaRechargeDelay = 2f;
    [SerializeField] private float staminaDisplayTime = 2f;
    [SerializeField] private float speedLerp = 5f;
    [SerializeField] private float minChargeForShot = .4f;
    [SerializeField] private float shotChargeRate = 1f;
    [SerializeField] private float maxChargeTime = 3f;
    [SerializeField] private float ShotMaxStrength = 1f;
    [SerializeField] private float ShotMinStrength = .4f;

    [HideInInspector] public GameObject targetBallPosition;
    [SerializeField] private GameObject[] targetBallPositions;
    [SerializeField] private SpriteRenderer chargeSprite, staminaSprite;
    void Awake()
    {
        m_Inputs = GetComponents<KeyboardInput>();
        rb = GetComponent<Rigidbody2D>();
    }

	private void Start() {
        animController = GetComponentInChildren<AnimController>();
        stamina = maxStamina;
        UpdateChargeDisplay();
        UpdateStaminaDisplay();
    }

	private void Reset() {
        stamina = maxStamina;
        hasBall = false;
	}

    public void SetHasBall(bool _value) {
        hasBall = _value;
        gameObject.layer = hasBall ? LayerMask.NameToLayer("BallCarrier") : LayerMask.NameToLayer("Player");

    }

	void FixedUpdate()
    {
        for (int i = 0; i < m_Inputs.Length; i++) {
            inputData = m_Inputs[i].GenerateInput();
        }
        rb.velocity = inputData.Move * speed * Time.fixedDeltaTime;

        if (inputData.Ability) {
            if(this == GameManager.Instance.GetBallCarrier()) {
                ChargeShot();
            } else if (ballAround){
                PlayerController _ballCarrier = GameManager.Instance.GetBallCarrier();
				#region Tackle
				if (_ballCarrier) {
                    if(_ballCarrier.GetCurrentStamina() > GetCurrentStamina()) {
                        _ballCarrier.ConsumeStamina(GetCurrentStamina());
                        ConsumeStamina(GetCurrentStamina());
                        return;
					} else {
                        _ballCarrier.ConsumeStamina(_ballCarrier.GetCurrentStamina());
                        ConsumeStamina(_ballCarrier.GetCurrentStamina());
                    }
				}
				#endregion
				GameManager.Instance.SetBallCarrier(this);
                shotChargeTime = 0;
                UpdateChargeDisplay();
            }
        } else if (shotChargeTime > minChargeForShot) {
            Shoot();
		}
		if (inputData.Sprint && stamina > staminaConsumeRate) {
            if (speed == standardSpeed) {
                StartCoroutine(FadeToSprintSpeed(speedLerp));
            };
            ConsumeStamina(staminaConsumeRate);
		} else if (speed != standardSpeed) {
            StopCoroutine(FadeToSprintSpeed(speedLerp));
            speed = standardSpeed;
        }

        Animate();
        
    }

    IEnumerator FadeToSprintSpeed(float _duration) {
        float time = 0;
        while (time < _duration) {
            time += Time.deltaTime;
            speed = Mathf.Lerp(standardSpeed, sprintSpeed, time / _duration);
            yield return null;
        }

        speed = sprintSpeed;
    }

    public void ConsumeStamina(float _value) {
        CancelInvoke("RechargeStamina");
        stamina = Mathf.Clamp(stamina - _value, 0, maxStamina);
        UpdateStaminaDisplay();
        InvokeRepeating("RechargeStamina", staminaRechargeDelay, .01f);
    }

    private void RechargeStamina() {
        Debug.Log("recharging stamina");
        stamina = Mathf.Clamp(stamina + staminaRechargeRate, 0, maxStamina);
        UpdateStaminaDisplay();
        if (stamina >= maxStamina) {
            CancelInvoke("RechargeStamina");
        }
    }

    private void UpdateStaminaDisplay() {
        CancelInvoke("DisableStaminaDisplay");
        staminaSprite.transform.parent.gameObject.SetActive(true);
        float _chargePct = Mathf.Clamp01(stamina / maxStamina);
        float _lerp = Mathf.Lerp(0, 1, _chargePct);

        staminaSprite.transform.localScale = new Vector3(_lerp, staminaSprite.transform.localScale.y, staminaSprite.transform.localScale.z);
        Invoke("DisableStaminaDisplay", staminaDisplayTime);
    }

    private void DisableStaminaDisplay() {
        staminaSprite.transform.parent.gameObject.SetActive(false);
    }

    public float GetCurrentStamina() {
        return stamina;
    }

    private void ChargeShot() {
        shotChargeTime = Mathf.Clamp(shotChargeTime + shotChargeRate, 0f , maxChargeTime);
        if (shotChargeTime > minChargeForShot) {
            UpdateChargeDisplay();
        }
    }

    private void Shoot() {
        if (this == GameManager.Instance.GetBallCarrier()) {
            Vector2 _shot = GetDirection() * Mathf.Lerp(ShotMinStrength, ShotMaxStrength, shotChargeTime / maxChargeTime);
            GameManager.Instance.ShootBall(_shot);
        }

        shotChargeTime = 0;
        UpdateChargeDisplay();
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
        float _chargePct = Mathf.Clamp01(shotChargeTime / maxChargeTime);
        float _lerp = Mathf.Lerp(0, 1, _chargePct);

        chargeSprite.transform.localScale = new Vector3(_lerp, chargeSprite.transform.localScale.y, chargeSprite.transform.localScale.z);
        chargeSprite.transform.parent.gameObject.SetActive(shotChargeTime > 0 && hasBall);
    }

    public void SetBallAround(bool _value) {
        ballAround = _value;
	}

    public float GetVelocityMagnitude() {
        return rb.velocity.magnitude;
	}

    private void Animate() {
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
}
