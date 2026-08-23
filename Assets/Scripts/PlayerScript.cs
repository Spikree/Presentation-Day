using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            return;
        }

        rb.gravityScale = 0f;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal"); // Left/Right
        movement.y = Input.GetAxisRaw("Vertical");   // Up/Down

        if (movement.magnitude > 1)
        {
            movement = movement.normalized;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}