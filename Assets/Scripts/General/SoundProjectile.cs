// SoundProjectile.cs
// Attach to the SoundProjectile prefab.
// Travels horizontally at fixed speed, deals damage and stuns on hit.
// Destroys itself after maxLifetime seconds or on hitting a valid target.
using UnityEngine;

public class SoundProjectile : MonoBehaviour
{
    // Set via Initialize() — not public Inspector fields
    private FighterController owner;
    private float direction;      //  1 = right,  -1 = left
    private float speed;
    private float damage;
    private float stunDuration;
    private float screenShake;

    [Header("Lifetime")]
    public float maxLifetime = 3f;   // auto-destroy if it never hits anything

    private bool hasHit = false;

    public void Initialize(FighterController owner, float direction, float speed,
                           float damage, float stunDuration, float screenShake)
    {
        this.owner = owner;
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.stunDuration = stunDuration;
        this.screenShake = screenShake;

        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (hasHit) return;

        // Move horizontally in world X — same axis as fighter movement
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null) return;
        if (hurtbox.owner == owner) return; // never hit the owner

        hasHit = true;

        FighterController target = hurtbox.owner;

        // If the target is blocking, the projectile is fully absorbed — no damage, no stun
        // The projectile still destroys itself so it can't pass through a block
        if (target.currentState == FighterState.Blocking)
        {
            Destroy(gameObject);
            return;
        }

        // Deal damage and stun — only reaches here if target is not blocking
        target.ApplyStun(stunDuration);
        target.currentHealth -= damage;
        target.currentHealth = Mathf.Max(target.currentHealth, 0f);

        // Screen shake on the owner's camera impulse source
        if (owner != null)
            owner.TriggerScreenShake(screenShake);

        // Check for KO after direct health edit
        if (target.currentHealth <= 0f)
            target.TransitionTo(FighterState.KO);

        // Small visual pause on the projectile before destroying
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Draw the projectile's collider for debugging
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}