using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    public FighterController owner;

    public void ReceiveHit(AttackData attack)
    {
        owner.TakeDamage(attack);
    }
}