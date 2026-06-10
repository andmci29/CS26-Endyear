// GuitarSpecial.cs
// Attach to Axel (guitar fighter) alongside FighterController.
// Fires a straight horizontal SoundProjectile on special press.
// Enforces a cooldown so it can't be spammed.
using System.Collections;
using UnityEngine;

public class GuitarSpecial : SpecialAbilityBase
{
    [Header("Projectile")]
    public GameObject projectilePrefab;   // drag SoundProjectile prefab here
    public Transform firePoint;           // empty child on the fighter — sets spawn position

    [Header("Tuning")]
    public float cooldownDuration = 4f;   // seconds before ability is usable again
    public float projectileSpeed = 18f;  // world units per second
    public float damage = 14f;
    public float stunDuration = 0.6f; // how long the opponent is frozen on hit
    public float screenShake = 0.2f;

    [Header("Timing")]
    [Tooltip("Seconds between animation start and projectile spawning — match to your animation")]
    public float fireDelay = 0.2f;

    [Header("Animation")]
    public string animationTrigger = "Special"; // trigger name in the Animator

    // Private
    private float cooldownTimer = 0f;    // counts down to 0 — ability ready when <= 0
    private bool isFiring = false;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // Called by FighterController when special button is pressed
    public override void TryActivate()
    {
        if (!IsReady()) return;
        if (isFiring) return;

        // Must be grounded and in an interruptible state
        if (fighter.currentState == FighterState.Attacking
            || fighter.currentState == FighterState.Knockback
            || fighter.currentState == FighterState.KO
            || fighter.currentState == FighterState.Blocking)
            return;

        StartCoroutine(FireSequence());
    }

    private IEnumerator FireSequence()
    {
        isFiring = true;
        cooldownTimer = cooldownDuration;

        // Lock fighter in place during firing (brief commit window like a real attack)
        fighter.TransitionTo(FighterState.Attacking);

        if (fighter.animator != null && fighter.animator.runtimeAnimatorController != null)
            fighter.animator.SetTrigger(animationTrigger);

        // Windup — waits for fireDelay seconds before spawning projectile
        // Tune this in the Inspector to match when the animation hits its release frame
        yield return new WaitForSeconds(fireDelay);

        SpawnProjectile();

        // Hold attacking state briefly after firing
        yield return new WaitForSeconds(0.3f);

        fighter.TransitionTo(FighterState.Idle);
        isFiring = false;
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("GuitarSpecial: projectilePrefab is not assigned.");
            return;
        }

        // Sound fires at the exact moment the projectile spawns
        fighter.GetComponent<FighterAudio>()?.PlaySpecialSound();

        // Use firePoint if assigned, otherwise spawn at fighter's position + small offset
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + new Vector3(fighter.facingRight ? 0.6f : -0.6f, 0.3f, 0f);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        SoundProjectile sp = proj.GetComponent<SoundProjectile>();
        if (sp != null)
        {
            sp.Initialize(
                owner: fighter,
                direction: fighter.facingRight ? 1f : -1f,
                speed: projectileSpeed,
                damage: damage,
                stunDuration: stunDuration,
                screenShake: screenShake
            );
        }
    }

    // -------------------------------------------------------
    // SpecialAbilityBase interface
    // -------------------------------------------------------

    public override bool IsReady() => cooldownTimer <= 0f && !isFiring;

    // Returns 0 when on cooldown, 1 when fully ready
    public override float CooldownProgress()
    {
        if (cooldownDuration <= 0f) return 1f;
        return Mathf.Clamp01(1f - (cooldownTimer / cooldownDuration));
    }
}