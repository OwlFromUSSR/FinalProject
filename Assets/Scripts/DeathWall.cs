using System;
using UnityEngine;

public enum MoveDirection
{
    Up,
    Down,
    Left,
    Right
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DeathWall : MonoBehaviour
{
    [Header("Движение")]
    [Tooltip("Направление движения (мировые оси).")]
    [SerializeField] private MoveDirection moveDirection = MoveDirection.Up;
    [Tooltip("Постоянная линейная скорость (юниты/сек).")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [Tooltip("Задержка в секундах после старта уровня перед началом движения.")]
    [SerializeField, Min(0f)] private float startDelay = 2f;
    [Tooltip("Активность стены. Можно переключать из кода или триггеров.")]
    [SerializeField] private bool isMoving = true;

    private Rigidbody2D rb;
    private float delayRemaining;
    private bool hasStartedMoving;

    public event Action OnStartedMoving;

    public bool IsMoving => isMoving;
    public float MoveSpeed => moveSpeed;
    public MoveDirection Direction => moveDirection;

    private void Reset()
    {
        int layer = LayerMask.NameToLayer("DeathWall");
        if (layer >= 0) gameObject.layer = layer;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.isTrigger = true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.isTrigger = true;

        delayRemaining = startDelay;
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        float dt = Time.fixedDeltaTime;

        if (delayRemaining > 0f)
        {
            delayRemaining -= dt;
            return;
        }

        if (!hasStartedMoving)
        {
            hasStartedMoving = true;
            OnStartedMoving?.Invoke();
        }

        rb.MovePosition(rb.position + DirectionToVector(moveDirection) * (moveSpeed * dt));
    }


    public void SetMoving(bool value) => isMoving = value;

    public void SetSpeed(float speed) => moveSpeed = Mathf.Max(0f, speed);

    public void SetDirection(MoveDirection direction) => moveDirection = direction;


    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);

    private void OnTriggerStay2D(Collider2D other) => TryKill(other);

    private static void TryKill(Collider2D other)
    {
        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
            death.Die();
    }

    private static Vector2 DirectionToVector(MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up: return Vector2.up;
            case MoveDirection.Down: return Vector2.down;
            case MoveDirection.Left: return Vector2.left;
            case MoveDirection.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 dir = DirectionToVector(moveDirection);
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        Vector3 start = transform.position;
        Vector3 end = start + dir * 3f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawLine(end, end - dir * 0.6f + perp * 0.35f);
        Gizmos.DrawLine(end, end - dir * 0.6f - perp * 0.35f);
    }
}