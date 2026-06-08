using UnityEngine;

[CreateAssetMenu(fileName = "FighterData", menuName = "Fighter Data")]
public class FighterData : ScriptableObject
{

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Combat")]
    public float lightDamage = 8f;
    public float heavyDamage = 18f;
    public float grabDamage = 15f;

    [Header("Defence")]
    public float blockDamageMultiplier = 0.2f;

    [Header("Recovery")]
    public float knockbackDuration = 0.4f;

    [Header("Health")]
    public float maxHealth = 100f;
}
