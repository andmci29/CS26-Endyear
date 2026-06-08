// SpecialAbilityBase.cs
// Abstract base — every character's special script inherits from this.
// FighterController calls TryActivate() when the special button is pressed.
// This means FighterController never needs to know what the special does.
using UnityEngine;

public abstract class SpecialAbilityBase : MonoBehaviour
{
    protected FighterController fighter;

    protected virtual void Awake()
    {
        fighter = GetComponent<FighterController>();

        if (fighter == null)
            Debug.LogWarning($"SpecialAbilityBase on {gameObject.name} could not find a FighterController.");
    }

    // Called by FighterController when the special button is pressed
    public abstract void TryActivate();

    // Returns 0-1 progress toward ability being ready again
    // Used by UI to draw a cooldown indicator
    public abstract float CooldownProgress();

    // True when the ability can currently be used
    public abstract bool IsReady();
}