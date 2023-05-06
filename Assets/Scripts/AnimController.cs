using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State {
    up,
    rightup,
    right,
    rightdown,
    down,
    leftdown,
    left,
    leftup,
    idle,
    cheer
}

public class AnimController : MonoBehaviour
{
    private State currentState;
    private int currentFrame;
    private int currentAnimFrame;
    private List<Sprite> activeAnim;
    [SerializeField] int frameRate;
    private SpriteRenderer spriteRenderer;
    [SerializeField] List<Sprite> up;
    [SerializeField] List<Sprite> rightUp;
    [SerializeField] List<Sprite> right;
    [SerializeField] List<Sprite> rightDown;
    [SerializeField] List<Sprite> down;
    [SerializeField] List<Sprite> leftDown;
    [SerializeField] List<Sprite> left;
    [SerializeField] List<Sprite> leftUp;

    [SerializeField] List<Sprite> idle;
    [SerializeField] List<Sprite> cheer;

	private void Awake() {
		spriteRenderer = GetComponent<SpriteRenderer>();
	}
	private void Start() {
        spriteRenderer.sprite = idle[0];
	}

    public State GetAnimState() {
        return currentState;
	}

	private void Update() {
        currentFrame++;
        if (currentFrame >= frameRate) {
            currentAnimFrame = currentAnimFrame == activeAnim.Count - 1 ? 0 : currentAnimFrame + 1;
            spriteRenderer.sprite = activeAnim[currentAnimFrame];
            currentFrame = 0;
        }
    }

    public void SetAnimState(State _state) {
        if(_state == currentState) {
            return;
		}
        currentState = _state;
        currentAnimFrame = 0;
        if (currentState == State.up) {
            activeAnim = up;

        } else if (currentState == State.rightup) {
            activeAnim = rightUp;

        } else if (currentState == State.right) {
            activeAnim = right;

        } else if (currentState == State.rightdown) {
            activeAnim = rightDown;

        } else if (currentState == State.down) {
            activeAnim = down;

        } else if (currentState == State.leftdown) {
            activeAnim = leftDown;

        } else if (currentState == State.left) {
            activeAnim = left;

        } else if (currentState == State.leftup) {
            activeAnim = leftUp;

        } else if (currentState == State.idle) {
            activeAnim = idle;

        } else if (currentState == State.cheer) {
            activeAnim = cheer;
        }
    }

}
