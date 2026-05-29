using UnityEngine;
using UnityEngine.InputSystem;

public class StateDebugger : MonoBehaviour
{
    private FighterController fighter;

    void Start() => fighter = GetComponent<FighterController>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Only attach this script to Fighter_P1 for testing
        if (kb.digit1Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Idle);
        if (kb.digit2Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Moving);
        if (kb.digit3Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Attacking);
        if (kb.digit4Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Blocking);
        if (kb.digit5Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Knockback);
        if (kb.digit6Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.KO);

        // Hitbox test — fires on this fighter only
        if (kb.hKey.wasPressedThisFrame) fighter.EnableHitboxForAttack(0);
        if (kb.nKey.wasPressedThisFrame) fighter.DisableHitbox();
    }
}