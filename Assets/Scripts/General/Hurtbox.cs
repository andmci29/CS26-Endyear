using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    [HideInInspector]
    public FighterController owner;

    private void Awake()
    {
        owner = GetComponentInParent<FighterController>();

        if (owner == null)
            Debug.LogWarning($"Hurtbox on {gameObject.name} could not find a FighterController in parent.");
    }

    public void ReceiveHit(AttackData attack, FighterController attacker)
    {
        if (owner == null) return;
        if (owner == attacker) return;

        Debug.Log($"Hurtbox hit — owner: {owner.name}, attacker: {attacker.name}, damage: {attack.damage}");

        // FIX: Apply damage and stage knockback vectors first
        owner.TakeDamage(attack);

        // Then freeze the frame loop simultaneously
        owner.ApplyHitStop(attack.hitStopDuration);
        attacker.ApplyHitStop(attack.hitStopDuration);
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
}