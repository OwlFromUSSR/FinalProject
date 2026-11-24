using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spikes : MonoBehaviour
{
    private void Reset()
    {
        int layer = LayerMask.NameToLayer("Hazard");
        if (layer >= 0) gameObject.layer = layer;

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);

    private void OnTriggerStay2D(Collider2D other) => TryKill(other);

    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

    private void OnCollisionStay2D(Collision2D collision) => TryKill(collision.collider);

    private static void TryKill(Collider2D other)
    {
        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
            death.Die();
    }
}