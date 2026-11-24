using UnityEngine;

/// <summary>
/// Плавное слежение за целью (SmoothDamp) с ограничением по границам уровня.
/// Границы учитывают размер видимой области, поэтому камера не показывает пустоту за уровнем.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Если пусто, будет найден объект с тегом Player.")]
    [SerializeField] private Transform target;
    [Tooltip("Смещение камеры относительно цели.")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

    [Header("Сглаживание")]
    [Tooltip("Время сглаживания (меньше = жёстче следит).")]
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;
    [Tooltip("Максимальная скорость камеры (юниты/сек).")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 100f;

    [Header("Границы уровня (мировые координаты)")]
    [SerializeField] private bool clampToBounds = true;
    [Tooltip("Левый нижний угол области уровня.")]
    [SerializeField] private Vector2 minBounds = new Vector2(-20f, -10f);
    [Tooltip("Правый верхний угол области уровня.")]
    [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (!cam.orthographic)
            Debug.LogWarning($"{nameof(CameraFollow)}: ожидается Orthographic-камера для 2D-платформера.", this);
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) target = playerObject.transform;
        }

        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            ComputeDesiredPosition(),
            ref velocity,
            smoothTime,
            maxSpeed);
    }

    public void SetTarget(Transform newTarget, bool snap = false)
    {
        target = newTarget;
        if (snap) SnapToTarget();
    }

    /// <summary>Мгновенно ставит камеру на цель.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        transform.position = ComputeDesiredPosition();
    }

    private Vector3 ComputeDesiredPosition()
    {
        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);

        return clampToBounds ? ClampToBounds(desired) : desired;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        position.x = ClampAxis(position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth, (minBounds.x + maxBounds.x) * 0.5f);
        position.y = ClampAxis(position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight, (minBounds.y + maxBounds.y) * 0.5f);
        return position;
    }

    // Если уровень уже области видимости по оси, камера центрируется.
    private static float ClampAxis(float value, float min, float max, float center)
    {
        return min > max ? center : Mathf.Clamp(value, min, max);
    }

    private void OnDrawGizmosSelected()
    {
        if (!clampToBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}