using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Смертельные слои")]
    [Tooltip("Контакт с объектами этих слоёв убивает игрока (Hazard, DeathWall).")]
    [SerializeField] private LayerMask deadlyLayers;

    [Header("Падение за пределы уровня")]
    [Tooltip("Убивать игрока, если он упал ниже заданной высоты.")]
    [SerializeField] private bool killBelowY = true;
    [SerializeField] private float killY = -30f;

    [Header("Эффекты")]
    [Tooltip("Спрайт/модель игрока: скрывается при смерти. Можно оставить пустым.")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("Префаб эффекта смерти. Можно оставить пустым.")]
    [SerializeField] private GameObject deathVfxPrefab;

    [Tooltip("Если включено, игрок не умирает (включается на время финиша).")]
    public bool IsInvulnerable { get; set; }

    private PlayerController controller;

    public event Action OnPlayerDied;

    public bool IsDead { get; private set; }

    private void Reset()
    {
        deadlyLayers = LayerMask.GetMask("Hazard", "DeathWall");
    }

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!IsDead && killBelowY && transform.position.y < killY)
            Die();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryKill(other.gameObject);
    private void OnTriggerStay2D(Collider2D other) => TryKill(other.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => TryKill(collision.gameObject);

    private void TryKill(GameObject other)
    {
        if (IsDead) return;

        if ((deadlyLayers.value & (1 << other.layer)) != 0)
            Die();
    }

    public void Die()
    {
        if (IsDead || IsInvulnerable) return;

        if (IsDead) return;
        IsDead = true;

        controller.FreezeForDeath();

        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);

        OnPlayerDied?.Invoke();
    }
}