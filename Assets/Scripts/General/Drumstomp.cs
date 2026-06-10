// DrumStomp.cs
// Attach to Koda (drum fighter) alongside FighterController.
// On activation: locks fighter in place, plays windup animation,
// then spawns a ground-level shockwave that travels toward the opponent.
using System.Collections;
using UnityEngine;

public class DrumStomp : SpecialAbilityBase
{
    [Header("Shockwave")]
    public GameObject shockwavePrefab;   // drag Shockwave prefab here
    public Transform spawnPoint;         // empty child at ground level in front of fighter

    [Header("Tuning")]
    public float cooldownDuration = 5f;  // seconds before usable again
    public float windupDuration = 0.4f;// seconds of stomp animation before wave spawns
    public float shockwaveSpeed = 12f; // world units per second
    public float damage = 20f; // high damage — high commitment move
    public float blockChipDamage = 0.15f; // fraction of damage dealt through block
    public Vector2 knockbackForce = new Vector2(8f, 3f);
    public float screenShake = 0.35f;

    [Header("Animation")]
    public string animationTrigger = "Special"; // trigger name in the Animator

    // Private
    private float cooldownTimer = 0f;
    private bool isActive = false;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public override void TryActivate()
    {
        if (!IsReady()) return;
        if (isActive) return;

        // Must be grounded — stomp doesn't work in the air
        if (!fighter.isGrounded) return;

        if (fighter.currentState == FighterState.Attacking
            || fighter.currentState == FighterState.Knockback
            || fighter.currentState == FighterState.KO
            || fighter.currentState == FighterState.Blocking)
            return;

        StartCoroutine(StompSequence());
    }

    private IEnumerator StompSequence()
    {
        isActive = true;
        cooldownTimer = cooldownDuration;

        // Lock fighter in attacking state for the full sequence
        fighter.TransitionTo(FighterState.Attacking);
        fighter.attackTimer = windupDuration + 0.2f; // cover full animation duration

        if (fighter.animator != null && fighter.animator.runtimeAnimatorController != null)
            fighter.animator.SetTrigger(animationTrigger);

        // Windup — fighter is locked in place, tension builds
        yield return new WaitForSeconds(windupDuration);

        // Spawn shockwave at the moment of impact
        SpawnShockwave();

        // Brief post-impact pause before returning to idle
        yield return new WaitForSeconds(0.2f);

        fighter.TransitionTo(FighterState.Idle);
        isActive = false;
    }

    private void SpawnShockwave()
    {
        if (shockwavePrefab == null)
        {
            Debug.LogWarning("DrumStomp: shockwavePrefab is not assigned.");
            return;
        }

        // Sound fires at the exact moment the shockwave spawns
        fighter.GetComponent<FighterAudio>()?.PlaySpecialSound();

        // Spawn at ground level — use spawnPoint if assigned, otherwise calculate
        Vector3 spawnPos = spawnPoint != null
            ? spawnPoint.position
            : transform.position + new Vector3(fighter.facingRight ? 0.8f : -0.8f, -0.5f, 0f);

        GameObject wave = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);

        Shockwave sw = wave.GetComponent<Shockwave>();
        if (sw != null)
        {
            sw.Initialize(
                owner: fighter,
                direction: fighter.facingRight ? 1f : -1f,
                speed: shockwaveSpeed,
                damage: damage,
                blockChipDamage: blockChipDamage,
                knockbackForce: knockbackForce,
                screenShake: screenShake
            );
        }

        // Screen shake and camera impulse at the stomp moment
        fighter.TriggerScreenShake(screenShake);
    }

    // -------------------------------------------------------
    // SpecialAbilityBase interface
    // -------------------------------------------------------

    public override bool IsReady() => cooldownTimer <= 0f && !isActive;

    public override float CooldownProgress()
    {
        if (cooldownDuration <= 0f) return 1f;
        return Mathf.Clamp01(1f - (cooldownTimer / cooldownDuration));
    }
}