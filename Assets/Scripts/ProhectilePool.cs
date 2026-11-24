using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [Header("Пул")]
    [Tooltip("Префаб снаряда (должен содержать TurretProjectile).")]
    [SerializeField] private TurretProjectile projectilePrefab;
    [Tooltip("Сколько снарядов создать заранее при старте сцены.")]
    [SerializeField, Min(0)] private int prewarmCount = 16;
    [Tooltip("Максимум одновременно существующих снарядов. 0 = без лимита.")]
    [SerializeField, Min(0)] private int maxSize = 64;

    private readonly Queue<TurretProjectile> available = new Queue<TurretProjectile>();
    private int totalCreated;
    private bool limitWarningShown;

    private void Awake()
    {
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;

        if (projectilePrefab == null)
        {
            Debug.LogError($"{nameof(ProjectilePool)}: не назначен префаб снаряда.", this);
            enabled = false;
            return;
        }

        int count = maxSize > 0 ? Mathf.Min(prewarmCount, maxSize) : prewarmCount;
        for (int i = 0; i < count; i++)
        {
            TurretProjectile projectile = CreateInstance();
            projectile.gameObject.SetActive(false);
            available.Enqueue(projectile);
        }
    }

    public TurretProjectile Get(Vector3 position, Quaternion rotation)
    {
        TurretProjectile projectile = null;

        while (available.Count > 0 && projectile == null)
            projectile = available.Dequeue();

        if (projectile == null)
        {
            if (maxSize > 0 && totalCreated >= maxSize)
            {
                if (!limitWarningShown)
                {
                    limitWarningShown = true;
                    Debug.LogWarning($"{nameof(ProjectilePool)}: достигнут лимит {maxSize}, выстрелы пропускаются. Увеличь Max Size или уменьши Projectile Lifetime.", this);
                }
                return null;
            }

            projectile = CreateInstance();
        }

        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    public void Release(TurretProjectile projectile)
    {
        if (projectile == null || !projectile.gameObject.activeSelf) return;

        projectile.gameObject.SetActive(false);
        available.Enqueue(projectile);
    }

    private TurretProjectile CreateInstance()
    {
        TurretProjectile projectile = Instantiate(projectilePrefab, transform);
        projectile.SetPool(this);
        totalCreated++;
        return projectile;
    }
}