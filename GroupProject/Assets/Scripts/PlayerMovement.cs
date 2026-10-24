using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D body;
    private Vector2 movementInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = new Vector2(
            (Keyboard.current.dKey.isPressed ? 1f : 0f) -
            (Keyboard.current.aKey.isPressed ? 1f : 0f),
            (Keyboard.current.wKey.isPressed ? 1f : 0f) -
            (Keyboard.current.sKey.isPressed ? 1f : 0f));

        movementInput += new Vector2(
            (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) -
            (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f),
            (Keyboard.current.upArrowKey.isPressed ? 1f : 0f) -
            (Keyboard.current.downArrowKey.isPressed ? 1f : 0f));

        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
    }

    private void FixedUpdate()
    {
        body.MovePosition(body.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }
}
