using UnityEngine;

public struct InputData {
    public Vector2 Move;
    public bool Ability;
    public bool Sprint;
}

public class KeyboardInput : MonoBehaviour
{
    public InputData GenerateInput() {
        return new InputData {
            Move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized,
            Ability = Input.GetButton("Ability"),
            Sprint = Input.GetButton("Sprint"),
        };
    }
}
