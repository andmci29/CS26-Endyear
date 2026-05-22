using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public AttackData attackData;
    private FighterController owner;

    void Start()
    {
        owner = GetComponentInParent<FighterController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Hurtbox hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null) return;
        if (hurtbox.owner == owner) return; // don't hit yourself

        hurtbox.ReceiveHit(attackData);
        gameObject.SetActive(false); // prevent hitting the same opponent twice per swing
    }
}