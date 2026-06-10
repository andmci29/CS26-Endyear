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

        // Play hit sound on the ATTACKER's audio — their instrument sound fires on confirmed hit
        FighterAudio attackerAudio = attacker.GetComponent<FighterAudio>();
        if (attackerAudio != null)
        {
            bool isHeavy = attack.damage >= 15f;
            attackerAudio.PlayAttackLandSound(isHeavy);
        }

        // Play impact sound on the DEFENDER — grunt, recoil sound
        FighterAudio defenderAudio = owner.GetComponent<FighterAudio>();
        if (defenderAudio != null)
        {
            bool wasBlocking = owner.currentState == FighterState.Blocking;
            defenderAudio.PlayHitSound(wasBlocking, attack.damage >= 15f);
        }

        owner.ApplyHitStop(attack.hitStopDuration);
        attacker.ApplyHitStop(attack.hitStopDuration);
        owner.TakeDamage(attack);
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