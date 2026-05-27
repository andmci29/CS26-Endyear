using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [HideInInspector]
    public AttackData attackData;

    // Lazy — found on first use rather than Start, since object starts disabled
    private FighterController _owner;
    private FighterController owner
    {
        get
        {
            if (_owner == null)
                _owner = GetComponentInParent<FighterController>();
            return _owner;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Hitbox triggered by: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (attackData == null)
        {
            Debug.LogWarning("Hitbox fired but attackData is null — was EnableHitboxForAttack called?");
            return;
        }

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null) return;
        if (hurtbox.owner == owner) return; // don't hit yourself

        hurtbox.ReceiveHit(attackData, owner);

        // Disable immediately — one hit per swing
        gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = gameObject.activeInHierarchy ? Color.red : Color.grey;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}