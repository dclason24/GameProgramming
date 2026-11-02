using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [Tooltip("Damage on contact. With an active shield, any value consumes 1 shield hit.")]
    [SerializeField] int damage = 1;

    void OnCollisionEnter2D(Collision2D c) => TryHit(c.collider);
    void OnCollisionStay2D(Collision2D c)  => TryHit(c.collider);
    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other)  => TryHit(other);

    void TryHit(Collider2D other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
    }
}