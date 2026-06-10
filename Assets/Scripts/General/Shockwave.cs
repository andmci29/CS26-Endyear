// Shockwave.cs
// Attach to the Shockwave prefab.
// Travels horizontally along the ground toward the opponent.
// Deals damage + knockback on hit.
// Can be blocked for chip damage — unlike the sound projectile it is not fully absorbed.
// Can be jumped over since its hitbox stays at ground level.
using UnityEngine;

public class Shockwave : MonoBehaviour
{
    // Set via Initialize() — not public Inspector fields
    private FighterController owner;
    private float direction;            //  1 = right,  -1 = left
    private float speed;
    private float damage;
    private float blockChipDamage;      // fraction of damage dealt through block
    private Vector2 knockbackForce;
    private float screenShake;

    [Header("Lifetime")]
    public float maxLifetime = 2.5f;    // auto-destroy if it never hits anything

    [Header("Ground Hug")]
    public float groundOffset = 0f;     // Y offset from spawn — keep at 0 for ground level

    private bool hasHit = false;

    public void Initialize(FighterController owner, float direction, float speed,
                           float damage, float blockChipDamage,
                           Vector2 knockbackForce, float screenShake)
    {
        this.owner = owner;
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.blockChipDamage = blockChipDamage;
        this.knockbackForce = knockbackForce;
        this.screenShake = screenShake;

        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (hasHit) return;

        // Travel horizontally — stays at its spawn Y position (ground level)
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

        // In OnTriggerEnter, after confirming a valid hit target:
        FighterAudio attackerAudio = owner.GetComponent<FighterAudio>();
        attackerAudio?.PlayAttackLandSound(true); // shockwave always counts as heavy

        if (target.currentState == FighterState.Blocking)
        {
            // Chip damage through block — shockwave is not fully absorbed
            float chipDamage = damage * blockChipDamage;
            target.currentHealth -= chipDamage;
            target.currentHealth = Mathf.Max(target.currentHealth, 0f);

            if (target.currentHealth <= 0f)
                target.TransitionTo(FighterState.KO);

            // Small screen shake even on block — player feels the impact
            owner.TriggerScreenShake(screenShake * 0.4f);

            Destroy(gameObject);
            return;
        }

        // Full hit — damage and knockback
        // Build a temporary AttackData-like struct to reuse TakeDamage
        // Instead we apply effects directly to keep things simple
        float pushDirection = target.transform.position.x > owner.transform.position.x ? 1f : -1f;

        target.currentHealth -= damage;
        target.currentHealth = Mathf.Max(target.currentHealth, 0f);

        // Apply knockback velocity directly
        target.GetComponent<Rigidbody>().linearVelocity = new Vector3(
            knockbackForce.x * pushDirection,
            knockbackForce.y,
            0f
        );

        owner.TriggerScreenShake(screenShake);

        if (target.currentHealth <= 0f)
            target.TransitionTo(FighterState.KO);
        else
        {
            target.knockbackTimer = target.data.knockbackDuration;
            target.TransitionTo(FighterState.Knockback);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f); // orange for ground wave
        Gizmos.DrawWireCube(transform.position, new Vector3(0.6f, 0.3f, 0.6f));
    }
}