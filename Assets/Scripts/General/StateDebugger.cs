// DebugStateSwitcher.cs — DELETE before final build
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

        if (kb.digit1Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Idle);
        if (kb.digit2Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Moving);
        if (kb.digit3Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Attacking);
        if (kb.digit4Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Blocking);
        if (kb.digit6Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.Knockback);
        if (kb.digit7Key.wasPressedThisFrame) fighter.TransitionTo(FighterState.KO);

        if (Keyboard.current.hKey.wasPressedThisFrame)
            fighter.EnableHitboxForAttack(0); // manually fire light attack hitbox

        if (Keyboard.current.nKey.wasPressedThisFrame)
            fighter.DisableHitbox();
    }
}