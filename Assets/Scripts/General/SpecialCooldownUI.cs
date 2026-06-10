// SpecialCooldownUI.cs
using UnityEngine;
using UnityEngine.UI;

public class SpecialCooldownUI : MonoBehaviour
{
    [Header("Player 1")]
    public Image p1CooldownImage;
    public SpecialAbilityBase p1Special;  // accepts GuitarSpecial, DrumStomp, or any future special

    [Header("Player 2")]
    public Image p2CooldownImage;
    public SpecialAbilityBase p2Special;

    private void Update()
    {
        if (p1Special != null && p1CooldownImage != null)
            p1CooldownImage.fillAmount = p1Special.CooldownProgress();

        if (p2Special != null && p2CooldownImage != null)
            p2CooldownImage.fillAmount = p2Special.CooldownProgress();
    }
}