using System;
using UnityEngine;

/// <summary>
/// Движение игрока: бег с ускорением/замедлением, спринт (с опциональной выносливостью),
/// прыжок с coyote time, jump buffering и регулируемой высотой.
/// Ввод читается в Update, физика применяется в FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Бег")]
    [Tooltip("Максимальная скорость ходьбы (юниты/сек).")]
    [SerializeField, Min(0f)] private float walkSpeed = 7f;
    [Tooltip("Ускорение при разгоне (юниты/сек²). Больше значение, меньше «вязкости».")]
    [SerializeField, Min(0f)] private float acceleration = 80f;
    [Tooltip("Замедление при отпускании кнопки или снижении скорости (юниты/сек²).")]
    [SerializeField, Min(0f)] private float deceleration = 100f;
    [Tooltip("Ускорение при развороте в противоположную сторону (юниты/сек²).")]
    [SerializeField, Min(0f)] private float turnAcceleration = 140f;
    [Tooltip("Множитель управляемости в воздухе (1 = как на земле).")]
    [SerializeField, Range(0f, 1f)] private float airControl = 0.75f;

    [Header("Спринт (Left Shift)")]
    [Tooltip("Во сколько раз спринт быстрее ходьбы.")]
    [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 1.5f;
    [Tooltip("Если включено, спринт расходует выносливость.")]
    [SerializeField] private bool useStamina = false;
    [Tooltip("Запас выносливости в секундах непрерывного спринта.")]
    [SerializeField, Min(0.1f)] private float maxStamina = 3f;
    [Tooltip("Расход выносливости в секунду во время спринта.")]
    [SerializeField, Min(0f)] private float staminaDrainPerSecond = 1f;
    [Tooltip("Восстановление выносливости в секунду.")]
    [SerializeField, Min(0f)] private float staminaRegenPerSecond = 1.5f;
    [Tooltip("Пауза в секундах после спринта перед началом восстановления.")]
    [SerializeField, Min(0f)] private float staminaRegenDelay = 0.5f;
    [Tooltip("После полного истощения спринт снова доступен при этой доле запаса (0..1).")]
    [SerializeField, Range(0f, 1f)] private float exhaustionRecoverThreshold = 0.3f;

    [Header("Прыжок")]
    [Tooltip("Высота полного прыжка в юнитах (при обычной гравитации Rigidbody2D).")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 3.2f;
    [Tooltip("Coyote time: сколько секунд после схода с платформы ещё можно прыгнуть.")]
    [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.12f;
    [Tooltip("Jump buffering: сколько секунд запоминается нажатие прыжка до приземления.")]
    [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.12f;
    [Tooltip("Множитель вертикальной скорости при раннем отпускании кнопки (меньше = короче прыжок).")]
    [SerializeField, Range(0.05f, 1f)] private float jumpCutMultiplier = 0.45f;
    [Tooltip("Множитель гравитации при падении (делает падение «тяжелее»).")]
    [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.6f;
    [Tooltip("Максимальная скорость падения (юниты/сек).")]
    [SerializeField, Min(1f)] private float maxFallSpeed = 25f;

    [Header("Проверка земли")]
    [Tooltip("Слой(и) земли.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Центр проверки относительно позиции игрока (обычно у ног).")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [Tooltip("Размер OverlapBox. Делай чуть уже коллайдера, чтобы стены не считались землёй.")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

    [Header("Визуал")]
    [Tooltip("Дочерний объект со спрайтом: зеркалится по X при смене направления. Можно оставить пустым.")]
    [SerializeField] private Transform visualRoot;

    private const float InputDeadzone = 0.01f;

    private Rigidbody2D rb;
    private IPlayerInput input;
    private float baseGravityScale;

    private bool controlEnabled = true;
    private float horizontalInput;
    private bool sprintRequested;

    private bool isGrounded;
    private bool isJumpingUp;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private float stamina;
    private bool isExhausted;
    private float regenTimer;

    public event Action OnJumped;
    public event Action OnLanded;
    /// <summary>Нормализованная выносливость 0..1 (для HUD). Вызывается только при изменении.</summary>
    public event Action<float> OnStaminaChanged;

    public bool IsGrounded => isGrounded;
    public bool IsSprinting { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public float Stamina01 => useStamina ? stamina / maxStamina : 1f;

    private Vector2 Velocity
    {
        get
        {
            return rb.linearVelocity;
        }
        set
        {
            rb.linearVelocity = value;
        }
    }

    private void Reset()
    {
        groundMask = LayerMask.GetMask("Ground");
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        baseGravityScale = rb.gravityScale;
        stamina = maxStamina;

        input = GetComponent<IPlayerInput>();
        if (input == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: на объекте нет компонента, реализующего IPlayerInput (например, PlayerInputReader).", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (controlEnabled)
        {
            horizontalInput = input.Horizontal;
            sprintRequested = input.SprintHeld;

            if (input.JumpPressedThisFrame)
                jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            horizontalInput = 0f;
            sprintRequested = false;
        }

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        UpdateGroundState(dt);
        UpdateSprint(dt);

        Vector2 velocity = Velocity;
        velocity.x = CalculateHorizontalVelocity(velocity.x, dt);
        velocity = ApplyJumpCut(velocity);
        velocity = TryJump(velocity, dt);
        velocity = ApplyFallModifiers(velocity);
        Velocity = velocity;
    }

    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        input.SetEnabled(value);

        if (!value)
        {
            horizontalInput = 0f;
            sprintRequested = false;
            jumpBufferCounter = 0f;
        }
    }

    public void FreezeForDeath()
    {
        SetControlEnabled(false);
        Velocity = Vector2.zero;
        rb.simulated = false;
        IsSprinting = false;
        enabled = false;
    }


    private void UpdateGroundState(float dt)
    {
        bool wasGrounded = isGrounded;

        Vector2 center = (Vector2)transform.position + groundCheckOffset;
        bool overlapping = Physics2D.OverlapBox(center, groundCheckSize, 0f, groundMask) != null;

        isGrounded = overlapping && Velocity.y <= 0.1f;

        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter = Mathf.Max(0f, coyoteCounter - dt);

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - dt);

        if (!wasGrounded && isGrounded)
        {
            isJumpingUp = false;
            OnLanded?.Invoke();
        }
    }

    private void UpdateSprint(float dt)
    {
        bool moving = Mathf.Abs(horizontalInput) > InputDeadzone;
        bool wantsSprint = sprintRequested && moving;

        if (!useStamina)
        {
            IsSprinting = wantsSprint;
            return;
        }

        float before = stamina;

        if (isExhausted && stamina >= maxStamina * exhaustionRecoverThreshold)
            isExhausted = false;

        IsSprinting = wantsSprint && !isExhausted && stamina > 0f;

        if (IsSprinting)
        {
            stamina -= staminaDrainPerSecond * dt;
            regenTimer = staminaRegenDelay;

            if (stamina <= 0f)
            {
                stamina = 0f;
                isExhausted = true;
                IsSprinting = false;
            }
        }
        else
        {
            regenTimer -= dt;
            if (regenTimer <= 0f)
                stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * dt);
        }

        if (!Mathf.Approximately(before, stamina))
            OnStaminaChanged?.Invoke(Stamina01);
    }

    private float CalculateHorizontalVelocity(float currentX, float dt)
    {
        float topSpeed = walkSpeed * (IsSprinting ? sprintSpeedMultiplier : 1f);
        float targetX = horizontalInput * topSpeed;
        bool hasInput = Mathf.Abs(horizontalInput) > InputDeadzone;

        float rate;
        if (!hasInput)
            rate = deceleration;
        else if (Mathf.Abs(currentX) > 0.1f && Mathf.Sign(currentX) != Mathf.Sign(targetX))
            rate = turnAcceleration;
        else if (Mathf.Abs(targetX) < Mathf.Abs(currentX))
            rate = deceleration;
        else
            rate = acceleration;

        if (!isGrounded)
            rate *= airControl;

        return Mathf.MoveTowards(currentX, targetX, rate * dt);
    }

    private Vector2 ApplyJumpCut(Vector2 velocity)
    {
        if (!isJumpingUp) return velocity;

        if (velocity.y <= 0f)
        {
            isJumpingUp = false;
        }
        else if (!input.JumpHeld)
        {
            velocity.y *= jumpCutMultiplier;
            isJumpingUp = false;
        }

        return velocity;
    }

    private Vector2 TryJump(Vector2 velocity, float dt)
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
            return velocity;

        float gravity = Mathf.Abs(Physics2D.gravity.y) * baseGravityScale;
        velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isGrounded = false;
        isJumpingUp = true;

        OnJumped?.Invoke();
        return velocity;
    }

    private Vector2 ApplyFallModifiers(Vector2 velocity)
    {
        rb.gravityScale = velocity.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        return velocity;
    }

    private void UpdateFacing()
    {
        if (horizontalInput > InputDeadzone) FacingDirection = 1;
        else if (horizontalInput < -InputDeadzone) FacingDirection = -1;
        else return;

        if (visualRoot != null)
        {
            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            visualRoot.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
    }
}