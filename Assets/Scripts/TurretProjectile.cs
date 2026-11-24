using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TurretProjectile : MonoBehaviour
{
    [Header("Столкновения")]
    [Tooltip("Слои, при касании которых снаряд исчезает (Ground).")]
    [SerializeField] private LayerMask groundMask;

    private Rigidbody2D rb;
    private TrailRenderer trail;
    private ProjectilePool pool;
    private float lifeRemaining;

    private Vector2 Velocity
    {
        set
        {
            rb.linearVelocity = value;
        }
    }

    private void Reset()
    {
        groundMask = LayerMask.GetMask("Ground");

        int layer = LayerMask.NameToLayer("Hazard");
        if (layer >= 0) gameObject.layer = layer;

        GetComponent<Collider2D>().isTrigger = true;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GetComponent<Collider2D>().isTrigger = true;
        trail = GetComponentInChildren<TrailRenderer>();
    }

    public void SetPool(ProjectilePool owner) => pool = owner;

    public void Launch(Vector2 direction, float speed, float lifetime)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        direction.Normalize();

        transform.right = direction;
        lifeRemaining = lifetime;
        Velocity = direction * speed;

        if (trail != null)
            trail.Clear();
    }

    private void Update()
    {
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining <= 0f)
            Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
        {
            death.Die();
            Despawn();
            return;
        }

        if ((groundMask.value & (1 << other.gameObject.layer)) != 0)
            Despawn();
    }

    private void OnDisable()
    {
        if (rb != null)
            Velocity = Vector2.zero;
    }

    private void Despawn()
    {
        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }
}