using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Damage")]
    public float damage = 10f;
    public float blockDamageMultiplier = 0.2f;

    [Header("Knockback")]
    public Vector2 knockbackForce = new Vector2(4f, 2f);

    [Header("Feel")]
    public float hitStopDuration = 0.08f;  // seconds — roughly 5 frames at 60fps
    public float screenShakeMagnitude = 0.15f;

    [Header("Hitbox Shape")]
    public Vector3 hitboxOffset = new Vector3(0.5f, 0f, 0f);
    public Vector3 hitboxSize = new Vector3(0.8f, 0.6f, 0.6f);

    [Header("Timing")]
    [Tooltip("Seconds to wait before the hitbox turns on (Startup phase).")]
    public float hitboxDelay = 0.12f;
    [Tooltip("How long the hitbox stays active once spawned (Active phase).")]
    public float hitboxActiveTime = 0.15f;
    [Tooltip("Total length of the entire attack state sequence.")]
    public float attackDuration = 0.4f;
}