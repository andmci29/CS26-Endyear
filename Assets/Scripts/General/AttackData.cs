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
}