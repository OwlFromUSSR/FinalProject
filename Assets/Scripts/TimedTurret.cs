using System;
using UnityEngine;
using UnityEngine.Events;

public class TimedTurret : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Пул снарядов. Если пусто, будет найден в сцене автоматически.")]
    [SerializeField] private ProjectilePool projectilePool;
    [Tooltip("Точка вылета снаряда. Ось X (красная) задаёт направление. Если пусто, используется сама турель.")]
    [SerializeField] private Transform firePoint;

    [Header("Стрельба")]
    [Tooltip("Интервал между выстрелами (сек).")]
    [SerializeField, Min(0.05f)] private float fireInterval = 2f;
    [Tooltip("Время до первого выстрела (сек). Разные значения у соседних турелей дают поочерёдную стрельбу.")]
    [SerializeField, Min(0f)] private float initialDelay = 0f;
    [Tooltip("Скорость снаряда (юниты/сек).")]
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [Tooltip("Время жизни снаряда (сек), затем он возвращается в пул.")]
    [SerializeField, Min(0.1f)] private float projectileLifetime = 4f;
    [Tooltip("Стрельба включена.")]
    [SerializeField] private bool isFiring = true;

    [Header("Направление")]
    [Tooltip("Если включено, используется заданный вектор вместо firePoint.right.")]
    [SerializeField] private bool useCustomDirection = false;
    [SerializeField] private Vector2 customDirection = Vector2.right;

    [Header("Телеграфирование выстрела")]
    [Tooltip("За сколько секунд до выстрела вызывать OnBeforeFire.")]
    [SerializeField, Min(0f)] private float warningTime = 0.3f;
    [Tooltip("Те же события для настройки в инспекторе (анимация, звук, частицы).")]
    [SerializeField] private UnityEvent onBeforeFire;
    [SerializeField] private UnityEvent onFired;

    private float timeUntilFire;
    private bool warningRaised;

    public event Action OnBeforeFire;

    public event Action OnFired;

    public bool IsFiring => isFiring;

    private void Awake()
    {
        timeUntilFire = initialDelay;

        if (projectilePool == null)
            projectilePool = FindFirstObjectByType<ProjectilePool>();

        if (projectilePool == null)
        {
            Debug.LogError($"{nameof(TimedTurret)}: в сцене нет ProjectilePool.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!isFiring) return;

        timeUntilFire -= Time.deltaTime;

        float warning = Mathf.Min(warningTime, fireInterval);
        if (!warningRaised && timeUntilFire <= warning)
        {
            warningRaised = true;
            OnBeforeFire?.Invoke();
            onBeforeFire?.Invoke();
        }

        if (timeUntilFire <= 0f)
        {
            Fire();
            timeUntilFire += fireInterval;
            warningRaised = false;
        }
    }


    public void SetFiring(bool value) => isFiring = value;

    public void ResetTimer()
    {
        timeUntilFire = initialDelay;
        warningRaised = false;
    }


    private void Fire()
    {
        Transform origin = firePoint != null ? firePoint : transform;

        TurretProjectile projectile = projectilePool.Get(origin.position, Quaternion.identity);
        if (projectile == null) return;

        projectile.Launch(GetFireDirection(origin), projectileSpeed, projectileLifetime);

        OnFired?.Invoke();
        onFired?.Invoke();
    }

    private Vector2 GetFireDirection(Transform origin)
    {
        return useCustomDirection ? customDirection.normalized : (Vector2)origin.right;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        Vector2 dir = useCustomDirection ? customDirection.normalized : (Vector2)origin.right;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, 0.1f);
        Gizmos.DrawLine(origin.position, (Vector2)origin.position + dir * 2f);
    }
}