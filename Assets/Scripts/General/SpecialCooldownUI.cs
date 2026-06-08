// SpecialCooldownUI.cs — attach to any GameObject in the scene, e.g. a UIManager
using UnityEngine;
using UnityEngine.UI;

public class SpecialCooldownUI : MonoBehaviour
{
    [Header("Player 1")]
    public Image p1CooldownImage;
    public GuitarSpecial p1Special;

    [Header("Player 2")]
    public Image p2CooldownImage;
    public GuitarSpecial p2Special;

    private void Update()
    {
        if (p1Special != null && p1CooldownImage != null)
            p1CooldownImage.fillAmount = p1Special.CooldownProgress();

        if (p2Special != null && p2CooldownImage != null)
            p2CooldownImage.fillAmount = p2Special.CooldownProgress();
    }
}